//---------------------------------------------------------------------------
//
// <copyright file="SafeCoTaskMem.cs" company="Microsoft">
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
using Microsoft.Win32.SafeHandles;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    internal sealed class SafeCoTaskMem : SafeHandleZeroOrMinusOneIsInvalid
    {
        // This constructor is used by the P/Invoke marshaling layer
        // to allocate a SafeHandle instance.  P/Invoke then does the
        // appropriate method call, storing the handle in this class.
        private SafeCoTaskMem() : base(true) {}

        internal SafeCoTaskMem(int length) : base(true)
        {
            SetHandle(Marshal.AllocCoTaskMem(length * sizeof (char)));
        }

        internal string GetStringAuto()
        {
            return Marshal.PtrToStringAuto(handle);
        }

        internal string GetStringUni(int length)
        {
            // Convert the local unmanaged buffer in to a string object
            return Marshal.PtrToStringUni(handle, length);
        }

        // 



        protected override bool ReleaseHandle()
        {
            Marshal.FreeCoTaskMem(handle);
            return true;
        }
    }
}
