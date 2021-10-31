// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Classes:  Object Security family of classes
**
**
===========================================================*/

using Microsoft.Win32;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if FEATURE_CORRUPTING_EXCEPTIONS
using System.Runtime.ExceptionServices;
#endif // FEATURE_CORRUPTING_EXCEPTIONS
using System.Security.Principal;
using System.Threading;
using System.Diagnostics.Contracts;

namespace System.Security.AccessControl
{

    public enum AccessControlModification
    {
        Add                    = 0,
        Set                    = 1,
        Reset                  = 2,
        Remove                 = 3,
        RemoveAll              = 4,
        RemoveSpecific         = 5,
    }


    public abstract class ObjectSecurity
    {
        #region Private Members

        private readonly ReaderWriterLock _lock = new ReaderWriterLock();

        internal CommonSecurityDescriptor _securityDescriptor;

        private bool _ownerModified = false;
        private bool _groupModified = false;
        private bool _saclModified = false;
        private bool _daclModified = false;
        
        // only these SACL control flags will be automatically carry forward
        // when update with new security descriptor.
        static private readonly ControlFlags SACL_CONTROL_FLAGS = 
            ControlFlags.SystemAclPresent | 
            ControlFlags.SystemAclAutoInherited |
            ControlFlags.SystemAclProtected;

        // only these DACL control flags will be automatically carry forward
        // when update with new security descriptor
        static private readonly ControlFlags DACL_CONTROL_FLAGS = 
            ControlFlags.DiscretionaryAclPresent | 
            ControlFlags.DiscretionaryAclAutoInherited |
            ControlFlags.DiscretionaryAclProtected;

        #endregion

        #region Constructors

        protected ObjectSecurity()
        {
        }
        
        protected ObjectSecurity( bool isContainer, bool isDS )
            : this()
        {
            // we will create an empty DACL, denying anyone any access as the default. 5 is the capacity.
            DiscretionaryAcl dacl = new DiscretionaryAcl(isContainer, isDS, 5);
             _securityDescriptor = new CommonSecurityDescriptor( isContainer, isDS, ControlFlags.None, null, null, null, dacl );
        }

        protected ObjectSecurity( CommonSecurityDescriptor securityDescriptor )
            : this()
        {
            if ( securityDescriptor == null )
            {
                throw new ArgumentNullException( "securityDescriptor" );
            }
            Contract.EndContractBlock();

             _securityDescriptor = securityDescriptor;
        }

        #endregion

        #region Private methods

        private void UpdateWithNewSecurityDescriptor( RawSecurityDescriptor newOne, AccessControlSections includeSections )
        {
            Contract.Assert( newOne != null, "Must not supply a null parameter here" );

            if (( includeSections & AccessControlSections.Owner ) != 0 )
            {
                _ownerModified = true;
                _securityDescriptor.Owner = newOne.Owner;
            }
 
            if (( includeSections & AccessControlSections.Group ) != 0 )
            {
                _groupModified = true;
                _securityDescriptor.Group = newOne.Group;
            }

            if (( includeSections & AccessControlSections.Audit ) != 0 )
            {
                _saclModified = true;
                if ( newOne.SystemAcl != null )
                {
                    _securityDescriptor.SystemAcl = new SystemAcl( IsContainer, IsDS, newOne.SystemAcl, true );
                }
                else
                {
                    _securityDescriptor.SystemAcl = null;
                }
                // carry forward the SACL related control flags
                _securityDescriptor.UpdateControlFlags(SACL_CONTROL_FLAGS, (ControlFlags)(newOne.ControlFlags & SACL_CONTROL_FLAGS));
            }

            if (( includeSections & AccessControlSections.Access ) != 0 )
            {
                _daclModified = true;
                if ( newOne.DiscretionaryAcl != null )
                {
                    _securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl( IsContainer, IsDS, newOne.DiscretionaryAcl, true );
                }
                else
                {
                    _securityDescriptor.DiscretionaryAcl = null;
                }
                // by the following property set, the _securityDescriptor's control flags
                // may contains DACL present flag. That needs to be carried forward! Therefore, we OR
                // the current _securityDescriptor.s DACL present flag.
                ControlFlags daclFlag = (_securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent);
                
                _securityDescriptor.UpdateControlFlags(DACL_CONTROL_FLAGS, 
                    (ControlFlags)((newOne.ControlFlags | daclFlag) & DACL_CONTROL_FLAGS));
            }
         }

