//#define OLD_ISF
using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Windows.Interop;
using MS.Internal;
using MS.Win32;

namespace MS.Win32.Penimc
{
    internal static class UnsafeNativeMethods
    {
        // DDVSO:514949
        // The flags in this region are all in support of COM hardening to add resilience
        // to (OSGVSO:10779198).
        // They are special arguments to COM calls that allow us to re-purpose them for 
        // functions relating to this hardening.
        #region PenIMC Operations Flags

        /// <summary>
        /// Instruct IPimcManager2.GetTablet to release the external lock on itself.
        /// </summary>
        private const UInt32 ReleaseManagerExt = 0xFFFFDEAD;

        /// <summary>
        /// Instruct IPimcTablet2.GetCursorButtonCount to release the external lock on itself.
        /// </summary>
        private const int ReleaseTabletExt = -1;

        /// <summary>
        /// Instruct IPimcTablet2.GetCursorButtonCount to return the GIT key for the WISP Tablet.
        /// </summary>
        private const int GetWispTabletKey = -2;

        /// <summary>
        /// Instruct IPimcTablet2.GetCursorButtonCount to return the GIT key for the WISP Tablet Manager.
        /// </summary>
        private const int GetWispManagerKey = -3;

        /// <summary>
        /// Instruct IPimcTablet2.GetCursorButtonCount to acquire the external lock on itself.
        /// </summary>
        private const int LockTabletExt = -4;

        /// <summary>
        /// Instruct IPimcContext2.GetPacketPropertyInfo to return the GIT key for the WISP Tablet Context.
        /// </summary>
        private const int GetWispContextKey = -1;

        #endregion

        /// <SecurityNote>
        /// Critical to prevent inadvertant spread to transparent code
        /// </SecurityNote>
        [SecurityCritical]
        private static IPimcManager2 _pimcManager;

        #region Stylus Input Thread Manager

        /// <summary>
        /// The GIT key to use when managing the WISP Tablet Manager objects
        /// </summary>
        /// <SecurityNote>
        ///     Critical:  This field can be used to manipulate the GIT entries for WISP objects.
        /// </SecurityNote>
        [SecurityCritical]
        [ThreadStatic]
        private static UInt32? _wispManagerKey;

        /// <summary>
        /// Whether or not the WISP Tablet Manager server object has been locked in the MTA.
        /// </summary>
        [ThreadStatic]
        private static bool _wispManagerLocked = false;

        /// <SecurityNote>
        /// Critical to prevent inadvertant spread to transparent code
        /// </SecurityNote>
        [SecurityCritical]
        [ThreadStatic]
        private static IPimcManager2 _pimcManagerThreadStatic;

        #endregion

        /// <summary>
        /// Make sure we load penimc.dll from COM registered location to avoid two instances of it.
        /// </summary>
        /// <SecurityNote>
        /// Critical calls COM interop code that uses suppress unmanaged code security attributes
        /// </SecurityNote>
        [SecurityCritical]
        static UnsafeNativeMethods()
        {
            // This static contructor was added to make sure we load the proper version of Penimc.dll.  
            // 
            // Details:
            // P/invoke will use LoadLibrary to load penimc.dll and it will check the current
            // application directory before checking the system32 directory where this DLL
            // is installed to.  If penimc.dll happens to be in the application directory as well
            // as the system32 directory we'll actually end up loaded two versions of
            // penimc.dll.  One that we'd use for P/invokes and one that we'd use for COM.
            // If this happens then our Stylus code will fail since it relies on both P/invoke and COM
            // calls to talk to penimc.dll and it requires just one instance of this DLL to work.
            // So to make sure this doesn't happen we want to ensure we load the DLL using the COM 
            // registered path before doing any P/invokes into it.
            _pimcManager = CreatePimcManager();

            // DDVSO:514949
            // Ensure that we release the lock taken by CoLockObjectExternal calls in CPimcManager::FinalConstruct
            // This doesn't release the object, but ensures we do not leak it when the thread ends.
            ReleaseManagerExternalLockImpl(_pimcManager);
        }

