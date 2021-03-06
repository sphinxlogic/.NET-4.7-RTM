// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>
// 

namespace System.Security {
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Reflection;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    internal class SecurityRuntime
    {
        private SecurityRuntime(){}

        // Returns the security object for the caller of the method containing
        // 'stackMark' on its frame.
        //
        // THE RETURNED OBJECT IS THE LIVE RUNTIME OBJECT. BE CAREFUL WITH IT!
        //
        // Internal only, do not doc.
        // 
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern 
        FrameSecurityDescriptor GetSecurityObjectForFrame(ref StackCrawlMark stackMark,
                                                          bool create);

        // Constants used to return status to native
        internal const bool StackContinue  = true;
        internal const bool StackHalt      = false;

        // this method is a big perf hit, so don't call unnecessarily
        [System.Security.SecurityCritical]  // auto-generated
        internal static MethodInfo GetMethodInfo(RuntimeMethodHandleInternal rmh)
        {
            if (rmh.IsNullHandle())
                return null;

#if _DEBUG
            try
            {
#endif
                // Assert here because reflection will check grants and if we fail the check,
                // there will be an infinite recursion that overflows the stack.
                PermissionSet.s_fullTrust.Assert();
                return (System.RuntimeType.GetMethodBase(RuntimeMethodHandle.GetDeclaringType(rmh), rmh) as MethodInfo);
#if _DEBUG
            }
            catch(Exception)
            {
                return null;
            }
#endif
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static bool FrameDescSetHelper(FrameSecurityDescriptor secDesc,
                                               PermissionSet demandSet,
                                               out PermissionSet alteredDemandSet,
                                               RuntimeMethodHandleInternal rmh)
        {
            return secDesc.CheckSetDemand(demandSet, out alteredDemandSet, rmh);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static bool FrameDescHelper(FrameSecurityDescriptor secDesc,
                                               IPermission demandIn, 
                                               PermissionToken permToken,
                                               RuntimeMethodHandleInternal rmh)
        {
            return secDesc.CheckDemand((CodeAccessPermission) demandIn, permToken, rmh);
        }

#if FEATURE_COMPRESSEDSTACK
        [System.Security.SecurityCritical]
        private static bool CheckDynamicMethodSetHelper(System.Reflection.Emit.DynamicResolver dynamicResolver,
                                                     PermissionSet demandSet,
                                                     out PermissionSet alteredDemandSet,
                                                     RuntimeMethodHandleInternal rmh)
        {
            System.Threading.CompressedStack creationStack = dynamicResolver.GetSecurityContext();
            bool result;
            try
            {
                result = creationStack.CheckSetDemandWithModificationNoHalt(demandSet, out alteredDemandSet, rmh);
            }
            catch (SecurityException ex)
            {
                throw new SecurityException(Environment.GetResourceString("Security_AnonymouslyHostedDynamicMethodCheckFailed"), ex);
            }
            
            return result;
        }

        [System.Security.SecurityCritical]
        private static bool CheckDynamicMethodHelper(System.Reflection.Emit.DynamicResolver dynamicResolver,
                                             IPermission demandIn, 
                                             PermissionToken permToken,
                                             RuntimeMethodHandleInternal rmh)
        {
            System.Threading.CompressedStack creationStack = dynamicResolver.GetSecurityContext();
            bool result;
            try
            {
                result = creationStack.CheckDemandNoHalt((CodeAccessPermission)demandIn, permToken, rmh);
            }
            catch (SecurityException ex)
            {
                throw new SecurityException(Environment.GetResourceString("Security_AnonymouslyHostedDynamicMethodCheckFailed"), ex);
            }
            return result;
        }
#endif // FEATURE_COMPRESSEDSTACK

        //
        // API for PermissionSets
        //
        
        [System.Security.SecurityCritical]  // auto-generated
        internal static void Assert(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
            // Note: if the "AssertPermission" is not a permission that implements IUnrestrictedPermission
            // you need to change the fourth parameter to a zero.
            FrameSecurityDescriptor secObj = CodeAccessSecurityEngine.CheckNReturnSO(
                                                CodeAccessSecurityEngine.AssertPermissionToken,
                                                CodeAccessSecurityEngine.AssertPermission,
                                                ref stackMark,
                                                1 );
            
            Contract.Assert(secObj != null,"Failure in SecurityRuntime.Assert() - secObj != null");
            if (secObj == null)
            {
                // Security: REQ_SQ flag is missing. Bad compiler ?
                System.Environment.FailFast(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
            else
            {
                if (secObj.HasImperativeAsserts())
                    throw new SecurityException( Environment.GetResourceString( "Security_MustRevertOverride" ) );

                secObj.SetAssert(permSet);
            }
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        internal static void AssertAllPossible(ref StackCrawlMark stackMark)
        {
            FrameSecurityDescriptor secObj =
                SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true);
    
            Contract.Assert(secObj != null, "Failure in SecurityRuntime.AssertAllPossible() - secObj != null");
            if (secObj == null)
            {
                // Security: REQ_SQ flag is missing. Bad compiler ?
                System.Environment.FailFast(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
            else
            {
                if (secObj.GetAssertAllPossible())
                    throw new SecurityException( Environment.GetResourceString( "Security_MustRevertOverride" ) );

                secObj.SetAssertAllPossible();
            }
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        internal static void Deny(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
#if FEATURE_CAS_POLICY
            // Deny is only valid in legacy mode
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CasDeny"));
            }
#endif // FEATURE_CAS_POLICY

            FrameSecurityDescriptor secObj =
                SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true);
    
            Contract.Assert(secObj != null, "Failure in SecurityRuntime.Deny() - secObj != null");
            if (secObj == null)
            {
                // Security: REQ_SQ flag is missing. Bad compiler ?
                System.Environment.FailFast(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
            else
            {
                if (secObj.HasImperativeDenials())
                    throw new SecurityException( Environment.GetResourceString( "Security_MustRevertOverride" ) );

                secObj.SetDeny(permSet);
            }
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        internal static void PermitOnly(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
            FrameSecurityDescriptor secObj =
                SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true);
    
            Contract.Assert(secObj != null, "Failure in SecurityRuntime.PermitOnly() - secObj != null");
            if (secObj == null)
            {
                // Security: REQ_SQ flag is missing. Bad compiler ?
                System.Environment.FailFast(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
            else
            {
                if (secObj.HasImperativeRestrictions())
                    throw new SecurityException( Environment.GetResourceString( "Security_MustRevertOverride" ) );

                secObj.SetPermitOnly(permSet);
            }
        }
    
        //
        // Revert API
        //
        
        [System.Security.SecurityCritical]  // auto-generated
        internal static void RevertAssert(ref StackCrawlMark stackMark)
        {
            FrameSecurityDescriptor secObj = GetSecurityObjectForFrame(ref stackMark, false);
            if (secObj != null)
            {
                secObj.RevertAssert();
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static void RevertDeny(ref StackCrawlMark stackMark)
        {
            FrameSecurityDescriptor secObj = GetSecurityObjectForFrame(ref stackMark, false);
            if (secObj != null)
            {
                secObj.RevertDeny();
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal static void RevertPermitOnly(ref StackCrawlMark stackMark)
        {
            FrameSecurityDescriptor secObj = GetSecurityObjectForFrame(ref stackMark, false);
            if (secObj != null)
            {
                secObj.RevertPermitOnly();
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal static void RevertAll(ref StackCrawlMark stackMark)
        {
            FrameSecurityDescriptor secObj = GetSecurityObjectForFrame(ref stackMark, false);
            if (secObj != null)
            {
                secObj.RevertAll();
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
        }
    }
}