        #endregion

        #region Protected Properties and Methods

        protected void ReadLock()
        {
            _lock.AcquireReaderLock( -1 );
        }

        protected void ReadUnlock()
        {
            _lock.ReleaseReaderLock();
        }

        protected void WriteLock()
        {
            _lock.AcquireWriterLock( -1 );
        }

        protected void WriteUnlock()
        {
            _lock.ReleaseWriterLock();
        }

        protected bool OwnerModified
        {
            get
            {
                if (!( _lock.IsReaderLockHeld || _lock.IsWriterLockHeld ))
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForReadOrWrite" ));
                }

                return _ownerModified;
            }

            set
            {
                if ( !_lock.IsWriterLockHeld )
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForWrite" ));
                }

                _ownerModified = value;
            }
        }

        protected bool GroupModified
        {
            get
            {
                if (!( _lock.IsReaderLockHeld || _lock.IsWriterLockHeld ))
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForReadOrWrite" ));
                }

                return _groupModified;
            }

            set
            {
                if ( !_lock.IsWriterLockHeld )
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForWrite" ));
                }

                _groupModified = value;
            }
        }

        protected bool AuditRulesModified
        {
            get
            {
                if (!( _lock.IsReaderLockHeld || _lock.IsWriterLockHeld ))
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForReadOrWrite" ));
                }

                return _saclModified;
            }

            set
            {
                if ( !_lock.IsWriterLockHeld )
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForWrite" ));
                }

                _saclModified = value;
            }
        }

        protected bool AccessRulesModified
        {
            get
            {
                if (!( _lock.IsReaderLockHeld || _lock.IsWriterLockHeld ))
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForReadOrWrite" ));
                }

                return _daclModified;
            }

            set
            {
                if ( !_lock.IsWriterLockHeld )
                {
                    throw new InvalidOperationException( Environment.GetResourceString( "InvalidOperation_MustLockForWrite" ));
                }

                _daclModified = value;
            }
        }

        protected bool IsContainer
        {
            get { return _securityDescriptor.IsContainer; }
        }

        protected bool IsDS
        {
            get { return _securityDescriptor.IsDS; }
        }

        //
        // Persists the changes made to the object
        //
        // This overloaded method takes a name of an existing object
        //

        protected virtual void Persist( string name, AccessControlSections includeSections )
        {
            throw new NotImplementedException();
        }

        //
        // if Persist (by name) is implemented, then this function will also try to enable take ownership
        // privilege while persisting if the enableOwnershipPrivilege is true.
        // Integrators can override it if this is not desired.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] // 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        protected virtual void Persist(bool enableOwnershipPrivilege, string name, AccessControlSections includeSections )
        {
            Privilege ownerPrivilege = null;

            // Ensure that the finally block will execute
            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                if (enableOwnershipPrivilege)
                {
                    ownerPrivilege = new Privilege(Privilege.TakeOwnership);
                    try
                    {
                        ownerPrivilege.Enable();
                    }
                    catch (PrivilegeNotHeldException)
                    {
                        // we will ignore this exception and press on just in case this is a remote resource
                    }
                }
                Persist(name, includeSections);
            }
            catch
            {
                // protection against exception filter-based luring attacks
                if ( ownerPrivilege != null )
                {
                    ownerPrivilege.Revert();
                }
                throw;
            }
            finally
            {
                if (ownerPrivilege != null)
                {
                    ownerPrivilege.Revert();
                }
            }
        }

        //
        // Persists the changes made to the object
        //
        // This overloaded method takes a handle to an existing object
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected virtual void Persist( SafeHandle handle, AccessControlSections includeSections )
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Public Methods

        //
        // Sets and retrieves the owner of this object
        //

        public IdentityReference GetOwner( System.Type targetType )
        {
            ReadLock();

            try
            {
                if ( _securityDescriptor.Owner == null )
                {
                    return null;
                }

                return _securityDescriptor.Owner.Translate( targetType );
            }
            finally
            {
                ReadUnlock();
            }
        }

        public void SetOwner( IdentityReference identity )
        {
            if ( identity == null )
            {
                throw new ArgumentNullException( "identity" );
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                _securityDescriptor.Owner = identity.Translate( typeof( SecurityIdentifier )) as SecurityIdentifier;
                _ownerModified = true;
            }
            finally
            {
                WriteUnlock();
            }
        }

        //
        // Sets and retrieves the group of this object
        //

        public IdentityReference GetGroup( System.Type targetType )
        {
            ReadLock();

            try
            {
                if ( _securityDescriptor.Group == null )
                {
                    return null;
                }

                return _securityDescriptor.Group.Translate( targetType );
            }
            finally
            {
                ReadUnlock();
            }
        }

        public void SetGroup( IdentityReference identity )
        {
            if ( identity == null )
            {
                throw new ArgumentNullException( "identity" );
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                _securityDescriptor.Group = identity.Translate( typeof( SecurityIdentifier )) as SecurityIdentifier;
                _groupModified = true;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public virtual void PurgeAccessRules( IdentityReference identity )
        {
            if ( identity == null )
            {
                throw new ArgumentNullException( "identity" );
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                _securityDescriptor.PurgeAccessControl( identity.Translate( typeof( SecurityIdentifier )) as SecurityIdentifier );
                _daclModified = true;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public virtual void PurgeAuditRules(IdentityReference identity)
        {
            if ( identity == null )
            {
                throw new ArgumentNullException( "identity" );
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                _securityDescriptor.PurgeAudit( identity.Translate( typeof( SecurityIdentifier )) as SecurityIdentifier );
                _saclModified = true;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public bool AreAccessRulesProtected
        {
            get
            {
                ReadLock();

                try
                {
                    return (( _securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected ) != 0 );
                }
                finally
                {
                    ReadUnlock();
                }
            }
        }

        public void SetAccessRuleProtection( bool isProtected, bool preserveInheritance )
        {
            WriteLock();

            try
            {
                _securityDescriptor.SetDiscretionaryAclProtection( isProtected, preserveInheritance );
                _daclModified = true;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public bool AreAuditRulesProtected
        {
            get
            {
                ReadLock();

                try
                {
                    return (( _securityDescriptor.ControlFlags & ControlFlags.SystemAclProtected ) != 0 );
                }
                finally
                {
                    ReadUnlock();
                }
            }
        }

        public void SetAuditRuleProtection( bool isProtected, bool preserveInheritance )
        {
            WriteLock();

            try
            {
                _securityDescriptor.SetSystemAclProtection( isProtected, preserveInheritance );
                _saclModified = true;
            }
            finally
            {
                WriteUnlock();
            }
        }

        public bool AreAccessRulesCanonical
        {
            get
            {
                ReadLock();

                try
                {
                    return _securityDescriptor.IsDiscretionaryAclCanonical;
                }
                finally
                {
                    ReadUnlock();
                }
            }
        }

        public bool AreAuditRulesCanonical
        {
            get
            {
                ReadLock();

                try
                {
                    return _securityDescriptor.IsSystemAclCanonical;
                }
                finally
                {
                    ReadUnlock();
                }
            }
        }

        public static bool IsSddlConversionSupported()
        {
            return true; // SDDL to binary conversions are supported on Windows 2000 and higher
        }
        
        public string GetSecurityDescriptorSddlForm( AccessControlSections includeSections )
        {
            ReadLock();

            try
            {
                return _securityDescriptor.GetSddlForm( includeSections );
            }
            finally
            {
                ReadUnlock();
            }
        }

        public void SetSecurityDescriptorSddlForm( string sddlForm )
        {
            SetSecurityDescriptorSddlForm( sddlForm, AccessControlSections.All );
        }

        public void SetSecurityDescriptorSddlForm( string sddlForm, AccessControlSections includeSections )
        {
            if ( sddlForm == null )
            {
                throw new ArgumentNullException( "sddlForm" );
            }

            if (( includeSections & AccessControlSections.All ) == 0 )
            {
                throw new ArgumentException(
                    Environment.GetResourceString( "Arg_EnumAtLeastOneFlag" ),
                    "includeSections" );
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                UpdateWithNewSecurityDescriptor( new RawSecurityDescriptor( sddlForm ), includeSections );
            }
            finally
            {
                WriteUnlock();
            }
        }

        public byte[] GetSecurityDescriptorBinaryForm()
        {
            ReadLock();

            try
            {
                byte[] result = new byte[_securityDescriptor.BinaryLength];

                _securityDescriptor.GetBinaryForm( result, 0 );

                return result;
            }
            finally
            {
                ReadUnlock();
            }
        }

        public void SetSecurityDescriptorBinaryForm( byte[] binaryForm )
        {
            SetSecurityDescriptorBinaryForm( binaryForm, AccessControlSections.All );
        }

        public void SetSecurityDescriptorBinaryForm( byte[] binaryForm, AccessControlSections includeSections )
        {
            if ( binaryForm == null )
            {
                throw new ArgumentNullException( "binaryForm" );
            }

            if (( includeSections & AccessControlSections.All ) == 0 )
            {
                throw new ArgumentException(
                    Environment.GetResourceString( "Arg_EnumAtLeastOneFlag" ),
                    "includeSections" );
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                UpdateWithNewSecurityDescriptor( new RawSecurityDescriptor( binaryForm, 0 ), includeSections );
            }
            finally
            {
                WriteUnlock();
            }
        }

        public abstract Type AccessRightType { get; }
        public abstract Type AccessRuleType { get; }
        public abstract Type AuditRuleType { get; }
        
        protected abstract bool ModifyAccess( AccessControlModification modification, AccessRule rule, out bool modified);
        protected abstract bool ModifyAudit( AccessControlModification modification, AuditRule rule, out bool modified );
        
        public virtual bool ModifyAccessRule(AccessControlModification modification, AccessRule rule, out bool modified)
        {
            if ( rule == null )
            {
                throw new ArgumentNullException( "rule" );
            }

            if ( !this.AccessRuleType.IsAssignableFrom(rule.GetType()) )
            {
                throw new ArgumentException(
                    Environment.GetResourceString("AccessControl_InvalidAccessRuleType"), 
                    "rule");
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                return ModifyAccess(modification, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }
        
        public virtual bool ModifyAuditRule(AccessControlModification modification, AuditRule rule, out bool modified)
        {
            if ( rule == null )
            {
                throw new ArgumentNullException( "rule" );
            }

            if ( !this.AuditRuleType.IsAssignableFrom(rule.GetType()) )
            {
                throw new ArgumentException(
                    Environment.GetResourceString("AccessControl_InvalidAuditRuleType"), 
                    "rule");
            }
            Contract.EndContractBlock();

            WriteLock();

            try
            {
                return ModifyAudit(modification, rule, out modified);
            }
            finally
            {
                WriteUnlock();
            }
        }
        
        public abstract AccessRule AccessRuleFactory( IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type );
        
        public abstract AuditRule AuditRuleFactory( IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags );
        #endregion
    }
}
