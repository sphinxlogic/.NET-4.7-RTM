//---------------------------------------------------------------------------
//
// <copyright file="SafeProcessHandle.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// Description:
//              
// History:  
//  10/04/2003 : Microsoft    Created
//---------------------------------------------------------------------------

using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Automation;
using Microsoft.Win32.SafeHandles;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // This constructor is used by the P/Invoke marshaling layer
        // to allocate a SafeHandle instance.  P/Invoke then does the
        // appropriate method call, storing the handle in this class.
        private SafeProcessHandle() : base(true) {}

        internal SafeProcessHandle(IntPtr hwnd) : base(true)
        {
            uint processId;

            if (hwnd == IntPtr.Zero)
            {
                processId = UnsafeNativeMethods.GetCurrentProcessId();
            }
            else
            {
                // Get process id...
                Misc.GetWindowThreadProcessId(hwnd, out processId);
            }

            // handle might be used to query for Wow64 information (_QUERY_), or to do cross-process allocs (VM_*)
            SetHandle(Misc.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE, false, processId, hwnd));
        }

        // Uncomment this if & only if we need a constructor 
        // that takes a handle from external code
        //internal SafeProcessHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        //{
        //    SetHandle(preexistingHandle);
        //}

        // 



        protected override bool ReleaseHandle()
        {
            return Misc.CloseHandle(handle);
        }
    }
}
