//---------------------------------------------------------------------------
//
// <copyright file="SafeThemeHandle.cs" company="Microsoft">
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
//using System.Runtime.CompilerServices;
using System.Windows.Automation;
using Microsoft.Win32.SafeHandles;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    internal sealed class SafeThemeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // This constructor is used by the P/Invoke marshaling layer
        // to allocate a SafeHandle instance.  P/Invoke then does the
        // appropriate method call, storing the handle in this class.
        private SafeThemeHandle() : base(true) {}

        // Uncomment this if & only if we need a constructor 
        // that takes a handle from external code
        internal SafeThemeHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        // 



        protected override bool ReleaseHandle()
        {
            // MustRun methods may only call other MustRun methods,
            // must not allocate along paths that must succeed, etc.
            return !IsInvalid ? CloseThemeData(handle) == (IntPtr)NativeMethods.S_OK : true;
        }

        [DllImport("UxTheme.dll", CharSet = CharSet.Auto)/*, SuppressUnmanagedCodeSecurity*/]
        // 



        private static extern IntPtr CloseThemeData(IntPtr handle);
    }
}
