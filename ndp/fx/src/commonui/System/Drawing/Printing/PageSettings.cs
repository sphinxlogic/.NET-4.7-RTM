//------------------------------------------------------------------------------
// <copyright file="PageSettings.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Drawing.Printing {
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System;    
    using System.Drawing;
    using System.ComponentModel;
    using Microsoft.Win32;
    using System.Drawing.Internal;
    using System.Runtime.Versioning;

    /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies
    ///       settings that apply to a single page.
    ///    </para>
    /// </devdoc>
    [Serializable]
    public class PageSettings : ICloneable
    {
        internal PrinterSettings printerSettings;

        private TriState color = TriState.Default;
        private PaperSize paperSize;
        private PaperSource paperSource;
        private PrinterResolution printerResolution;
        private TriState landscape = TriState.Default;
        private Margins margins = new Margins();

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.PageSettings"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PageSettings'/> class using
        ///       the default printer.
        ///    </para>
        /// </devdoc>
        public PageSettings() : this(new PrinterSettings()) {
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.PageSettings1"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Drawing.Printing.PageSettings'/> class using
        ///    the specified printer.</para>
        /// </devdoc>
        public PageSettings(PrinterSettings printerSettings) {
            Debug.Assert(printerSettings != null, "printerSettings == null");
            this.printerSettings = printerSettings;
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.Bounds"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the bounds of the page, taking into account the Landscape property.
        ///    </para>
        /// </devdoc>
        public Rectangle Bounds {
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
            get {
                IntSecurity.AllPrintingAndUnmanagedCode.Assert();

                IntPtr modeHandle = printerSettings.GetHdevmode();

                Rectangle pageBounds = GetBounds(modeHandle);

                SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));
                return pageBounds;
            }
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.Color"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the page is printed in color.
        ///    </para>
        /// </devdoc>
        public bool Color {
            get {
                if (color.IsDefault)
                    return printerSettings.GetModeField(ModeField.Color, SafeNativeMethods.DMCOLOR_MONOCHROME) == SafeNativeMethods.DMCOLOR_COLOR;
                else
                    return(bool) color;
            }
            set { color = value;}
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.HardMarginX"]/*' />
        /// <devdoc>
        ///    <para>Returns the x dimension of the hard margin</para>
        /// </devdoc>
        public float HardMarginX {
            [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
            get {
                // SECREVIEW: 
                //    Its ok to Assert the permission and Let the user know the HardMarginX.
                //    This is consistent with the Bounds property.
                IntSecurity.AllPrintingAndUnmanagedCode.Assert();

                float hardMarginX = 0;
                DeviceContext dc = printerSettings.CreateDeviceContext(this);

                try
                {
                    int dpiX = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.LOGPIXELSX);
                    int hardMarginX_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.PHYSICALOFFSETX);
                    hardMarginX = hardMarginX_DU * 100 / dpiX;
                }
                finally
                {
                    dc.Dispose();
                }
                return hardMarginX;
            }
        }

        
        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.HardMarginY"]/*' />
        /// <devdoc>
        ///    <para>Returns the y dimension of the hard margin</para>
        /// </devdoc>
        public float HardMarginY {
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
            get {
                
                // SECREVIEW: 
                //    Its ok to Assert the permission and Let the user know the HardMarginY.
                //    This is consistent with the Bounds property.

                float hardMarginY = 0;
                DeviceContext dc = printerSettings.CreateDeviceContext(this);

                try {
                    int dpiY = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.LOGPIXELSY);
                    int hardMarginY_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.PHYSICALOFFSETY);
                    hardMarginY = hardMarginY_DU * 100 / dpiY;
                }
                finally {
                    dc.Dispose();
                }
                return hardMarginY;
            }
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.Landscape"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the page should be printed in landscape or portrait orientation.
        ///    </para>
        /// </devdoc>
        public bool Landscape {
            get {
                if (landscape.IsDefault)
                    return printerSettings.GetModeField(ModeField.Orientation, SafeNativeMethods.DMORIENT_PORTRAIT) == SafeNativeMethods.DMORIENT_LANDSCAPE;
                else
                    return(bool) landscape;
            }
            set { landscape = value;}
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.Margins"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating the margins for this page.
        ///       
        ///    </para>
        /// </devdoc>
        public Margins Margins {
            get { return margins;}
            set { margins = value;}
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.PaperSize"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the paper size.
        ///    </para>
        /// </devdoc>
        public PaperSize PaperSize {
            get {
                IntSecurity.AllPrintingAndUnmanagedCode.Assert();
                return GetPaperSize(IntPtr.Zero);
            }
            set { paperSize = value;}
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.PaperSource"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating the paper source (i.e. upper bin).
        ///       
        ///    </para>
        /// </devdoc>
        public PaperSource PaperSource {
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
            get {
                if (paperSource == null) {
                    IntSecurity.AllPrintingAndUnmanagedCode.Assert();

                    IntPtr modeHandle = printerSettings.GetHdevmode();
                    IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE) UnsafeNativeMethods.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                    PaperSource result = PaperSourceFromMode(mode);

                    SafeNativeMethods.GlobalUnlock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));

                    return result;
                }
                else
                    return paperSource;
            }
            set { paperSource = value;}
            }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.PrintableArea"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the PrintableArea for the printer. Units = 100ths of an inch.
        ///    </para>
        /// </devdoc>
        public RectangleF PrintableArea {
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
            get {
                RectangleF printableArea = new RectangleF();
                DeviceContext dc = printerSettings.CreateInformationContext(this);
                HandleRef hdc = new HandleRef(dc, dc.Hdc);

                try {
                    int dpiX = UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.LOGPIXELSX);
                    int dpiY = UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.LOGPIXELSY);
                    if (!this.Landscape) {
                        //
                        // Need to convert the printable area to 100th of an inch from the device units
                        printableArea.X = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETX) * 100 / dpiX;
                        printableArea.Y = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETY) * 100 / dpiY;
                        printableArea.Width = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.HORZRES) * 100 / dpiX;
                        printableArea.Height = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.VERTRES) * 100 / dpiY;
                    }
                    else {
                        //
                        // Need to convert the printable area to 100th of an inch from the device units
                        printableArea.Y = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETX) * 100 / dpiX;
                        printableArea.X = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETY) * 100 / dpiY;
                        printableArea.Height = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.HORZRES) * 100 / dpiX;
                        printableArea.Width = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.VERTRES) * 100 / dpiY;
                    }
                }
                finally {
                    dc.Dispose();
                }

                return printableArea;
            }
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.PrinterResolution"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the printer resolution for the page.
        ///    </para>
        /// </devdoc>
        public PrinterResolution PrinterResolution {
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
            get {
                if (printerResolution == null) {
                    IntSecurity.AllPrintingAndUnmanagedCode.Assert();

                    IntPtr modeHandle = printerSettings.GetHdevmode();
                    IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE) UnsafeNativeMethods.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                    PrinterResolution result = PrinterResolutionFromMode(mode);

                    SafeNativeMethods.GlobalUnlock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));

                    return result;
                }
                else
                    return printerResolution;
            }
            set {
                printerResolution = value;
            }
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.PrinterSettings"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the
        ///       associated printer settings.
        ///    </para>
        /// </devdoc>
        public PrinterSettings PrinterSettings {
            get { return printerSettings;}
            set { 
                if (value == null)
                    value = new PrinterSettings();
                printerSettings = value;
            }
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.Clone"]/*' />
        /// <devdoc>
        ///     Copies the settings and margins.
        /// </devdoc>
        public object Clone() {
            PageSettings result = (PageSettings) MemberwiseClone();
            result.margins = (Margins) margins.Clone();
            return result;
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.CopyToHdevmode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Copies the relevant information out of the PageSettings and into the handle.
        ///    </para>
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        public void CopyToHdevmode(IntPtr hdevmode) {
            IntSecurity.AllPrintingAndUnmanagedCode.Demand();

            IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(null, hdevmode));
            SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE) UnsafeNativeMethods.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

            if (color.IsNotDefault && ((mode.dmFields & SafeNativeMethods.DM_COLOR) == SafeNativeMethods.DM_COLOR))
                mode.dmColor = unchecked((short) (((bool) color) ? SafeNativeMethods.DMCOLOR_COLOR : SafeNativeMethods.DMCOLOR_MONOCHROME));
            if (landscape.IsNotDefault && ((mode.dmFields & SafeNativeMethods.DM_ORIENTATION) == SafeNativeMethods.DM_ORIENTATION))
                mode.dmOrientation = unchecked((short) (((bool) landscape) ? SafeNativeMethods.DMORIENT_LANDSCAPE : SafeNativeMethods.DMORIENT_PORTRAIT));

            if (paperSize != null) {
                
                if ((mode.dmFields & SafeNativeMethods.DM_PAPERSIZE) == SafeNativeMethods.DM_PAPERSIZE)
                {
                    mode.dmPaperSize = unchecked((short) paperSize.RawKind);
                }

                bool setWidth = false;
                bool setLength = false;

                if ((mode.dmFields & SafeNativeMethods.DM_PAPERLENGTH) == SafeNativeMethods.DM_PAPERLENGTH)
                {
                    // dmPaperLength is always in tenths of millimeter but paperSizes are in hundredth of inch .. 
                    // so we need to convert :: use PrinterUnitConvert.Convert(value, PrinterUnit.TenthsOfAMillimeter /*fromUnit*/, PrinterUnit.Display /*ToUnit*/)
                    int length = PrinterUnitConvert.Convert(paperSize.Height, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                    mode.dmPaperLength = unchecked((short)length);
                    setLength = true;
                }
                if ((mode.dmFields & SafeNativeMethods.DM_PAPERWIDTH) == SafeNativeMethods.DM_PAPERWIDTH)
                {
                    int width = PrinterUnitConvert.Convert(paperSize.Width, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                    mode.dmPaperWidth = unchecked((short)width);
                    setWidth = true;
                }

                if (paperSize.Kind == PaperKind.Custom)
                {
                    if (!setLength)
                    {
                        mode.dmFields |= SafeNativeMethods.DM_PAPERLENGTH;
                        int length = PrinterUnitConvert.Convert(paperSize.Height, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                        mode.dmPaperLength = unchecked((short)length);
                    }
                    if (!setWidth)
                    {
                        mode.dmFields |= SafeNativeMethods.DM_PAPERWIDTH;
                        int width = PrinterUnitConvert.Convert(paperSize.Width, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                        mode.dmPaperWidth = unchecked((short)width);
                    }
                }
            }

            if (paperSource != null && ((mode.dmFields & SafeNativeMethods.DM_DEFAULTSOURCE) == SafeNativeMethods.DM_DEFAULTSOURCE)) {
                mode.dmDefaultSource = unchecked((short) paperSource.RawKind);
            }

            if (printerResolution != null) {
                if (printerResolution.Kind == PrinterResolutionKind.Custom) {
                    if ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    {
                        mode.dmPrintQuality = unchecked((short) printerResolution.X);
                    }
                    if ((mode.dmFields & SafeNativeMethods.DM_YRESOLUTION) == SafeNativeMethods.DM_YRESOLUTION)
                    {
                        mode.dmYResolution = unchecked((short) printerResolution.Y);
                    }
                }
                else {
                    if ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    {
                        mode.dmPrintQuality = unchecked((short) printerResolution.Kind);
                    }
                }
            }

            Marshal.StructureToPtr(mode, modePointer, false);
             
            // It's possible this page has a DEVMODE for a different printer than the DEVMODE passed in here 
            // (Ex: occurs when Doc.DefaultPageSettings.PrinterSettings.PrinterName != Doc.PrinterSettings.PrinterName)
            // 
            // if the passed in devmode has fewer bytes than our buffer for the extrainfo, we want to skip the merge as it will cause
            // a buffer overrun
            if (mode.dmDriverExtra >= ExtraBytes) {
                int retCode = SafeNativeMethods.DocumentProperties(NativeMethods.NullHandleRef, NativeMethods.NullHandleRef, printerSettings.PrinterName, modePointer, modePointer, SafeNativeMethods.DM_IN_BUFFER | SafeNativeMethods.DM_OUT_BUFFER);
                if (retCode < 0) {
                    SafeNativeMethods.GlobalFree(new HandleRef(null, modePointer));
                }
            }
            
            SafeNativeMethods.GlobalUnlock(new HandleRef(null, hdevmode));
        }

        private short ExtraBytes {
            get {
                IntPtr modeHandle = printerSettings.GetHdevmodeInternal();
                IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(this, modeHandle));
                SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE) UnsafeNativeMethods.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                short result = mode.dmDriverExtra;

                SafeNativeMethods.GlobalUnlock(new HandleRef(this, modeHandle));
                SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));

                return result;
            }
        }


        // This function shows up big on profiles, so we need to make it fast
        internal Rectangle GetBounds(IntPtr modeHandle) {
            Rectangle pageBounds;
            PaperSize size = GetPaperSize(modeHandle);
            if (GetLandscape(modeHandle))
                pageBounds = new Rectangle(0, 0, size.Height, size.Width);
            else
                pageBounds = new Rectangle(0, 0, size.Width, size.Height);

            return pageBounds;
        }

        private bool GetLandscape(IntPtr modeHandle) {
            if (landscape.IsDefault)
                return printerSettings.GetModeField(ModeField.Orientation, SafeNativeMethods.DMORIENT_PORTRAIT, modeHandle) == SafeNativeMethods.DMORIENT_LANDSCAPE;
            else
                return(bool) landscape;
        }

        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        private PaperSize GetPaperSize(IntPtr modeHandle) {
            if (paperSize == null) {
                bool ownHandle = false;
                if (modeHandle == IntPtr.Zero) {
                    modeHandle = printerSettings.GetHdevmode();
                    ownHandle = true;
                }

                IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(null, modeHandle));
                SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE) UnsafeNativeMethods.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                PaperSize result = PaperSizeFromMode(mode);

                SafeNativeMethods.GlobalUnlock(new HandleRef(null, modeHandle));

                if (ownHandle)
                    SafeNativeMethods.GlobalFree(new HandleRef(null, modeHandle));

                return result;
            }
            else
                return paperSize;
        }

        private PaperSize PaperSizeFromMode(SafeNativeMethods.DEVMODE mode) {
            PaperSize[] sizes = printerSettings.Get_PaperSizes();
            if ((mode.dmFields & SafeNativeMethods.DM_PAPERSIZE) == SafeNativeMethods.DM_PAPERSIZE)
            {
                for (int i = 0; i < sizes.Length; i++) {
                    if ((int)sizes[i].RawKind == mode.dmPaperSize)
                        return sizes[i];
                }
            }
            return new PaperSize(PaperKind.Custom, "custom",
                                     //mode.dmPaperWidth, mode.dmPaperLength);
                                     PrinterUnitConvert.Convert(mode.dmPaperWidth, PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display),
                                     PrinterUnitConvert.Convert(mode.dmPaperLength, PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display));
        }

        private PaperSource PaperSourceFromMode(SafeNativeMethods.DEVMODE mode) {
            PaperSource[] sources = printerSettings.Get_PaperSources();
            if ((mode.dmFields & SafeNativeMethods.DM_DEFAULTSOURCE) == SafeNativeMethods.DM_DEFAULTSOURCE)
            {
                for (int i = 0; i < sources.Length; i++) {
                    // the dmDefaultSource == to the RawKind in the Papersource.. and Not the Kind...
                    // if the PaperSource is populated with CUSTOM values...
                    if (unchecked((short)sources[i].RawKind) == mode.dmDefaultSource)
                    {
                        return sources[i];
                    }
                    
                }
            }
            return new PaperSource((PaperSourceKind) mode.dmDefaultSource, "unknown");
        }

        private PrinterResolution PrinterResolutionFromMode(SafeNativeMethods.DEVMODE mode) {
            PrinterResolution[] resolutions = printerSettings.Get_PrinterResolutions();
            for (int i = 0; i < resolutions.Length; i++) {
                if (mode.dmPrintQuality >= 0 && ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    && ((mode.dmFields & SafeNativeMethods.DM_YRESOLUTION) == SafeNativeMethods.DM_YRESOLUTION)) {
                    if (resolutions[i].X == unchecked((int)(PrinterResolutionKind) mode.dmPrintQuality)
                        && resolutions[i].Y == unchecked((int)(PrinterResolutionKind) mode.dmYResolution))
                        return resolutions[i];
                }
                else {
                    if ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    {
                        if (resolutions[i].Kind == (PrinterResolutionKind) mode.dmPrintQuality)
                            return resolutions[i];
                    }
                }
            }
            return new PrinterResolution(PrinterResolutionKind.Custom,
                                         mode.dmPrintQuality, mode.dmYResolution);
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.SetHdevmode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Copies the relevant information out of the handle and into the PageSettings.
        ///    </para>
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        public void SetHdevmode(IntPtr hdevmode) {
            // SECREVIEW: 
            // PrinterSettings.SetHdevmode demand AllPrintingANDUMC so lets be consistent here.
            IntSecurity.AllPrintingAndUnmanagedCode.Demand();
            if (hdevmode == IntPtr.Zero)
                throw new ArgumentException(SR.GetString(SR.InvalidPrinterHandle, hdevmode));

            IntPtr pointer = SafeNativeMethods.GlobalLock(new HandleRef(null, hdevmode));
            SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE) UnsafeNativeMethods.PtrToStructure(pointer, typeof(SafeNativeMethods.DEVMODE));

            if ((mode.dmFields & SafeNativeMethods.DM_COLOR) == SafeNativeMethods.DM_COLOR)
            {
                color = (mode.dmColor == SafeNativeMethods.DMCOLOR_COLOR);
            }

            if ((mode.dmFields & SafeNativeMethods.DM_ORIENTATION) == SafeNativeMethods.DM_ORIENTATION)
            {
                landscape = (mode.dmOrientation == SafeNativeMethods.DMORIENT_LANDSCAPE);
            }
            
            paperSize = PaperSizeFromMode(mode);
            paperSource = PaperSourceFromMode(mode);
            printerResolution = PrinterResolutionFromMode(mode);

            SafeNativeMethods.GlobalUnlock(new HandleRef(null, hdevmode));
        }

        /// <include file='doc\PageSettings.uex' path='docs/doc[@for="PageSettings.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Provides some interesting information about the PageSettings in
        ///       String form.
        ///    </para>
        /// </devdoc>
        public override string ToString() {
            return "[PageSettings:"
            + " Color=" + Color.ToString()
            + ", Landscape=" + Landscape.ToString()
            + ", Margins=" + Margins.ToString()
            + ", PaperSize=" + PaperSize.ToString()
            + ", PaperSource=" + PaperSource.ToString()
            + ", PrinterResolution=" + PrinterResolution.ToString()
            + "]";
        }
    }
}