        /// <summary>
        /// Returns IPimcManager2 interface.  Creates this object the first time per thread.
        /// </summary>
        /// <SecurityNote>
        /// Critical  - returns critial data _pimcManager.
        /// </SecurityNote>
        internal static IPimcManager2 PimcManager
        {
            [SecurityCritical]
            get
            {
                if (_pimcManagerThreadStatic == null)
                {
                    _pimcManagerThreadStatic = CreatePimcManager();
                }
                return _pimcManagerThreadStatic;
            }
        }

        /// <summary>
        /// Creates a new instance of PimcManager.
        /// </summary>
        /// <SecurityNote>
        /// Critical calls interop code that uses suppress unmanaged code security attributes
        /// </SecurityNote>
        [SecurityCritical]
        private static IPimcManager2 CreatePimcManager()
        {
            // Instantiating PimcManager using "new PimcManager()" results
            // in calling CoCreateInstanceForApp from an immersive process
            // (like designer). Such a call would fail because PimcManager is not
            // in ---- for that call. Hence we call CoCreateInstance directly.
            // Note: Normally WPF is not supported for immersive processes
            // but designer is an exception.
            Guid clsid = Guid.Parse(PimcConstants.PimcManager2CLSID);
            Guid iid = Guid.Parse(PimcConstants.IPimcManager2IID);
            object pimcManagerObj = CoCreateInstance(ref clsid,
                                                     null,
                                                     0x1, /*CLSCTX_INPROC_SERVER*/
                                                     ref iid);
            return ((IPimcManager2)pimcManagerObj);
        }

        #region COM Locking/Unlocking Functions

        #region General

