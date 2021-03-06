//------------------------------------------------------------------------------
// <copyright file="System.Drawing.IntNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

#if Microsoft_NAMESPACE
namespace System.Windows.Forms.Internal
#elif DRAWING_NAMESPACE
namespace System.Drawing.Internal
#else
namespace System.Experimental.Gdi
#endif
{
    using System;   
    using System.Internal;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Runtime.Versioning;

    /// <devdoc>
    ///   This is an extract of the System.Drawing IntNativeMethods in the CommonUI tree.
    ///   This is done to be able to compile the GDI code in both assemblies System.Drawing
    ///   and System.Windows.Forms.
    /// </devdoc>
    [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
#if Microsoft_PUBLIC_GRAPHICS_LIBRARY
    public
#else
    internal
#endif
    static partial class IntSafeNativeMethods 
    {
        public sealed class CommonHandles 
        {
            static CommonHandles(){}
            
            /// <devdoc>
            ///     Handle type for enhanced metafiles.
            /// </devdoc>
            public static readonly int EMF = System.Internal.HandleCollector.RegisterType("EnhancedMetaFile", 20, 500);

            /// <devdoc>
            ///     Handle type for GDI objects.
            /// </devdoc>
            public static readonly int GDI = System.Internal.HandleCollector.RegisterType("GDI", 90, 50);

            /// <devdoc>
            ///     Handle type for HDC's that count against the Win98 limit of five DC's.  HDC's
            ///     which are not scarce, such as HDC's for bitmaps, are counted as GDIHANDLE's.
            /// </devdoc>
            public static readonly int HDC = System.Internal.HandleCollector.RegisterType("HDC", 100, 2); // wait for 2 dc's before collecting
        }
       
        // Brush.

        [DllImport(ExternDll.Gdi32, SetLastError=true, ExactSpelling = true, EntryPoint = "CreateSolidBrush", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.Process)]
        private static extern IntPtr IntCreateSolidBrush(int crColor);
        [ResourceExposure(ResourceScope.Process)]
        [ResourceConsumption(ResourceScope.Process)]
        public static IntPtr CreateSolidBrush(int crColor) 
        {
            IntPtr hBrush = System.Internal.HandleCollector.Add(IntCreateSolidBrush(crColor), IntSafeNativeMethods.CommonHandles.GDI);
            DbgUtil.AssertWin32(hBrush != IntPtr.Zero, "IntCreateSolidBrush(color={0}) failed.", crColor);
            return hBrush;
        }

        // Pen.

        [DllImport(ExternDll.Gdi32, SetLastError=true, ExactSpelling = true, EntryPoint = "CreatePen", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.Process)]
        private static extern IntPtr IntCreatePen(int fnStyle, int nWidth, int crColor);
        [ResourceExposure(ResourceScope.Process)]
        [ResourceConsumption(ResourceScope.Process)]
        public static IntPtr CreatePen(int fnStyle, int nWidth, int crColor)
        {
            IntPtr hPen = System.Internal.HandleCollector.Add(IntCreatePen(fnStyle, nWidth, crColor), IntSafeNativeMethods.CommonHandles.GDI);
            DbgUtil.AssertWin32(hPen != IntPtr.Zero, "IntCreatePen(style={0}, width={1}, color=[{2}]) failed.", fnStyle, nWidth, crColor);
            return hPen;
        }

        [DllImport(ExternDll.Gdi32, SetLastError=true, ExactSpelling = true, EntryPoint = "ExtCreatePen", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.Process)]
        private static extern IntPtr IntExtCreatePen(int fnStyle, int dwWidth, IntNativeMethods.LOGBRUSH lplb, int dwStyleCount, [MarshalAs(UnmanagedType.LPArray)] int[] lpStyle);
        [ResourceExposure(ResourceScope.Process)]
        [ResourceConsumption(ResourceScope.Process)]
        public static IntPtr ExtCreatePen(int fnStyle, int dwWidth, IntNativeMethods.LOGBRUSH lplb, int dwStyleCount, int[] lpStyle)
        {
            IntPtr hPen = System.Internal.HandleCollector.Add(IntExtCreatePen(fnStyle, dwWidth, lplb, dwStyleCount, lpStyle), IntSafeNativeMethods.CommonHandles.GDI);
            DbgUtil.AssertWin32(hPen != IntPtr.Zero, "IntExtCreatePen(style={0}, width={1}, brush={2}, styleCount={3}, styles={4}) failed.", fnStyle, dwWidth, lplb, dwStyleCount, lpStyle);
            return hPen;
        }

        // Region

        [DllImport(ExternDll.Gdi32, SetLastError=true, ExactSpelling=true, EntryPoint="CreateRectRgn", CharSet=System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern IntPtr IntCreateRectRgn(int x1, int y1, int x2, int y2);
        [ResourceExposure(ResourceScope.Process)]
        [ResourceConsumption(ResourceScope.Process)]
        public static IntPtr CreateRectRgn(int x1, int y1, int x2, int y2) 
        {
            IntPtr hRgn = System.Internal.HandleCollector.Add(IntCreateRectRgn(x1, y1, x2, y2), IntSafeNativeMethods.CommonHandles.GDI);
            DbgUtil.AssertWin32(hRgn != IntPtr.Zero, "IntCreateRectRgn([x1={0}, y1={1}, x2={2}, y2={3}]) failed.", x1, y1, x2, y2);
            return hRgn;
        }

        // Misc.
           
        [DllImport(ExternDll.Kernel32, SetLastError=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.AppDomain)]
        public static extern int GetUserDefaultLCID();
        
        [DllImport(ExternDll.Gdi32, SetLastError=true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        public static extern bool GdiFlush();
    }
}

