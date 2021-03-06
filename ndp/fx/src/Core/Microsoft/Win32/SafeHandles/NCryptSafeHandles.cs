// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.Contracts;

namespace Microsoft.Win32.SafeHandles {

    /// <summary>
    ///     Base class for NCrypt handles which need to support being pseudo-duplicated. This class is not for
    ///     external use (instead applications should consume the concrete subclasses of this class).
    /// </summary>
    /// <remarks>
    ///     Since NCrypt handles do not have a native DuplicateHandle type call, we need to do manual 
    ///     reference counting in managed code whenever we hand out an extra reference to one of these handles.
    ///     This class wraps up the logic to correctly duplicate and free these handles to simluate a native
    ///     duplication.
    /// 
    ///     Each open handle object can be thought of as being in one of three states:
    ///        1. Owner     - created via the marshaler, traditional style safe handle. Notably, only one owner
    ///                       handle exists for a given native handle.
    ///        2. Duplicate - points at a handle in the Holder state. Releasing a handle in the duplicate state
    ///                       results only in decrementing the reference count of the holder, not in a release
    ///                       of the native handle.
    ///        3. Holder    - holds onto a native handle and is referenced by handles in the duplicate state.
    ///                       When all duplicate handles are closed, the holder handle releases the native
    ///                       handle. A holder handle will never be finalized, since this results in a ----
    ///                       between the finalizers of the duplicate handles and the holder handle. Instead,
    ///                       it relies upon all of the duplicate handles to be finalized and decriment the
    ///                       ref count to zero.  Instances of a holder handle should never be referenced by
    ///                       anything but a duplicate handle.
    /// </remarks>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public abstract class SafeNCryptHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private enum OwnershipState {
            /// <summary>
            ///     The safe handle owns the native handle outright. This must be value 0, as this is the
            ///     state the marshaler will place the handle in when marshaling back a SafeHandle
            /// </summary>
            Owner = 0,

            /// <summary>
            ///     The safe handle does not own the native handle, but points to a Holder which does
            /// </summary>
            Duplicate,

