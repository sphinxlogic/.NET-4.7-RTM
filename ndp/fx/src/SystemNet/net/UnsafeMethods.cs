//------------------------------------------------------------------------------
// <copyright file="UnsafeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace System.Net
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Win32.SafeHandles;
    using System.Security;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Threading;
    using System.Net.Sockets;

    [Flags]
    internal enum FormatMessageFlags : uint
    {
        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
        //FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
        //FORMAT_MESSAGE_FROM_STRING = 0x00000400,
        FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
        FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000
    }

    [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
    internal static class UnsafeSystemNativeMethods
    {
        private const string KERNEL32 = "KERNEL32.dll";

        [System.Security.SecurityCritical]
        [DllImport(KERNEL32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe SafeLoadLibrary LoadLibraryExW( string lpwLibFileName, [In] void* hFile,  uint dwFlags);

        [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool FreeLibrary(IntPtr hModule);

        [System.Security.SecurityCritical]
        [DllImport(KERNEL32, ExactSpelling=true, CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern IntPtr GetModuleHandleW(string modName);

        [System.Security.SecurityCritical]
        [DllImport(KERNEL32, EntryPoint = "GetProcAddress", SetLastError = true, BestFitMapping = false)]
        internal extern static IntPtr GetProcAddress(SafeLoadLibrary hModule, string entryPoint);

        [System.Security.SecurityCritical]
        [DllImport(KERNEL32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal extern static IntPtr GetProcAddress(IntPtr hModule, string entryPoint);

        [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
        internal extern static uint FormatMessage(
                                            FormatMessageFlags dwFlags, 
                                            IntPtr lpSource, 
                                            UInt32 dwMessageId, 
                                            UInt32 dwLanguageId, 
                                            ref IntPtr lpBuffer,
                                            UInt32 nSize,
                                            IntPtr vaArguments
            );

        [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
        internal extern static uint LocalFree(IntPtr lpMem);

       }

    // <SecurityKernel Critical="True" Ring="0">
    // <SatisfiesLinkDemand Name="SafeHandleZeroOrMinusOneIsInvalid" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Design", 
        "CA1031:DoNotCatchGeneralExceptionTypes", 
        Justification = "We need to ensure that this class initializes in any condition.")]
    [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
    internal sealed class SafeLoadLibrary : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const string KERNEL32 = "kernel32.dll";
        private const string AddDllDirectory = "AddDllDirectory";
        private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        // KB2533623 introduced the LOAD_LIBRARY_SEARCH_SYSTEM32 flag. It also introduced
        // the AddDllDirectory function. We test for presence of AddDllDirectory as  
        // indirect evidence of support for the LOAD_LIBRARY_SEARCH_SYSTEM32 flag. 
        private static uint _flags = 0;

        static SafeLoadLibrary() {
            try {
                IntPtr hKernel32 = UnsafeSystemNativeMethods.GetModuleHandleW(KERNEL32);

                if (hKernel32 != IntPtr.Zero &&
                    UnsafeSystemNativeMethods.GetProcAddress(hKernel32, AddDllDirectory) != IntPtr.Zero) {
                    _flags = LOAD_LIBRARY_SEARCH_SYSTEM32;
                }
            }
            catch { /* noop to ensure this class can initialize */ }
        }

        private SafeLoadLibrary() : base(true) { }

        internal unsafe static SafeLoadLibrary LoadLibraryEx(string library) {
            SafeLoadLibrary result = UnsafeSystemNativeMethods.LoadLibraryExW(library, null, _flags);
            if (result.IsInvalid) {
                //NOTE:
                //IsInvalid tests the numeric value of the handle. 
                //SetHandleAsInvalid sets the handle as closed, so that further closing 
                //does not have to take place in the critical finalizer thread. 
                //
                //You would think that when you assign 0 or -1 to an instance of 
                //SafeHandleZeroOrMinusoneIsInvalid, the handle will not be closed, since after all it is invalid 
                //It turns out that the SafeHandleZetoOrMinusOneIsInvalid overrides only the IsInvalid() method
                //It does not do anything to automatically close it.
                //So we have to SetHandleAsInvalid --> Which means mark it closed -- so that
                //we will not eventually call CloseHandle on 0 or -1
                result.SetHandleAsInvalid();
            }
            return result;
        }
        protected override bool ReleaseHandle() {
            return UnsafeSystemNativeMethods.FreeLibrary(handle);
        }
    }
}