        /// <summary>
        /// Calls WISP GIT lock functions on Win8+.
        /// On Win7 these will always fail since WISP objects are always proxies (WISP is out of proc).
        /// </summary>
        /// <param name="gitKey">The GIT key for the object to lock.</param>
        /// <SecurityNote>
        ///     Critical:   Calls LockWispObjectFromGit
        /// </SecurityNote>
        [SecurityCritical]
        internal static void CheckedLockWispObjectFromGit(UInt32 gitKey)
        {
            if (OSVersionHelper.IsOsWindows8OrGreater)
            {
                if (!LockWispObjectFromGit(gitKey))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Calls WISP GIT unlock functions on Win8+.
        /// On Win7 these will always fail since WISP objects are always proxies (WISP is out of proc).
        /// </summary>
        /// <param name="gitKey">The GIT key for the object to unlock.</param>
        /// <SecurityNote>
        ///     Critical:   Calls UnlockWispObjectFromGit
        /// </SecurityNote>
        [SecurityCritical]
        internal static void CheckedUnlockWispObjectFromGit(UInt32 gitKey)
        {
            if (OSVersionHelper.IsOsWindows8OrGreater)
            {
                if (!UnlockWispObjectFromGit(gitKey))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion

        #region Manager

        /// <summary>
        /// DDVSO:514949
        /// Calls into GetTablet with a special flag that indicates we should release
        /// the lock obtained previously by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The manager to release the lock for.</param>'
        /// <SecurityNote>
        ///     Critical:       Accesses IPimcManager2.
        /// </SecurityNote>
        [SecurityCritical]
        private static void ReleaseManagerExternalLockImpl(IPimcManager2 manager)
        {
            IPimcTablet2 unused = null;
            manager.GetTablet(ReleaseManagerExt, out unused);
        }

        /// <summary>
        /// DDVSO:514949
        /// Calls into GetTablet with a special flag that indicates we should release
        /// the lock obtained previously by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The manager to release the lock for.</param>'
        /// <SecurityNote>
        ///     Critical:       Accesses IPimcManager2.
        /// </SecurityNote>
        [SecurityCritical]
        internal static void ReleaseManagerExternalLock()
        {
            if (_pimcManagerThreadStatic != null)
            {
                ReleaseManagerExternalLockImpl(_pimcManagerThreadStatic);
            }
        }

        /// <summary>
        /// Queries and sets the GIT key for the WISP Tablet Manager
        /// </summary>
        /// <param name="tablet">The tablet to call through</param>
        /// <SecurityNote>
        ///     Critical:       Accesses IPimcTablet2.
        /// </SecurityNote>
        [SecurityCritical]
        internal static void SetWispManagerKey(IPimcTablet2 tablet)
        {
            UInt32 latestKey = QueryWispKeyFromTablet(GetWispManagerKey, tablet);

            // Assert here to ensure that every call through to this specific manager has the same
            // key.  This should be guaranteed since these calls are always done on the thread the tablet
            // is created on and all tablets created on a particular thread should be through the same
            // manager.
            Invariant.Assert(!_wispManagerKey.HasValue || _wispManagerKey.Value == latestKey);

            _wispManagerKey = latestKey;
        }

        /// <summary>
        /// Calls down into PenIMC in order to lock the WISP Tablet Manager.
        /// </summary>
        [SecurityCritical]
        internal static void LockWispManager()
        {
            if (!_wispManagerLocked && _wispManagerKey.HasValue)
            {
                CheckedLockWispObjectFromGit(_wispManagerKey.Value);
                _wispManagerLocked = true;
            }
        }

        /// <summary>
        /// Calls down into PenIMC in order to unlock the WISP Tablet Manager.
        /// </summary>
        [SecurityCritical]
        internal static void UnlockWispManager()
        {
            if (_wispManagerLocked && _wispManagerKey.HasValue)
            {
                CheckedUnlockWispObjectFromGit(_wispManagerKey.Value);
                _wispManagerLocked = false;
            }
        }

        #endregion

        #region Tablet

        /// <summary>
        /// DDVSO:514949
        /// Calls into GetCursorButtonCount with a special flag that indicates we should acquire
        /// the external lock by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The tablet to acquire the lock for.</param>'
        /// <SecurityNote>
        ///     Critical:       Accesses IPimcTablet2.
        /// </SecurityNote>
        [SecurityCritical]
        internal static void AcquireTabletExternalLock(IPimcTablet2 tablet)
        {
            int unused = 0;

            // Call through with special param to release the external lock on the tablet.
            tablet.GetCursorButtonCount(LockTabletExt, out unused);
        }

        /// <summary>
        /// DDVSO:514949
        /// Calls into GetCursorButtonCount with a special flag that indicates we should release
        /// the lock obtained previously by a CoLockObjectExternal call.
        /// </summary>
        /// <param name="manager">The tablet to release the lock for.</param>'
        /// <SecurityNote>
        ///     Critical:       Accesses IPimcTablet2.
        /// </SecurityNote>
        [SecurityCritical]
        internal static void ReleaseTabletExternalLock(IPimcTablet2 tablet)
        {
            int unused = 0;

            // Call through with special param to release the external lock on the tablet.
            tablet.GetCursorButtonCount(ReleaseTabletExt, out unused);
        }

        /// <summary>
        /// Queries the GIT key from the PenIMC Tablet
        /// </summary>
        /// <param name="keyType">The kind of key to instruct the tablet to return</param>
        /// <param name="tablet">The tablet to call through</param>
        /// <returns>The GIT key for the requested operation</returns>
        /// <SecurityNote>
        ///     Critical:       Accesses IPimcTablet2.
        /// </SecurityNote>
        [SecurityCritical]
        private static UInt32 QueryWispKeyFromTablet(int keyType, IPimcTablet2 tablet)
        {
            int key = 0;

            tablet.GetCursorButtonCount(keyType, out key);

            if(key == 0)
            {
                throw new InvalidOperationException();
            }

            return (UInt32)key;
        }

        /// <summary>
        /// Queries the GIT key for the WISP Tablet
        /// </summary>
        /// <param name="tablet">The tablet to call through</param>
        /// <returns>The GIT key for the WISP Tablet</returns>
        /// <SecurityNote>
        ///     Critical:       Accesses IPimcTablet2.
        /// </SecurityNote>
        [SecurityCritical]
        internal static UInt32 QueryWispTabletKey(IPimcTablet2 tablet)
        {
            return QueryWispKeyFromTablet(GetWispTabletKey, tablet);
        }

        #endregion

        #region Context

        /// <summary>
        /// Queries the GIT key for the WISP Tablet Context
        /// </summary>
        /// <param name="context">The context to query through</param>
        /// <returns>The GIT key for the WISP Tablet Context</returns>
        ///  <SecurityNote>
        ///     Critical:       Accesses IPimcContext2.
        /// </SecurityNote>
        [SecurityCritical]
        internal static UInt32 QueryWispContextKey(IPimcContext2 context)
        {
            int key = 0;
            Guid unused = Guid.Empty;
            int unused2 = 0;
            int unused3 = 0;
            float unused4 = 0;

            context.GetPacketPropertyInfo(GetWispContextKey, out unused, out key, out unused2, out unused3, out unused4);

            if (key == 0)
            {
                throw new InvalidOperationException();
            }

            return (UInt32)key;
        }

        #endregion

        #endregion

#if OLD_ISF
        /// <summary>
        /// Managed wrapper for IsfCompressPropertyData
        /// </summary>
        /// <param name="pbInput">Input byte array</param>
        /// <param name="cbInput">number of bytes in byte array</param>
        /// <param name="pnAlgoByte">
        /// In: Preferred algorithm Id
        /// Out: Best algorithm with parameters
        /// </param>
        /// <param name="pcbOutput">
        /// In: output buffer size (of pbOutput)
        /// Out: Actual number of bytes needed for compressed data
        /// </param>
        /// <param name="pbOutput">Buffer to hold the output</param>
        /// <returns>Status</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfCompressPropertyData(
                [In] byte [] pbInput,
                uint cbInput, 
                ref byte pnAlgoByte, 
                ref uint pcbOutput, 
                [In, Out] byte [] pbOutput
            );

        /// <summary>
        /// Managed wrapper for IsfDecompressPropertyData
        /// </summary>
        /// <param name="pbCompressed">Input buffer to be decompressed</param>
        /// <param name="cbCompressed">Number of bytes in the input buffer to be decompressed</param>
        /// <param name="pcbOutput">
        /// In: Output buffer capacity
        /// Out: Actual number of bytes required to hold uncompressed bytes
        /// </param>
        /// <param name="pbOutput">Output buffer</param>
        /// <param name="pnAlgoByte">Algorithm id and parameters</param>
        /// <returns>Status</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfDecompressPropertyData(
                [In] byte [] pbCompressed,   
                uint cbCompressed,   
                ref uint pcbOutput,  
                [In, Out] byte [] pbOutput, 
                ref byte pnAlgoByte 
            );

        /// <summary>
        /// Managed wrapper for IsfCompressPacketData
        /// </summary>
        /// <param name="hCompress">Handle to the compression engine (null is ok)</param>
        /// <param name="pbInput">Input buffer</param>
        /// <param name="cInCount">Number of bytes in the input buffer</param>
        /// <param name="pnAlgoByte">
        /// In: Preferred compression algorithm byte
        /// Out: Actual compression algorithm byte
        /// </param>
        /// <param name="pcbOutput">
        /// In: Output buffer capacity
        /// Out: Actual number of bytes required to hold compressed bytes
        /// </param>
        /// <param name="pbOutput">Output buffer</param>
        /// <returns>Status</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfCompressPacketData(
                CompressorSafeHandle hCompress,
                [In] int [] pbInput,
                [In] uint cInCount,
                ref byte pnAlgoByte,
                ref uint pcbOutput,
                [In, Out] byte [] pbOutput
            );

        /// <summary>
        /// Managed wrapper for IsfDecompressPacketData
        /// </summary>
        /// <param name="hCompress">Handle to the compression engine (null is ok)</param>
        /// <param name="pbCompressed">Input buffer of compressed bytes</param>
        /// <param name="pcbCompressed">
        /// In: Size of the input buffer
        /// Out: Actual number of compressed bytes decompressed.
        /// </param>
        /// <param name="cInCount">Count of int's in the compressed buffer</param>
        /// <param name="pbOutput">Output buffer to receive the decompressed int's</param>
        /// <param name="pnAlgoData">Algorithm bytes</param>
        /// <returns>Status</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern int IsfDecompressPacketData(
                CompressorSafeHandle hCompress,
                [In] byte [] pbCompressed,
                ref uint pcbCompressed,
                uint cInCount,
                [In, Out] int [] pbOutput,
                ref byte pnAlgoData
            );

