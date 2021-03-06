// #define CUSTOM_MARSHALING_ISTREAM

//------------------------------------------------------------------------------
// <copyright file="Icon.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing {
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using Microsoft.Win32;
    using System.Drawing.Design;    
    using System.Drawing.Imaging;    
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Drawing.Internal;
    using System.Runtime.Versioning;

    /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon"]/*' />
    /// <devdoc>
    ///     This class represents a Windows icon, which is a small bitmap image used to
    ///     represent an object.  Icons can be thought of as transparent bitmaps, although
    ///     their size is determined by the system.
    /// </devdoc>
    [
    TypeConverterAttribute(typeof(IconConverter)),
    Editor("System.Drawing.Design.IconEditor, " + AssemblyRef.SystemDrawingDesign, typeof(UITypeEditor))
    ]
    [Serializable]
    public sealed class Icon : MarshalByRefObject, ISerializable, ICloneable, IDisposable {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        private static int bitDepth;

        // The PNG signature is specified at:
        // http://www.w3.org/TR/PNG/#5PNG-file-signature
        private const int PNGSignature1 = 137 + ('P' << 8) + ('N' << 16) + ('G' << 24);
        private const int PNGSignature2 = 13 + (10 << 8) + (26 << 16) + (10 << 24);

        // Icon data
        //
        private byte[] iconData;
        private int bestImageOffset;
        private int bestBitDepth;
        private int bestBytesInRes;
        private bool?  isBestImagePng = null;
        private Size   iconSize = System.Drawing.Size.Empty;
        private IntPtr handle = IntPtr.Zero;
        private bool   ownHandle = true;

        private Icon() {
        }

        internal Icon(IntPtr handle) : this(handle, false) {
        }

        internal Icon(IntPtr handle, bool takeOwnership) {
            if (handle == IntPtr.Zero) {
                throw new ArgumentException(SR.GetString(SR.InvalidGDIHandle, (typeof(Icon)).Name));
            }
            this.handle = handle;
            this.ownHandle = takeOwnership;
        }

        

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Icon"]/*' />
        /// <devdoc>
        ///     Loads an icon object from the given filename.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Icon(string fileName) : this(fileName, 0,0) {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Icon(string fileName, Size size) : this(fileName, size.Width, size.Height) {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Icon(string fileName, int width, int height) : this() {
            // SECREVIEW : This FileStream constructor demands FileIOPermission (FileIOPermissionAccess) for reading, writing, 
            //             and appending to files so we don't need to set a demand here.
            //
            using (FileStream f = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                Debug.Assert(f != null, "File.OpenRead returned null instead of throwing an exception");
                iconData = new byte[(int)f.Length];
                f.Read(iconData, 0, iconData.Length);
            }
            
            Initialize(width, height);
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Icon1"]/*' />
        /// <devdoc>
        ///     Duplicates the given icon, attempting to find a version of the icon
        ///     that matches the requested size.  If a version cannot be found that
        ///     exactally matches the size, the closest match will be used.  Note
        ///     that if original is an icon with a single size, this will
        ///     merely create a dupicate icon.  You can use the stretching modes
        ///     of drawImage to force the icon to the size you want.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Icon(Icon original, Size size) : this(original, size.Width, size.Height) {
        }
        
        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Icon2"]/*' />
        /// <devdoc>
        ///     Duplicates the given icon, attempting to find a version of the icon
        ///     that matches the requested size.  If a version cannot be found that
        ///     exactally matches the size, the closest match will be used.  Note
        ///     that if original is an icon with a single size, this will
        ///     merely create a dupicate icon.  You can use the stretching modes
        ///     of drawImage to force the icon to the size you want.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Icon(Icon original, int width, int height) : this() {
            if (original == null) {
                throw new ArgumentException(SR.GetString(SR.InvalidArgument, "original", "null"));
            }
            
            iconData = original.iconData;
            
            if (iconData == null) {
                iconSize = original.Size;
                handle = SafeNativeMethods.CopyImage(new HandleRef(original, original.Handle), SafeNativeMethods.IMAGE_ICON, iconSize.Width, iconSize.Height, 0);
            }
            else {
                Initialize(width, height);
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Icon3"]/*' />
        /// <devdoc>
        ///     Loads an icon object from the given resource.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Icon(Type type, string resource) : this() {
            Stream stream = type.Module.Assembly.GetManifestResourceStream(type, resource);
            if (stream == null) {
                throw new ArgumentException(SR.GetString(SR.ResourceNotFound, type, resource));
            }

            iconData = new byte[(int)stream.Length];
            stream.Read(iconData, 0, iconData.Length);
            Initialize(0, 0);
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Icon4"]/*' />
        /// <devdoc>
        ///     Loads an icon object from the given data stream.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Icon(Stream stream) : this(stream, 0,0) {
        }
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Icon(Stream stream, Size size) : this(stream, size.Width, size.Height) {
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Icon5"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Icon(Stream stream, int width, int height) : this() {
            if (stream == null) {
                throw new ArgumentException(SR.GetString(SR.InvalidArgument, "stream", "null"));
            }

            iconData = new byte[(int)stream.Length];
            stream.Read(iconData, 0, iconData.Length);
            Initialize(width, height);
        }

        

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Icon6"]/*' />
        /// <devdoc>
        ///     Constructor used in deserialization
        /// </devdoc>
        /// <internalonly/>        
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private Icon(SerializationInfo info, StreamingContext context) {
            iconData = (byte[])info.GetValue("IconData", typeof(byte[]));
            iconSize = (Size)info.GetValue("IconSize", typeof(Size));
            
            if (iconSize.IsEmpty) {
                Initialize(0, 0);
            }
            else {
                Initialize(iconSize.Width, iconSize.Height);
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.ExtractAssociatedIcon"]/*' />
        /// <devdoc>
        ///     Extracts an icon object from the given filename.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static Icon ExtractAssociatedIcon(string filePath) {
            return ExtractAssociatedIcon(filePath, 0);
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.ExtractAssociatedIcon"]/*' />
        /// <devdoc>
        ///     Extracts an icon object from the given filename.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static Icon ExtractAssociatedIcon(string filePath, int index) {
            if (filePath == null) {
                throw new ArgumentException(SR.GetString(SR.InvalidArgument, "filePath", "null"));
            }

            Uri uri;
            try
            {
                uri = new Uri(filePath);
            }
            catch (UriFormatException)
            {
                // It's a relative pathname, get its full path as a file. 
                // SECREVIEW : If path does exist, the caller must have FileIOPermissionAccess.PathDiscovery permission.
                //             Note that unlike most members of the Path class, this method accesses the file system.
                //
                filePath = Path.GetFullPath(filePath);
                uri = new Uri(filePath);
            }
            if (uri.IsUnc) {
                throw new ArgumentException (SR.GetString (SR.InvalidArgument, "filePath", filePath));
            }
            if (uri.IsFile)
            {
                // SECREVIEW : The File.Exists() below will do the demand for the FileIOPermission
                //             for us. So, we do not need an additional demand anymore.
                //
                if (!File.Exists(filePath)) {
                    // I have to do this so I can give a meaningful
                    // error back to the user. File.Exists() cal fail because of either
                    // a failure to demand security or because the file does not exist.
                    // Always telling the user that the file does not exist is not a good
                    // choice. So, we demand the permission again. This means that we are
                    // going to demand the permission twice for the failure case, but that's
                    // better than always demanding the permission twice.
                    //
                    IntSecurity.DemandReadFileIO(filePath);
    
                    throw new FileNotFoundException(filePath);
                }
                
                Icon icon = new Icon();
    
                StringBuilder sb = new StringBuilder(NativeMethods.MAX_PATH);
                sb.Append(filePath);
    
                IntPtr hIcon = SafeNativeMethods.ExtractAssociatedIcon(NativeMethods.NullHandleRef, sb, ref index);
    
                if (hIcon != IntPtr.Zero) {
                    IntSecurity.ObjectFromWin32Handle.Demand();
                    icon = new Icon(hIcon, true);
                    return icon;
                }
            }
            return null;
            
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Handle"]/*' />
        /// <devdoc>
        ///     The Win32 handle for this object.  This is not a copy of the handle; do
        ///     not free it.
        /// </devdoc>
        [Browsable(false)]
        public IntPtr Handle {
            // SECREVIEW : Getting the handle is ok, methods receiving a handle should demand security permissions though.
            //
            get {
                if (handle == IntPtr.Zero) {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return handle;
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Height"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [Browsable(false)]
        public int Height {
            get { return Size.Height;}
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Size"]/*' />
        /// <devdoc>
        ///     The size of this icon object.
        /// </devdoc>
        public Size Size {
            get {
                if (iconSize.IsEmpty) {
                    SafeNativeMethods.ICONINFO info = new SafeNativeMethods.ICONINFO();
                    SafeNativeMethods.GetIconInfo(new HandleRef(this, Handle), info);
                    SafeNativeMethods.BITMAP bmp = new SafeNativeMethods.BITMAP();

                    if (info.hbmColor != IntPtr.Zero) {
                        SafeNativeMethods.GetObject(new HandleRef(null, info.hbmColor), Marshal.SizeOf(typeof(SafeNativeMethods.BITMAP)), bmp);
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmColor));
                        iconSize = new Size(bmp.bmWidth, bmp.bmHeight);
                    }
                    else if (info.hbmMask != IntPtr.Zero) {
                        SafeNativeMethods.GetObject(new HandleRef(null, info.hbmMask), Marshal.SizeOf(typeof(SafeNativeMethods.BITMAP)), bmp);
                        iconSize = new Size(bmp.bmWidth, bmp.bmHeight / 2);
                    }
                    
                    if (info.hbmMask != IntPtr.Zero) {
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmMask));
                    }
                }

                return iconSize;
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Width"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [Browsable(false)]
        public int Width {
            get { return Size.Width;}
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Clone"]/*' />
        /// <devdoc>
        ///     Clones the icon object, creating a duplicate image.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public object Clone() {
            return new Icon(this, Size.Width, Size.Height);
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.DestroyHandle"]/*' />
        /// <devdoc>
        ///     Called when this object is going to destroy it's Win32 handle.  You
        ///     may override this if there is something special you need to do to
        ///     destroy the handle.  This will be called even if the handle is not
        ///     owned by this object, which is handy if you want to create a
        ///     derived class that has it's own create/destroy semantics.
        ///
        ///     The default implementation will call the appropriate Win32
        ///     call to destroy the handle if this object currently owns the
        ///     handle.  It will do nothing if the object does not currently
        ///     own the handle.
        /// </devdoc>
        internal void DestroyHandle() {
            if (ownHandle) {
                SafeNativeMethods.DestroyIcon(new HandleRef(this, handle));
                handle = IntPtr.Zero;
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Dispose"]/*' />
        /// <devdoc>
        ///     Cleans up the resources allocated by this object.  Once called, the cursor
        ///     object is no longer useful.
        /// </devdoc>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing) {
            if (handle != IntPtr.Zero) {
#if FINALIZATION_WATCH
                if (!disposing) {
                    Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
                }
#endif
                DestroyHandle();
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.DrawIcon"]/*' />
        /// <devdoc>
        ///     Draws this image to a graphics object.  The drawing command originates on the graphics
        ///     object, but a graphics object generally has no idea how to render a given image.  So,
        ///     it passes the call to the actual image.  This version crops the image to the given
        ///     dimensions and allows the user to specify a rectangle within the image to draw.
        /// </devdoc>
        // This method is way more powerful than what we expose, but I'll leave it in place.
        private void DrawIcon(IntPtr dc, Rectangle imageRect, Rectangle targetRect, bool stretch) {
            int imageX = 0;
            int imageY = 0;
            int imageWidth;
            int imageHeight;
            int targetX = 0;
            int targetY = 0;
            int targetWidth = 0;
            int targetHeight = 0;

            Size cursorSize = Size;

            // compute the dimensions of the icon, if needed
            //
            if (!imageRect.IsEmpty) {
                imageX = imageRect.X;
                imageY = imageRect.Y;
                imageWidth = imageRect.Width;
                imageHeight = imageRect.Height;
            }
            else {
                imageWidth = cursorSize.Width;
                imageHeight = cursorSize.Height;
            }

            if (!targetRect.IsEmpty) {
                targetX = targetRect.X;
                targetY = targetRect.Y;
                targetWidth = targetRect.Width;
                targetHeight = targetRect.Height;
            }
            else {
                targetWidth = cursorSize.Width;
                targetHeight = cursorSize.Height;
            }

            int drawWidth, drawHeight;
            int clipWidth, clipHeight;

            if (stretch) {
                drawWidth = cursorSize.Width * targetWidth / imageWidth;
                drawHeight = cursorSize.Height * targetHeight / imageHeight;
                clipWidth = targetWidth;
                clipHeight = targetHeight;
            }
            else {
                drawWidth = cursorSize.Width;
                drawHeight = cursorSize.Height;
                clipWidth = targetWidth < imageWidth ? targetWidth : imageWidth;
                clipHeight = targetHeight < imageHeight ? targetHeight : imageHeight;
            }

            // The ROP is SRCCOPY, so we can be simple here and take
            // advantage of clipping regions.  Drawing the cursor
            // is merely a matter of offsetting and clipping.
            //
            IntPtr hSaveRgn = SafeNativeMethods.SaveClipRgn(dc);
            try {
                SafeNativeMethods.IntersectClipRect(new HandleRef(this, dc), targetX, targetY, targetX+clipWidth, targetY+clipHeight);
                SafeNativeMethods.DrawIconEx(new HandleRef(null, dc), 
                                            targetX - imageX, 
                                            targetY - imageY,
                                            new HandleRef(this, handle), 
                                            drawWidth, 
                                            drawHeight, 
                                            0, 
                                            NativeMethods.NullHandleRef, 
                                            SafeNativeMethods.DI_NORMAL);
            }
            finally {
                SafeNativeMethods.RestoreClipRgn(dc, hSaveRgn);
            }
        }

        internal void Draw(Graphics graphics, int x, int y) {
            Size size = Size;
            Draw(graphics, new Rectangle(x, y, size.Width, size.Height));
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Draw"]/*' />
        /// <devdoc>
        ///     Draws this image to a graphics object.  The drawing command originates on the graphics
        ///     object, but a graphics object generally has no idea how to render a given image.  So,
        ///     it passes the call to the actual image.  This version stretches the image to the given
        ///     dimensions and allows the user to specify a rectangle within the image to draw.
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        internal void Draw(Graphics graphics, Rectangle targetRect) {
            Rectangle copy = targetRect;
            copy.X += (int) graphics.Transform.OffsetX;
            copy.Y += (int) graphics.Transform.OffsetY;

            WindowsGraphics wg = WindowsGraphics.FromGraphics(graphics, ApplyGraphicsProperties.Clipping);
            IntPtr dc = wg.GetHdc();

            try {
                DrawIcon(dc, Rectangle.Empty, copy, true);
            }
            finally {
                wg.Dispose();
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.DrawUnstretched"]/*' />
        /// <devdoc>
        ///     Draws this image to a graphics object.  The drawing command originates on the graphics
        ///     object, but a graphics object generally has no idea how to render a given image.  So,
        ///     it passes the call to the actual image.  This version crops the image to the given
        ///     dimensions and allows the user to specify a rectangle within the image to draw.
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        internal void DrawUnstretched(Graphics graphics, Rectangle targetRect) {
            Rectangle copy = targetRect;            
            copy.X += (int) graphics.Transform.OffsetX;
            copy.Y += (int) graphics.Transform.OffsetY;
            
            WindowsGraphics wg = WindowsGraphics.FromGraphics(graphics, ApplyGraphicsProperties.Clipping);
            IntPtr dc = wg.GetHdc();
            try {
                DrawIcon(dc, Rectangle.Empty, copy, false);
            }
            finally {
                wg.Dispose();
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Finalize"]/*' />
        /// <devdoc>
        ///     Cleans up Windows resources for this object.
        /// </devdoc>
        ~Icon() {
            Dispose(false);
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.FromHandle"]/*' />
        /// <devdoc>
        ///     Creates an icon object from a given Win32 icon handle.  The Icon object
        ///     does not claim ownership of the icon handle; you must free it when you are
        ///     done.
        /// </devdoc>
        public static Icon FromHandle(IntPtr handle) {
            IntSecurity.ObjectFromWin32Handle.Demand();
            return new Icon(handle);
        }

        private unsafe short GetShort(byte* pb)
        {
            int retval = 0;
            if (0 != (unchecked((byte)pb) & 1))
            {
                retval = *pb;                   
                pb++;
                retval = unchecked(retval | (*pb << 8));
            }
            else
            {
                retval = unchecked((int)(*(short*)pb));
            }
            return unchecked((short)retval);
        }

        private unsafe int GetInt(byte* pb)
        {
            int retval = 0;
            if (0 != (unchecked((byte)pb) & 3))
            {
                retval = *pb;                   pb++;
                retval = retval | (*pb <<  8);  pb++;
                retval = retval | (*pb << 16);  pb++;
                retval = unchecked(retval | (*pb << 24));
            }
            else
            {
                retval = *(int*)pb;
            }
            return retval;
        }


        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Initialize"]/*' />
        /// <devdoc>
        ///     Initializes this Image object.  This is identical to calling the image's
        ///     constructor with picture, but this allows non-constructor initialization,
        ///     which may be necessary in some instances.
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private unsafe void Initialize(int width, int height) {
            if (iconData == null || handle != IntPtr.Zero) {
                throw new InvalidOperationException(SR.GetString(SR.IllegalState, GetType().Name));
            }

            int icondirSize = Marshal.SizeOf(typeof(SafeNativeMethods.ICONDIR));
            if (iconData.Length < icondirSize) {
                throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
            }
            
            // Get the correct width / height
            //
            if (width == 0) {
                width = UnsafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CXICON);
            }
            
            if (height == 0) {
                height = UnsafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CYICON);
            }
            
            
            if (bitDepth == 0) {
                IntPtr dc = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
                bitDepth = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), SafeNativeMethods.BITSPIXEL);
                bitDepth *= UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), SafeNativeMethods.PLANES);
                UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, dc));
                
                // If the bitdepth is 8, make it 4.  Why?  Because windows does not
                // choose a 256 color icon if the display is running in 256 color mode
                // because of palette flicker.  
                //
                if (bitDepth == 8) bitDepth = 4;
            }
            
            fixed(byte *pbIconData = iconData) 
            {
                short idReserved  = GetShort(pbIconData);
                short idType      = GetShort(pbIconData + 2);
                short idCount     = GetShort(pbIconData + 4);
                
                if (idReserved != 0 || idType != 1 || idCount == 0) 
                {
                    throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
                }

                SafeNativeMethods.ICONDIRENTRY  EntryTemp;

                byte    bestWidth           = 0;
                byte    bestHeight          = 0;
                //int     bestBitDepth        = 0;

                byte*   pbIconDirEntry      = unchecked(pbIconData + 6);
                int     icondirEntrySize    = Marshal.SizeOf(typeof(SafeNativeMethods.ICONDIRENTRY));

                Debug.Assert((icondirEntrySize * (idCount - 1) + icondirSize) <= iconData.Length, "Illegal number of ICONDIRENTRIES");

                if ((icondirEntrySize * (idCount - 1) + icondirSize) > iconData.Length) 
                {
                    throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
                }


                for (int i = 0; i < idCount; i++) 
                {
                    //
                    // Fill in EntryTemp
                    //
                    EntryTemp.bWidth        = pbIconDirEntry[0];
                    EntryTemp.bHeight       = pbIconDirEntry[1];
                    EntryTemp.bColorCount   = pbIconDirEntry[2];
                    EntryTemp.bReserved     = pbIconDirEntry[3];
                    EntryTemp.wPlanes       = GetShort(pbIconDirEntry + 4);
                    EntryTemp.wBitCount     = GetShort(pbIconDirEntry + 6);
                    EntryTemp.dwBytesInRes  = GetInt(  pbIconDirEntry + 8);
                    EntryTemp.dwImageOffset = GetInt(  pbIconDirEntry +12);
                    //
                    //
                    //
                    bool fUpdateBestFit = false;
                    int  iconBitDepth = 0;
                    if(EntryTemp.bColorCount != 0) {
                        iconBitDepth = 4;
                        if(EntryTemp.bColorCount < 0x10) iconBitDepth = 1;
                        
                    } else {
                        iconBitDepth = EntryTemp.wBitCount;
                    }

                    // it looks like if nothing is specified at this point, bpp is 8...
                    if(iconBitDepth == 0)
                        iconBitDepth = 8; 
                    
                    //  Windows rules for specifing an icon:
                    //
                    //  1.  The icon with the closest size match.
                    //  2.  For matching sizes, the image with the closest bit depth.
                    //  3.  If there is no color depth match, the icon with the closest color depth that does not exceed the display.
                    //  4.  If all icon color depth > display, lowest color depth is chosen.
                    //  5.  color depth of > 8bpp are all equal.
                    //  6.  Never choose an 8bpp icon on an 8bpp system.
                    //
                    
                    if (0 == bestBytesInRes) 
                    {
                        fUpdateBestFit = true;
                    }
                    else 
                    {
                        int bestDelta = Math.Abs(bestWidth - width) + Math.Abs(bestHeight - height);
                        int thisDelta = Math.Abs(EntryTemp.bWidth - width) + Math.Abs(EntryTemp.bHeight - height);
                        
                        if ((thisDelta < bestDelta) ||
                            (thisDelta == bestDelta && (iconBitDepth <= bitDepth && iconBitDepth > bestBitDepth || bestBitDepth > bitDepth && iconBitDepth < bestBitDepth)))
                        {
                            fUpdateBestFit = true;
                        }
                    }

                    if (fUpdateBestFit)
                    {
                        bestWidth       = EntryTemp.bWidth;
                        bestHeight      = EntryTemp.bHeight;
                        bestImageOffset = EntryTemp.dwImageOffset;
                        bestBytesInRes  = EntryTemp.dwBytesInRes;
                        bestBitDepth    = iconBitDepth;
                    }
                    
                    pbIconDirEntry += icondirEntrySize;
                }
                
                Debug.Assert(bestImageOffset >= 0 && bestBytesInRes >= 0 && (bestImageOffset + bestBytesInRes) <= iconData.Length, "Illegal offset/length for the Icon data");

                if (bestImageOffset < 0) {
                    throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
                }

                if (bestBytesInRes < 0) {
                    throw new Win32Exception(SafeNativeMethods.ERROR_INVALID_PARAMETER);
                }

                int endOffset;
                try {
                    endOffset = checked(bestImageOffset + bestBytesInRes);
                }
                catch (OverflowException) {
                    throw new Win32Exception(SafeNativeMethods.ERROR_INVALID_PARAMETER);
                }

                if (endOffset > iconData.Length) {
                    throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
                }

                // See DevDivBugs 17509. Copying bytes into an aligned buffer if needed
                if ((bestImageOffset % IntPtr.Size) != 0) {
                    // Beginning of icon's content is misaligned
                    byte[] alignedBuffer = new byte[bestBytesInRes];
                    Array.Copy(this.iconData, bestImageOffset, alignedBuffer, 0, bestBytesInRes);
                    
                    fixed (byte *pbAlignedBuffer = alignedBuffer) {
                        handle = SafeNativeMethods.CreateIconFromResourceEx(pbAlignedBuffer, bestBytesInRes, true, 0x00030000, 0, 0, 0);
                    }
                }
                else {
                    try {
                        handle = SafeNativeMethods.CreateIconFromResourceEx(checked(pbIconData + bestImageOffset), bestBytesInRes, true, 0x00030000, 0, 0, 0);
                    }
                    catch (OverflowException) {
                        throw new Win32Exception(SafeNativeMethods.ERROR_INVALID_PARAMETER);
                    }
                }
                if (handle == IntPtr.Zero) {
                    throw new Win32Exception();
                }

#if NEVER // old code
                SafeNativeMethods.ICONDIR *pIconDir = (SafeNativeMethods.ICONDIR *)pbIconData;
                
                if (pIconDir->idReserved != 0 || pIconDir->idType != 1 || pIconDir->idCount == 0) {
                    throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
                }
                
                SafeNativeMethods.ICONDIRENTRY *pIconDirEntry = &pIconDir->idEntries;
                SafeNativeMethods.ICONDIRENTRY *pBestFit = null;
                int bestBitDepth = 0;

                int icondirEntrySize = Marshal.SizeOf(typeof(SafeNativeMethods.ICONDIRENTRY));
                
                Debug.Assert((icondirEntrySize * pIconDir->idCount) < iconData.Length, "Illegal number of ICONDIRENTRIES");

                if ((icondirEntrySize * pIconDir->idCount) >= iconData.Length) {
                    throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
                }

                for (int i = 0; i < pIconDir->idCount; i++) {
                
                    int iconBitDepth = pIconDirEntry->wPlanes * pIconDirEntry->wBitCount;
                    
                    if (iconBitDepth == 0) {
                        if (pIconDirEntry->bColorCount == 0) {
                            iconBitDepth = 16;
                        }
                        else {
                            iconBitDepth = 8;
                            if (pIconDirEntry->bColorCount < 0xFF) iconBitDepth = 4;
                            if (pIconDirEntry->bColorCount < 0x10) iconBitDepth = 2;
                        }
                    }
                    
                    //  Windows rules for specifing an icon:
                    //
                    //  1.  The icon with the closest size match.
                    //  2.  For matching sizes, the image with the closest bit depth.
                    //  3.  If there is no color depth match, the icon with the closest color depth that does not exceed the display.
                    //  4.  If all icon color depth > display, lowest color depth is chosen.
                    //  5.  color depth of > 8bpp are all equal.
                    //  6.  Never choose an 8bpp icon on an 8bpp system.
                    //
                    
                    if (pBestFit == null) {
                        pBestFit = pIconDirEntry;
                        bestBitDepth = iconBitDepth;
                    }
                    else {
                        int bestDelta = Math.Abs(pBestFit->bWidth - width) + Math.Abs(pBestFit->bHeight - height);
                        int thisDelta = Math.Abs(pIconDirEntry->bWidth - width) + Math.Abs(pIconDirEntry->bHeight - height);
                        
                        if (thisDelta < bestDelta) {
                            pBestFit = pIconDirEntry;
                            bestBitDepth = iconBitDepth;
                        }
                        else if (thisDelta == bestDelta && (iconBitDepth <= bitDepth && iconBitDepth > bestBitDepth || bestBitDepth > bitDepth && iconBitDepth < bestBitDepth)) {
                            pBestFit = pIconDirEntry;
                            bestBitDepth = iconBitDepth;
                        }
                    }
                    
                    pIconDirEntry++;
                }
                
                Debug.Assert(pBestFit->dwImageOffset >= 0 && (pBestFit->dwImageOffset + pBestFit->dwBytesInRes) <= iconData.Length, "Illegal offset/length for the Icon data");

                if (pBestFit->dwImageOffset < 0 || (pBestFit->dwImageOffset + pBestFit->dwBytesInRes) > iconData.Length) {
                    throw new ArgumentException(SR.GetString(SR.InvalidPictureType, "picture", "Icon"));
                }

                handle = SafeNativeMethods.CreateIconFromResourceEx(pbIconData + pBestFit->dwImageOffset, pBestFit->dwBytesInRes, true, 0x00030000, 0, 0, 0);
                if (handle == IntPtr.Zero) {
                    throw new Win32Exception();
                }
#endif // WIN64
            }
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.Save"]/*' />
        /// <devdoc>
        ///     Saves this image to the given output stream.
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public void Save(Stream outputStream) {
            if (iconData != null) {
                outputStream.Write(iconData, 0, iconData.Length);
            }
            else {
                // Ideally, we would pick apart the icon using 
                // GetIconInfo, and then pull the individual bitmaps out,
                // converting them to DIBS and saving them into the file.
                // But, in the interest of simplicity, we just call to 
                // OLE to do it for us.
                //
                SafeNativeMethods.IPicture picture;
                SafeNativeMethods.PICTDESC pictdesc = SafeNativeMethods.PICTDESC.CreateIconPICTDESC(Handle);
                Guid g = typeof(SafeNativeMethods.IPicture).GUID;
                picture = SafeNativeMethods.OleCreatePictureIndirect(pictdesc, ref g, false);
                
                if (picture != null) {
                    int temp;
                    try{
                        picture.SaveAsFile(new UnsafeNativeMethods.ComStreamFromDataStream(outputStream), -1, out temp);
                    }
                    finally{
                        Marshal.ReleaseComObject(picture);
                    }
                }
            }
        }

        // SECREVIEW : Make sure the code calling this methods validates the source and target data.
        //
        // SAME CODE OR SIMILAR IN ImageList.cs
        private void CopyBitmapData(BitmapData sourceData, BitmapData targetData) {
            // do the actual copy
            int offsetSrc = 0;
            int offsetDest = 0;

            Debug.Assert(sourceData.Height == targetData.Height, "Unexpected height. How did this happen?");

            for (int i = 0; i < Math.Min(sourceData.Height, targetData.Height); i++) {
                IntPtr srcPtr, destPtr;
                if (IntPtr.Size == 4) {
                    srcPtr = new IntPtr(sourceData.Scan0.ToInt32() + offsetSrc);
                    destPtr = new IntPtr(targetData.Scan0.ToInt32() + offsetDest);
                } else {
                    srcPtr = new IntPtr(sourceData.Scan0.ToInt64() + offsetSrc);
                    destPtr = new IntPtr(targetData.Scan0.ToInt64() + offsetDest);
                }

                // SECREVIEW : This is an usafe call, buffer overflow can occur.  It is mitigated by copying as much data as the smaller 
                //             buffer can hold.  The src and dst of the data needs to be validated by callers of this function.
                //
                UnsafeNativeMethods.CopyMemory(new HandleRef(this, destPtr), new HandleRef(this, srcPtr), Math.Abs(targetData.Stride)); 

                offsetSrc += sourceData.Stride;
                offsetDest += targetData.Stride;
            }
        }

        private static bool BitmapHasAlpha(BitmapData bmpData) {
            bool hasAlpha = false;
            for (int i = 0; i < bmpData.Height; i++) {
                for (int j = 3; j < Math.Abs(bmpData.Stride); j += 4) { // stride here is fine since we know we're doing this on the whole image
                    unsafe {
                        byte* candidate = unchecked(((byte*)bmpData.Scan0.ToPointer()) + (i * bmpData.Stride) + j);
                        if (*candidate != 0) {
                            hasAlpha = true;
                            goto Found;
                        }
                    }
                }
            }
            Found:
                return hasAlpha;
        }

        // 





        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")] // supressing here since the call within the assert is safe
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Bitmap ToBitmap() {
            // DontSupportPngFramesInIcons is true when the application is targeting framework version below 4.6
            // and false when the application is targeting 4.6 and above. Downlevel application can also set the following switch
            // to false in the .config file's runtime section in order to opt-in into the new behavior:
            // <AppContextSwitchOverrides value="Switch.System.Drawing.DontSupportPngFramesInIcons=false" />
            if (HasPngSignature() && !LocalAppContextSwitches.DontSupportPngFramesInIcons) {
                return PngFrame();
            } else {
                return BmpFrame();
            }
        }

        private Bitmap BmpFrame() {
            Bitmap bitmap = null;
            if(iconData != null && bestBitDepth == 32) {
                // GDI+ doesnt handle 32 bpp icons with alpha properly
                // we load the icon ourself from the byte table
                bitmap = new Bitmap(Size.Width, Size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Debug.Assert(bestImageOffset >= 0 && (bestImageOffset + bestBytesInRes) <= iconData.Length, "Illegal offset/length for the Icon data");

                unsafe
                {
                    System.Drawing.Imaging.BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, Size.Width, Size.Height),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    try
                    {
                        uint * pixelPtr = (uint*)bmpdata.Scan0.ToPointer();

                        // jumping the image header
                        int newOffset = bestImageOffset + Marshal.SizeOf(typeof(SafeNativeMethods.BITMAPINFOHEADER));
                        // there is no color table that we need to skip since we're 32bpp

                        int lineLength = Size.Width * 4;
                        int width = Size.Width;
                        for(int j=(Size.Height-1)*4;j>=0;j-=4)
                        {
                            Marshal.Copy(iconData, newOffset + j * width, (IntPtr)pixelPtr, lineLength);
                            pixelPtr+=width;
                        }

                        // note: we ignore the mask that's available after the pixel table
                    }
                    finally
                    {
                        bitmap.UnlockBits(bmpdata);
                    }
                }
            } else if(bestBitDepth == 0 || bestBitDepth == 32){ // we don't know or we are 32bpp for sure
                //we don't have any icon data, let's fish out the data from the handle that we got...
                // we have to fish out the data for this icon if the icon is a 32bpp icon
                SafeNativeMethods.ICONINFO info = new SafeNativeMethods.ICONINFO();
                SafeNativeMethods.GetIconInfo(new HandleRef(this, handle), info);
                SafeNativeMethods.BITMAP bmp = new SafeNativeMethods.BITMAP();
                try {
                    if (info.hbmColor != IntPtr.Zero) {
                        SafeNativeMethods.GetObject(new HandleRef(null, info.hbmColor), Marshal.SizeOf(typeof(SafeNativeMethods.BITMAP)), bmp);
                        if(bmp.bmBitsPixel ==32) {
                            Bitmap tmpBitmap = null;
                            BitmapData bmpData = null;
                            BitmapData targetData = null;
                            // SECREVIEW : This assert is safe here, all data passed to unmanaged code is created by us.  The scope is ok too,
                            //             most operations in it demand this permission.
                            //
                            IntSecurity.ObjectFromWin32Handle.Assert();
                            try
                            {
                                tmpBitmap = Bitmap.FromHbitmap(info.hbmColor);

                                // 




                                bmpData = tmpBitmap.LockBits(new Rectangle(0,0, tmpBitmap.Width, tmpBitmap.Height), ImageLockMode.ReadOnly, tmpBitmap.PixelFormat);

                                // we need do the following if the image has alpha because otherwise the image is fully transparent even though it has data
                                if(BitmapHasAlpha(bmpData)) {
                                    bitmap = new Bitmap(bmpData.Width, bmpData.Height, PixelFormat.Format32bppArgb);
                                    targetData = bitmap.LockBits(new Rectangle(0, 0, bmpData.Width, bmpData.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                                    // SECREVIEW : both bmpData and targetData point to memory owned by this Icon object so the following call
                                    //             is safe.
                                    //
                                    CopyBitmapData(bmpData, targetData);
                                }
                            } finally {
                                CodeAccessPermission.RevertAssert();
                                if(tmpBitmap != null && bmpData != null) {
                                    tmpBitmap.UnlockBits(bmpData);
                                }
                                if(bitmap != null && targetData != null) {
                                    bitmap.UnlockBits(targetData);
                                }
                            }
                            tmpBitmap.Dispose();
                        }
                    }
                } finally {
                    if(info.hbmColor != IntPtr.Zero)
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmColor));
                    if(info.hbmMask != IntPtr.Zero)
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmMask));
                }
            }


            if(bitmap == null) {
                // last chance... all the other cases (ie non 32 bpp icons coming from a handle or from the bitmapData)

                // we have to do this rather than just return Bitmap.FromHIcon because 
                // the bitmap returned from that, even though it's 32bpp, just paints where the mask allows it
                // seems like another GDI+ weirdness. might be interesting to investigate further. In the meantime
                // this looks like the right thing to do and is not more expansive that what was present before.

                Size size = Size;
                bitmap = new Bitmap(size.Width, size.Height); // initialized to transparent
                Graphics graphics = null;
                try {
                    graphics = Graphics.FromImage(bitmap);
                    // SECREVIEW : This assert is safe here, no user data is involved here.
                    //
                    IntSecurity.ObjectFromWin32Handle.Assert();
                    try{
                        using(Bitmap tmpBitmap = Bitmap.FromHicon(this.Handle)) {
                            graphics.DrawImage(tmpBitmap, new Rectangle(0, 0, size.Width, size.Height));
                        }
                    } catch(ArgumentException) { // GDI+ weirdness episode MMMCLXXXXIVI, sometime FromHicon crash with no real reason,
                        // see VSWhidbey 518812
                        // backup plan is to just draw the image like we used to. 
                        // NOTE: FromHIcon is also where we have the buffer overrun
                        // if width and height are mismatched
                        Draw(graphics, new Rectangle(0, 0, size.Width, size.Height));
                    }
                    finally{
                        CodeAccessPermission.RevertAssert();
                    }
                }
                finally {
                    if (graphics != null) {
                        graphics.Dispose();
                    }
                }


                // gpr: GDI+ is filling the surface with a sentinel color for GetDC,
                // but is not correctly cleaning it up again, so we have to for it.
                Color fakeTransparencyColor = Color.FromArgb(0x0d, 0x0b, 0x0c);
                bitmap.MakeTransparent(fakeTransparencyColor);

            }

            Debug.Assert(bitmap != null, "Bitmap cannot be null");
            return bitmap;
        }

        private Bitmap PngFrame() {
            Bitmap bitmap = null;
            if (iconData != null) {
                using (MemoryStream stream = new MemoryStream()) {
                    stream.Write(iconData, bestImageOffset, bestBytesInRes);
                    bitmap = new Bitmap(stream);
                }
            }
            return bitmap;
        }

        private bool HasPngSignature() {
            if (!isBestImagePng.HasValue) {
                if (iconData != null && iconData.Length >= bestImageOffset + 8) {
                    int iconSignature1 = BitConverter.ToInt32(iconData, bestImageOffset);
                    int iconSignature2 = BitConverter.ToInt32(iconData, bestImageOffset + 4);
                    isBestImagePng = (iconSignature1 == PNGSignature1) && (iconSignature2 == PNGSignature2);
                } else {
                    isBestImagePng = false;
                }
            }

            return isBestImagePng.Value;
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.ToString"]/*' />
        /// <devdoc>
        ///     Retrieves a human readable string representing the cursor.
        /// </devdoc>
        public override string ToString() {
            return SR.GetString(SR.toStringIcon);
        }

        /// <include file='doc\Icon.uex' path='docs/doc[@for="Icon.ISerializable.GetObjectData"]/*' />
        /// <devdoc>
        ///     ISerializable private implementation
        /// </devdoc>
        /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context) {
            if (iconData != null) {
                si.AddValue("IconData", iconData, typeof(byte[]));
            }
            else {
                MemoryStream stream = new MemoryStream();
                Save(stream);
                si.AddValue("IconData", stream.ToArray(), typeof(byte[]));
            }
            si.AddValue("IconSize", iconSize, typeof(Size));
        }

    }
}

