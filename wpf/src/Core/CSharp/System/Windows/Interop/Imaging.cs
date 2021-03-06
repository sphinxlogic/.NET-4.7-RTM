//------------------------------------------------------------------------------
//  Microsoft Avalon
//  Copyright (c) Microsoft Corporation, All Rights Reserved.
//
//  File: Imaging.cs
//
//------------------------------------------------------------------------------
using System;
using System.Security;
using System.Security.Permissions;
using MS.Internal;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32.SafeHandles;
using MS.Internal.PresentationCore;                        // SecurityHelper
using System.Windows.Interop;

namespace System.Windows.Interop
{
    /// <summary>
    /// Managed/Unmanaged Interop for Imaging.
    /// </summary>
    public static class Imaging
    {
        /// <summary>
        /// Construct an Bitmap from a HBITMAP.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="palette"></param>
        /// <param name="sourceRect"></param>
        /// <param name="sizeOptions"></param>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        /// Critical - calls critical code, access unmanaged resources
        /// PublicOK - demands unmanaged code permission
        /// </SecurityNote>
        [SecurityCritical]
        unsafe public static BitmapSource CreateBitmapSourceFromHBitmap(
            IntPtr bitmap,
            IntPtr palette,
            Int32Rect sourceRect,
            BitmapSizeOptions sizeOptions)
        {
            SecurityHelper.DemandUnmanagedCode();

            // CR: Microsoft (1681459)
            return CriticalCreateBitmapSourceFromHBitmap(bitmap, palette, sourceRect, sizeOptions, WICBitmapAlphaChannelOption.WICBitmapUseAlpha);
        }

        /// <summary>
        /// Construct an Bitmap from a HBITMAP.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="palette"></param>
        /// <param name="sourceRect"></param>
        /// <param name="sizeOptions"></param>
        /// <param name="alphaOptions"></param>
        /// <SecurityNote>
        /// Critical - calls critical code, access unmanaged resources
        /// </SecurityNote>
        [SecurityCritical]
        unsafe internal static BitmapSource CriticalCreateBitmapSourceFromHBitmap(
            IntPtr bitmap,
            IntPtr palette,
            Int32Rect sourceRect,
            BitmapSizeOptions sizeOptions,
            WICBitmapAlphaChannelOption alphaOptions)
        {
            if (bitmap == IntPtr.Zero)
            {
                throw new ArgumentNullException("bitmap");
            }

            return new InteropBitmap(bitmap, palette, sourceRect, sizeOptions, alphaOptions); // use the critical version
        }
        
        /// <summary>
        /// Construct an Bitmap from a HICON.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="sourceRect"></param>
        /// <param name="sizeOptions"></param>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        /// Critical - calls critical code, access unmanaged resources
        /// PublicOK - demands unmanaged code permission
        /// </SecurityNote>
        [SecurityCritical ]
        unsafe public static BitmapSource CreateBitmapSourceFromHIcon(
            IntPtr icon,
            Int32Rect sourceRect,
            BitmapSizeOptions sizeOptions)
        {
            SecurityHelper.DemandUnmanagedCode();

            if (icon == IntPtr.Zero)
            {
                throw new ArgumentNullException("icon");
            }

            return new InteropBitmap(icon, sourceRect, sizeOptions);
        }

        /// <summary>
        /// Construct an Bitmap from a section handle.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="pixelWidth"></param>
        /// <param name="pixelHeight"></param>
        /// <param name="format"></param>
        /// <param name="stride"></param>
        /// <param name="offset"></param>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        /// Critical - calls critical code, access unmanaged resources
        /// PublicOK - demands unmanaged code permission
        /// </SecurityNote>
        [SecurityCritical ]
        unsafe public static BitmapSource CreateBitmapSourceFromMemorySection(
            IntPtr section,
            int pixelWidth,
            int pixelHeight,
            Media.PixelFormat format,
            int stride,
            int offset)
        {
            SecurityHelper.DemandUnmanagedCode();

            if (section == IntPtr.Zero)
            {
                throw new ArgumentNullException("section");
            }

            return new InteropBitmap(section, pixelWidth, pixelHeight, format, stride, offset);
        }

    }
}