        /// <summary>
        /// Managed wrapper for IsfLoadCompressor
        /// </summary>
        /// <param name="pbInput">Input buffer where compressor is saved</param>
        /// <param name="pcbInput">
        /// In: Size of the input buffer
        /// Out: Number of bytes in the input buffer decompressed to construct compressor
        /// </param>
        /// <returns>Handle to the compression engine loaded</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern CompressorSafeHandle IsfLoadCompressor(
                [In] byte [] pbInput,
                ref uint pcbInput
            );

        /// <summary>
        /// Managed wrapper for IsfReleaseCompressor
        /// </summary>
        /// <param name="hCompress">Handle to the Compression Engine to be released</param>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        internal static extern void IsfReleaseCompressor(
                IntPtr hCompress
            );
#endif

        /// <summary>
        /// Managed wrapper for GetPenEvent
        /// </summary>
        /// <param name="commHandle">Win32 event handle to wait on for new stylus input.</param>
        /// <param name="handleReset">Win32 event the signals a reset.</param>
        /// <param name="evt">Stylus event that was triggered.</param>
        /// <param name="stylusPointerId">Stylus Device ID that triggered input.</param>
        /// <param name="cPackets">Count of packets returned.</param>
        /// <param name="cbPacket">Byte count of packet data returned.</param>
        /// <param name="pPackets">Array of ints containing the packet data.</param>
        /// <returns>true if succeeded, false if failed or shutting down.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPenEvent(
            IntPtr      commHandle,
            IntPtr      handleReset,
            out int     evt,
            out int     stylusPointerId,
            out int     cPackets,
            out int     cbPacket,
            out IntPtr  pPackets);

        /// <summary>
        /// Managed wrapper for GetPenEventMultiple
        /// </summary>
        /// <param name="cCommHandles">Count of elements in commHandles.</param>
        /// <param name="commHandles">Array of Win32 event handles to wait on for new stylus input.</param>
        /// <param name="handleReset">Win32 event the signals a reset.</param>
        /// <param name="iHandle">Index to the handle that triggered return.</param>
        /// <param name="evt">Stylus event that was triggered.</param>
        /// <param name="stylusPointerId">Stylus Device ID that triggered input.</param>
        /// <param name="cPackets">Count of packets returned.</param>
        /// <param name="cbPacket">Byte count of packet data returned.</param>
        /// <param name="pPackets">Array of ints containing the packet data.</param>
        /// <returns>true if succeeded, false if failed or shutting down.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPenEventMultiple(
            int         cCommHandles,
            IntPtr[]    commHandles,
            IntPtr      handleReset,
            out int     iHandle,
            out int     evt,
            out int     stylusPointerId,
            out int     cPackets,
            out int     cbPacket,
            out IntPtr  pPackets);

        /// <summary>
        /// Managed wrapper for GetLastSystemEventData
        /// </summary>
        /// <param name="commHandle">Specifies PimcContext object handle to get event data on.</param>
        /// <param name="evt">ID of system event that was triggered.</param>
        /// <param name="modifier">keyboar modifier (unused).</param>
        /// <param name="key">Keyboard key (unused).</param>
        /// <param name="x">X position in device units of gesture.</param>
        /// <param name="y">Y position in device units of gesture.</param>
        /// <param name="cursorMode">Mode of the cursor.</param>
        /// <param name="buttonState">State of stylus buttons (flick returns custom data in this).</param>
        /// <returns>true if succeeded, false if failed.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetLastSystemEventData(
            IntPtr      commHandle,
            out int     evt,
            out int     modifier,
            out int     key,
            out int     x,
            out int     y,
            out int     cursorMode,
            out int     buttonState);

        /// <summary>
        /// Managed wrapper for CreateResetEvent
        /// </summary>
        /// <param name="handle">Win32 event handle created.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateResetEvent(out IntPtr handle);

        /// <summary>
        /// Managed wrapper for DestroyResetEvent
        /// </summary>
        /// <param name="handle">Win32 event handle to destroy.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyResetEvent(IntPtr handle);

        /// <summary>
        /// Managed wrapper for RaiseResetEvent
        /// </summary>
        /// <param name="handle">Win32 event handle to set.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RaiseResetEvent(IntPtr handle);

        /// <summary>
        /// Managed wrapper for LockObjectExtFromGit
        /// </summary>
        /// <param name="gitKey">The key used to refer to this object in the GIT.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LockWispObjectFromGit(UInt32 gitKey);

        /// <summary>
        /// Managed wrapper for UnlockObjectExtFromGit
        /// </summary>
        /// <param name="gitKey">The key used to refer to this object in the GIT.</param>
        /// <returns>true if succeeded, false if failed.</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Penimc, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnlockWispObjectFromGit(UInt32 gitKey);

        /// <summary>
        /// Managed wrapper for CoCreateInstance
        /// </summary>
        /// <param name="clsid">CLSID of the COM class to be instantiated</param>
        /// <param name="punkOuter">Aggregate object</param>
        /// <param name="context">Context in which the newly created object will run</param>
        /// <param name="iid">Identifier of the Interface</param>
        /// <returns>Returns the COM object created by CoCreateInstance</returns>
        /// <SecurityNote>
        /// Critical as suppressing UnmanagedCodeSecurity
        /// </SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Interface)][DllImport(ExternDll.Ole32, ExactSpelling=true, PreserveSig=false)]
        private static extern object CoCreateInstance(
            [In]
            ref Guid clsid,
            [MarshalAs(UnmanagedType.Interface)]
            object punkOuter,
            int context,
            [In]
            ref Guid iid);
    }