            /// <summary>
            ///     The safe handle owns the native handle, but shares it with other Duplicate handles
            /// </summary>
            Holder
        }

        private OwnershipState m_ownershipState;

        /// <summary>
        ///     If the handle is a Duplicate, this points at the safe handle which actually owns the native handle.
        /// </summary>
        private SafeNCryptHandle m_holder;

        private SafeHandle _parentHandle;

        protected SafeNCryptHandle() : base(true) {
            return;
        }

        protected SafeNCryptHandle(IntPtr handle, SafeHandle parentHandle) : base(true) {
            if (parentHandle == null)
                throw new ArgumentNullException(nameof(parentHandle));
            if (parentHandle.IsClosed || parentHandle.IsInvalid)
                throw new ArgumentException(SR.Argument_Invalid_SafeHandleInvalidOrClosed, nameof(parentHandle));

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                bool addedRef = false;
                parentHandle.DangerousAddRef(ref addedRef);

                if (addedRef)
                {
                    _parentHandle = parentHandle;

                    SetHandle(handle);

                    // IsInvalid is a virtual call, but if it's evaluating to true we'll never call
                    // ReleaseHandle, so we'll never call DangerousRelease. So we need to call it here to
                    // maintain refcount parity.
                    if (IsInvalid)
                    {
                        _parentHandle.DangerousRelease();
                        _parentHandle = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Wrapper for the m_holder field which ensures that we're in a consistent state
        /// </summary>
        private SafeNCryptHandle Holder {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get {
                Contract.Requires((m_ownershipState == OwnershipState.Duplicate && m_holder != null) ||
                                  (m_ownershipState != OwnershipState.Duplicate && m_holder == null));
                Contract.Requires(m_holder == null || m_holder.m_ownershipState == OwnershipState.Holder);

                return m_holder;
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set {
                Contract.Ensures(m_holder.m_ownershipState == OwnershipState.Holder);
                Contract.Ensures(m_ownershipState == OwnershipState.Duplicate);
#if DEBUG
                Contract.Ensures(IsValidOpenState);
                Contract.Assert(value.IsValidOpenState);
#endif
                Contract.Assert(m_ownershipState != OwnershipState.Duplicate);
                Contract.Assert(value.m_ownershipState == OwnershipState.Holder);
                
               
                m_holder = value;
                m_ownershipState = OwnershipState.Duplicate;
            }
        }

#if DEBUG
        /// <summary>
        ///     Ensure the state of the handle is consistent for an open handle
        /// </summary>
        private bool IsValidOpenState {
            [Pure]
            get {
                switch (m_ownershipState) {
                    // Owner handles do not have a holder
                    case OwnershipState.Owner:
                        return Holder == null && !IsInvalid && !IsClosed;

                    // Duplicate handles have valid open holders with the same raw handle value,
                    // and should not be tracking a distinct parent.
                    case OwnershipState.Duplicate:
                        bool acquiredHolder = false;

                        RuntimeHelpers.PrepareConstrainedRegions();
                        try {
                            IntPtr holderRawHandle = IntPtr.Zero;

                            if (Holder != null) {
                                Holder.DangerousAddRef(ref acquiredHolder);
                                holderRawHandle = Holder.DangerousGetHandle();
                            }


                            bool holderValid = Holder != null &&
                                               !Holder.IsInvalid &&
                                               !Holder.IsClosed &&
                                               holderRawHandle != IntPtr.Zero &&
                                               holderRawHandle == handle &&
                                               _parentHandle == null;

                            return holderValid && !IsInvalid && !IsClosed;
                        }
                        finally {
                            if (acquiredHolder) {
                                Holder.DangerousRelease();
                            }
                        }

                    // Holder handles do not have a holder
                    case OwnershipState.Holder:
                        return Holder == null && !IsInvalid && !IsClosed;

                    // Unknown ownership state
                    default:
                        return false;
                }
            }
        }
#endif

        /// <summary>
        ///     Duplicate a handle
        /// </summary>
        /// <remarks>
        ///     #NCryptHandleDuplicationAlgorithm
        /// 
        ///     Duplicating a handle performs different operations depending upon the state of the handle:
        /// 
        ///     * Owner     - Allocate two new handles, a holder and a duplicate.
        ///                 - Suppress finalization on the holder
        ///                 - Transition into the duplicate state
        ///                 - Use the new holder as the holder for both this handle and the duplicate
        ///                 - Increment the reference count on the holder
        /// 
        ///     * Duplicate - Allocate a duplicate handle
        ///                 - Increment the reference count of our holder
        ///                 - Assign the duplicate's holder to be our holder
        /// 
        ///     * Holder    - Specifically disallowed. Holders should only ever be referenced by duplicates,
        ///                   so duplication will occur on the duplicate rather than the holder handle.
        /// </remarks>
        internal T Duplicate<T>() where T : SafeNCryptHandle, new() {
            // Spec#: Consider adding a model variable for ownership state?
            Contract.Ensures(Contract.Result<T>() != null);
            Contract.Ensures(m_ownershipState == OwnershipState.Duplicate);
            Contract.Ensures(Contract.Result<T>().m_ownershipState == OwnershipState.Duplicate);
#if DEBUG
            // Spec#: Consider a debug-only? model variable for IsValidOpenState?
            Contract.Ensures(Contract.Result<T>().IsValidOpenState);
            Contract.Ensures(IsValidOpenState);

            Contract.Assert(IsValidOpenState);
#endif
            Contract.Assert(m_ownershipState != OwnershipState.Holder);
            Contract.Assert(typeof(T) == this.GetType());

            if (m_ownershipState == OwnershipState.Owner) {
                return DuplicateOwnerHandle<T>();
            }
            else {
                // If we're not an owner handle, and we're being duplicated then we must be a duplicate handle.
                return DuplicateDuplicatedHandle<T>();
            }
        }

        /// <summary>
        ///     Duplicate a safe handle which is already duplicated.
        /// 
        ///     See code:Microsoft.Win32.SafeHandles.SafeNCryptHandle#NCryptHandleDuplicationAlgorithm for
        ///     details about the algorithm.
        /// </summary>
        private T DuplicateDuplicatedHandle<T>() where T : SafeNCryptHandle, new() {
            Contract.Ensures(m_ownershipState == OwnershipState.Duplicate);
            Contract.Ensures(Contract.Result<T>() != null &&
                             Contract.Result<T>().m_ownershipState == OwnershipState.Duplicate);
#if DEBUG
            Contract.Ensures(IsValidOpenState);
            Contract.Ensures(Contract.Result<T>().IsValidOpenState);

            Contract.Assert(IsValidOpenState);
#endif
            Contract.Assert(m_ownershipState == OwnershipState.Duplicate);
            Contract.Assert(typeof(T) == this.GetType());

            bool addedRef = false;
            T duplicate = new T();

            // We need to do this operation in a CER, since we need to make sure that if the AddRef occurs
            // that the duplicated handle will always point back to the Holder, otherwise the Holder will leak
            // since it will never have its ref count reduced to zero.
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally {
                Holder.DangerousAddRef(ref addedRef);
                duplicate.SetHandle(Holder.DangerousGetHandle());
                duplicate.Holder = Holder;              // Transitions to OwnershipState.Duplicate
            }

            return duplicate;
        }

        /// <summary>
        ///     Duplicate a safe handle which is currently the exclusive owner of a native handle
        /// 
        ///     See code:Microsoft.Win32.SafeHandles.SafeNCryptHandle#NCryptHandleDuplicationAlgorithm for
        ///     details about the algorithm.
        /// </summary>
        private T DuplicateOwnerHandle<T>() where T : SafeNCryptHandle, new() {
            Contract.Ensures(m_ownershipState == OwnershipState.Duplicate);
            Contract.Ensures(Contract.Result<T>() != null &&
                             Contract.Result<T>().m_ownershipState == OwnershipState.Duplicate);
#if DEBUG
            Contract.Ensures(IsValidOpenState);
            Contract.Ensures(Contract.Result<T>().IsValidOpenState);

            Contract.Assert(IsValidOpenState);
#endif
            Contract.Assert(m_ownershipState == OwnershipState.Owner);
            Contract.Assert(typeof(T) == this.GetType());

            bool addRef = false;

            T holder = new T();
            T duplicate = new T();

            // We need to do this operation in a CER in order to ensure that everybody's state stays consistent
            // with the current view of the world.  If the state of the various handles gets out of sync, then
            // we'll end up leaking since reference counts will not be set up properly.
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally {
                // Setup a holder safe handle to ref count the native handle
                holder.m_ownershipState = OwnershipState.Holder;
                holder.SetHandle(DangerousGetHandle());
                GC.SuppressFinalize(holder);

                if (_parentHandle != null)
                {
                    holder._parentHandle = _parentHandle;
                    _parentHandle = null;
                }

                // Transition into the duplicate state, referencing the holder. The initial reference count
                // on the holder will refer to the original handle so there is no need to AddRef here.
                Holder = holder;        // Transitions to OwnershipState.Duplicate

                // The duplicate handle will also reference the holder
                holder.DangerousAddRef(ref addRef);
                duplicate.SetHandle(holder.DangerousGetHandle());
                duplicate.Holder = holder;  // Transitions to OwnershipState.Duplicate
            }

            return duplicate;
        }

        /// <summary>
        ///     Release the handle
        /// </summary>
        /// <remarks>
        ///     Similar to duplication, releasing a handle performs different operations based upon the state
        ///     of the handle.
        /// 
        ///     An instance which was constructed with a parentHandle will only call DangerousRelease on
        ///     the parentHandle object. Otherwise the behavior is dictated by the ownership state.
        ///     
        ///     * Owner     - Simply call the release P/Invoke method
        ///     * Duplicate - Decrement the reference count of the current holder
        ///     * Holder    - Call the release P/Invoke. Note that ReleaseHandle on a holder implies a reference
        ///                   count of zero.
        /// </remarks>
        protected override bool ReleaseHandle() {
            if (m_ownershipState == OwnershipState.Duplicate) {
                Holder.DangerousRelease();
                return true;
            }
            else if (_parentHandle != null) {
                _parentHandle.DangerousRelease();
                return true;
            }
            else {
                return ReleaseNativeHandle();
            }
        }

        /// <summary>
        ///     Perform the actual release P/Invoke
        /// </summary>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected abstract bool ReleaseNativeHandle();
    }

    /// <summary>
    ///     Safe handle representing an NCRYPT_KEY_HANDLE
    /// </summary>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class SafeNCryptKeyHandle : SafeNCryptHandle {
        [DllImport("ncrypt.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int NCryptFreeObject(IntPtr hObject);

        public SafeNCryptKeyHandle() { }

        public SafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle)
            : base(handle, parentHandle)
        {
        }

        internal SafeNCryptKeyHandle Duplicate() {
            return Duplicate<SafeNCryptKeyHandle>();
        }

        protected override bool ReleaseNativeHandle() {
            return NCryptFreeObject(handle) == 0;
        }
    }

    /// <summary>
    ///     Safe handle representing an NCRYPT_PROV_HANDLE
    /// </summary>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class SafeNCryptProviderHandle : SafeNCryptHandle {
        [DllImport("ncrypt.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int NCryptFreeObject(IntPtr hObject);

        internal SafeNCryptProviderHandle Duplicate() {
            return Duplicate<SafeNCryptProviderHandle>();
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal void SetHandleValue(IntPtr newHandleValue) {
            Contract.Requires(newHandleValue != IntPtr.Zero);
            Contract.Requires(!IsClosed);
            Contract.Ensures(!IsInvalid);
            Contract.Assert(handle == IntPtr.Zero);

            SetHandle(newHandleValue);
        }

        protected override bool ReleaseNativeHandle() {
            return NCryptFreeObject(handle) == 0;
        }
    }

    /// <summary>
    ///     Safe handle representing an NCRYPT_SECRET_HANDLE
    /// </summary>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class SafeNCryptSecretHandle : SafeNCryptHandle {
        [DllImport("ncrypt.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int NCryptFreeObject(IntPtr hObject);

        protected override bool ReleaseNativeHandle() {
            return NCryptFreeObject(handle) == 0;
        }
    }
}
