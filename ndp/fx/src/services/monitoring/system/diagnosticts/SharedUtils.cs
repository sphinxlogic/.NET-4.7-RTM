//------------------------------------------------------------------------------
// <copyright file="SharedUtils.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Diagnostics {
    using System.Security.Permissions;
    using System.Security;    
    using System.Threading;
    using System.Text;
    using Microsoft.Win32;
    using System.Globalization;
    using System.ComponentModel;
    using System.Security.Principal;
    using System.Security.AccessControl;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics.CodeAnalysis;
    
    internal static class SharedUtils {
        
        internal const int UnknownEnvironment = 0;
        internal const int W2kEnvironment = 1;
        internal const int NtEnvironment = 2;
        internal const int NonNtEnvironment = 3;        
        private static volatile int environment = UnknownEnvironment;                

        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject {
            get {
                if (s_InternalSyncObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        internal static Win32Exception CreateSafeWin32Exception() {
            return CreateSafeWin32Exception(0);
        }

        internal static Win32Exception CreateSafeWin32Exception(int error) {
            Win32Exception newException = null;
            // Need to assert SecurtiyPermission, otherwise Win32Exception
            // will not be able to get the error message. At this point the right
            // permissions have already been demanded.
            SecurityPermission securityPermission = new SecurityPermission(PermissionState.Unrestricted);
            securityPermission.Assert();
            try {
                if (error == 0)
                    newException = new Win32Exception();
                else
                    newException = new Win32Exception(error);
            }
            finally {
                SecurityPermission.RevertAssert();
            }

            return newException;
        }

        internal static int CurrentEnvironment {
            get {
                if (environment == UnknownEnvironment) { 
                    lock (InternalSyncObject) {
                        if (environment == UnknownEnvironment) {
                            // Need to assert Environment permissions here
                            // the environment check is not exposed as a public method                        
                            if (Environment.OSVersion.Platform == PlatformID.Win32NT)  {
                                if (Environment.OSVersion.Version.Major >= 5)
                                    environment = W2kEnvironment; 
                                else
                                    environment = NtEnvironment; 
                            }                                
                            else                    
                                environment = NonNtEnvironment;
                        }                
                    }
                }
            
                return environment;                        
            }                
        }               
                        
        internal static void CheckEnvironment() {            
            if (CurrentEnvironment == NonNtEnvironment)
                throw new PlatformNotSupportedException(SR.GetString(SR.WinNTRequired));
        }

        internal static void CheckNtEnvironment() {            
            if (CurrentEnvironment == NtEnvironment)
                throw new PlatformNotSupportedException(SR.GetString(SR.Win2000Required));
        }
        
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static void EnterMutex(string name, ref Mutex mutex) {
            string mutexName = null;
            if (CurrentEnvironment == W2kEnvironment)
                mutexName = "Global\\" +  name; 
            else
                mutexName = name;

            EnterMutexWithoutGlobal(mutexName, ref mutex);
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        [SecurityPermission(SecurityAction.Assert, ControlPrincipal = true)]
        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts", Justification = "Microsoft: We pass fixed data into sec.AddAccessRule")]
        internal static void EnterMutexWithoutGlobal(string mutexName, ref Mutex mutex) {
            bool createdNew;
            MutexSecurity sec = new MutexSecurity();
            SecurityIdentifier everyoneSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            sec.AddAccessRule(new MutexAccessRule(everyoneSid, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));

            Mutex tmpMutex = new Mutex(false, mutexName, out createdNew, sec);

            SafeWaitForMutex(tmpMutex, ref mutex);
        }

        // We need to atomically attempt to acquire the mutex and record whether we took it (because we require thread affinity
        // while the mutex is held and the two states must be kept in lock step). We can get atomicity with a CER, but we don't want
        // to hold a CER over a call to WaitOne (this could cause deadlocks). The ---- is to provide a new API out of
        // mscorlib that performs the wait and lets us know if it succeeded. But at this late stage we don't want to expose a new
        // API out of mscorlib, so we'll build our own solution.
        // We'll P/Invoke out to the WaitForSingleObject inside a CER, but use a timeout to ensure we can't block a thread abort for
        // an unlimited time (we do this in an infinite loop so the semantic of acquiring the mutex is unchanged, the timeout is
        // just to allow us to poll for abort). A limitation of CERs in Whidbey (and part of the problem that put us in this
        // position in the first place) is that a CER root in a method will cause the entire method to delay thread aborts. So we
        // need to carefully partition the real CER part of out logic in a sub-method (and ensure the jit doesn't inline on us).
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static bool SafeWaitForMutex(Mutex mutexIn, ref Mutex mutexOut)
        {
            Debug.Assert(mutexOut == null, "You must pass in a null ref Mutex");

            // Wait as long as necessary for the mutex.
            while (true) {

                // Attempt to acquire the mutex but timeout quickly if we can't.
                if (!SafeWaitForMutexOnce(mutexIn, ref mutexOut))
                    return false;
                if (mutexOut != null)
                    return true;

                // We come out here to the outer method every so often so we're not in a CER and a thread abort can interrupt us.
                // But the abort logic itself is poll based (in the this case) so we really need to check for a pending abort
                // explicitly else the two timing windows will virtually never line up and we'll still end up stalling the abort
                // attempt. Thread.Sleep checks for pending abort for us.
                Thread.Sleep(0);
            }
        }

        // The portion of SafeWaitForMutex that runs under a CER and thus must not block for a arbitrary period of time.
        // This method must not be inlined (to stop the CER accidently spilling into the calling method).
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static bool SafeWaitForMutexOnce(Mutex mutexIn, ref Mutex mutexOut)
        {
            bool ret;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {} finally {
                // Wait for the mutex for half a second (long enough to gain the mutex in most scenarios and short enough to avoid
                // impacting a thread abort for too long).
                // Holding a mutex requires us to keep thread affinity and announce ourselves as a critical region.
                Thread.BeginCriticalRegion();
                Thread.BeginThreadAffinity();
                int result = WaitForSingleObjectDontCallThis(mutexIn.SafeWaitHandle, 500);
                switch (result) {

                case NativeMethods.WAIT_OBJECT_0:
                case NativeMethods.WAIT_ABANDONED:
                    // Mutex was obtained, atomically record that fact.
                    mutexOut = mutexIn;
                    ret = true;
                    break;

                case NativeMethods.WAIT_TIMEOUT:
                    // Couldn't get mutex yet, simply return and we'll try again later.
                    ret = true;
                    break;

                default:
                    // Some sort of failure return immediately all the way to the caller of SafeWaitForMutex.
                    ret = false;
                    break;
                }

                // If we're not leaving with the Mutex we don't require thread affinity and we're not a critical region any more.
                if (mutexOut == null) {
                    Thread.EndThreadAffinity();
                    Thread.EndCriticalRegion();
                }
            }

            return ret;
        }

        // P/Invoke for the methods above. Don't call this from anywhere else.
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(ExternDll.Kernel32, ExactSpelling=true, SetLastError=true, EntryPoint="WaitForSingleObject")]
        private static extern int WaitForSingleObjectDontCallThis(SafeWaitHandle handle, int timeout);

        [ResourceExposure(ResourceScope.Machine)]  // This is scoped to a Fx build dir.
        [ResourceConsumption(ResourceScope.Machine)]
            // What if an app is locked back?  Why would we use this?
        internal static string GetLatestBuildDllDirectory(string machineName) {
            string dllDir = "";
            RegistryKey baseKey = null;
            RegistryKey complusReg = null;
            
            //This property is retrieved only when creationg a new category,
            //                          the calling code already demanded PerformanceCounterPermission.
            //                          Therefore the assert below is safe.
                                                
            //This property is retrieved only when creationg a new log,
            //                          the calling code already demanded EventLogPermission.
            //                          Therefore the assert below is safe.

            RegistryPermission registryPermission = new RegistryPermission(PermissionState.Unrestricted);
            registryPermission.Assert();

            try {
                if (machineName.Equals(".")) {
                    return GetLocalBuildDirectory();
                }
                else {
                    baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName);
                }
                if (baseKey == null)
                    throw new InvalidOperationException(SR.GetString(SR.RegKeyMissingShort, "HKEY_LOCAL_MACHINE", machineName));

                complusReg = baseKey.OpenSubKey("SOFTWARE\\Microsoft\\.NETFramework");
                if (complusReg != null) {
                    string installRoot = (string)complusReg.GetValue("InstallRoot");
                    if (installRoot != null && installRoot != String.Empty) {
                        // the "policy" subkey contains a v{major}.{minor} subkey for each version installed.  There are also
                        // some extra subkeys like "standards" and "upgrades" we want to ignore.

                        // first we figure out what version we are...
                        string versionPrefix = "v" + Environment.Version.Major + "." + Environment.Version.Minor;
                        RegistryKey policyKey = complusReg.OpenSubKey("policy");

                        // This is the full version string of the install on the remote machine we want to use (for example "v2.0.50727")
                        string version = null;

                        if (policyKey != null) {
                            try {
                                
                                // First check to see if there is a version of the runtime with the same minor and major number:
                                RegistryKey bestKey = policyKey.OpenSubKey(versionPrefix);

                                if (bestKey != null) {
                                    try {
                                        version = versionPrefix + "." + GetLargestBuildNumberFromKey(bestKey);
                                    } finally {
                                        bestKey.Close();
                                    }
                                } else {
                                    // There isn't an exact match for our version, so we will look for the largest version
                                    // installed.
                                    string[] majorVersions = policyKey.GetSubKeyNames();
                                    int[] largestVersion = new int[] { -1, -1, -1 };
                                    for (int i = 0; i < majorVersions.Length; i++) {

                                        string majorVersion = majorVersions[i];

                                        // If this looks like a key of the form v{something}.{something}, we should see if it's a usable build.
                                        if (majorVersion.Length > 1 && majorVersion[0] == 'v' && majorVersion.Contains(".")) {
                                            int[] currentVersion = new int[] { -1, -1, -1 };

                                            string[] splitVersion = majorVersion.Substring(1).Split('.');

                                            if(splitVersion.Length != 2) {
                                                continue;
                                            }

                                            if (!Int32.TryParse(splitVersion[0], out currentVersion[0]) || !Int32.TryParse(splitVersion[1], out currentVersion[1])) {
                                                continue;
                                            }

                                            RegistryKey k = policyKey.OpenSubKey(majorVersion);
                                            if (k == null) {
                                                // We may be able to use another subkey
                                                continue;
                                            }
                                            try {
                                                currentVersion[2] = GetLargestBuildNumberFromKey(k);

                                                if (currentVersion[0] > largestVersion[0]
                                                    || ((currentVersion[0] == largestVersion[0]) && (currentVersion[1] > largestVersion[1]))) {
                                                    largestVersion = currentVersion;
                                                }
                                            } finally {
                                                k.Close();
                                            }
                                        }
                                    }

                                    version = "v" + largestVersion[0] + "." + largestVersion[1] + "." + largestVersion[2];
                                }
                            } finally {
                                policyKey.Close();
                            }

                            if (version != null && version != String.Empty) {
                                StringBuilder installBuilder = new StringBuilder();
                                installBuilder.Append(installRoot);
                                if (!installRoot.EndsWith("\\", StringComparison.Ordinal))
                                    installBuilder.Append("\\");
                                installBuilder.Append(version);
                                dllDir = installBuilder.ToString();
                            }
                        }
                    }
                }                                      
            }
            catch {
                // ignore
            }
            finally {
                if (complusReg != null)
                    complusReg.Close();

                if (baseKey != null)
                    baseKey.Close();

                RegistryPermission.RevertAssert();                             
            }

            return dllDir;
        }                

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static int GetLargestBuildNumberFromKey(RegistryKey rootKey) {
            int largestBuild = -1;

            string[] minorVersions = rootKey.GetValueNames();
            for (int i = 0; i < minorVersions.Length; i++) {
                int o;
                if (Int32.TryParse(minorVersions[i], out o)) {
                    largestBuild = (largestBuild > o) ? largestBuild : o;
                }
            }

            return largestBuild;
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static string GetLocalBuildDirectory() {
            return RuntimeEnvironment.GetRuntimeDirectory();
        }
    }
}   