#if OLD_ISF
    internal class CompressorSafeHandle: SafeHandle
    {
        /// <SecurityNote>
        /// Critical: This code calls into a base class which is protected by link demand and by inheritance demand
        /// </SecurityNote>
        [SecurityCritical]
        private CompressorSafeHandle() 
            : this(true)
        {
        }

        /// <SecurityNote>
        /// Critical: This code calls into a base class which is protected by link demand and by inheritance demand
        /// </SecurityNote>
        [SecurityCritical]
        private CompressorSafeHandle(bool ownHandle) 
            : base(IntPtr.Zero, ownHandle)
        {
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.
        /// <SecurityNote>
        /// Critical:This code calls into a base class which is protected by link demand an by inheritance demand
        /// TreatAsSafe: It's safe to give out a boolean value.
        /// </SecurityNote>
        public override bool IsInvalid
        {
            [SecurityCritical, SecurityTreatAsSafe]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return IsClosed || handle == IntPtr.Zero;
            }
        }

        /// <SecurityNote>
        /// Critical: This code calls into a base class which is protected by link demand and by inheritance demand.
        /// </SecurityNote>
        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        override protected bool ReleaseHandle()
        {
            //
            // return code from this is void. 
            // internally it just calls delete on 
            // the compressor pointer
            //
            UnsafeNativeMethods.IsfReleaseCompressor(handle);
            handle = IntPtr.Zero;
            return true;
        }
    
        /// <SecurityNote>
        /// Critical: This code calls into a base class which is protected by link demand and by inheritance demand.
        /// </SecurityNote>
        public static CompressorSafeHandle Null
        {
            [SecurityCritical]
            get
            {
              return new CompressorSafeHandle(false);
            }
        }
    }
#endif
}
