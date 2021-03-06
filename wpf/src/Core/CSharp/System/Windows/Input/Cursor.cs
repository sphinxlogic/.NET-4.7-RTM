using System;
using System.ComponentModel;
using System.Text;
using System.Globalization;
using MS.Win32;
using System.Runtime.InteropServices;
using System.Resources;
using System.IO;
using System.Security;
using System.Security.Permissions;
using SecurityHelper=MS.Internal.SecurityHelper; 
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.PresentationCore;     //  FriendAccessAllowed

namespace System.Windows.Input 
{
    /// <summary>
    ///     Cursor class to support default cursor types.
    ///    TBD: Support for cutomized cursor types.
    /// </summary>
    [TypeConverter(typeof(CursorConverter))]    
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public sealed class Cursor : IDisposable 
    {
        /// <summary>
        /// Constructor for Standard Cursors, needn't be public as Stock Cursors
        /// are exposed in Cursors clas.
        /// </summary>
        /// <param name="cursorType"></param>
        internal Cursor(CursorType cursorType)
        {
            if (IsValidCursorType(cursorType))
            {
                LoadCursorHelper(cursorType);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.InvalidCursorType, cursorType));
            }
        }

        /// <summary>
        /// Cursor from .ani or .cur file
        /// </summary>
        /// <param name="cursorFile"></param>
        public Cursor(string cursorFile):this(cursorFile, false)
        {
        }
        
        /// <summary>
        /// Cursor from .ani or .cur file
        /// </summary>
        /// <param name="cursorFile"></param>
        /// <param name="scaleWithDpi"></param>
        public Cursor(string cursorFile, bool scaleWithDpi)
        {
            _scaleWithDpi = scaleWithDpi;
            if (cursorFile == null)
                throw new ArgumentNullException("cursorFile");
        
            if ((cursorFile != String.Empty) &&
                (cursorFile.EndsWith(".cur", StringComparison.OrdinalIgnoreCase) || 
                 cursorFile.EndsWith(".ani", StringComparison.OrdinalIgnoreCase)))
            {
                LoadFromFile(cursorFile);
                _fileName = cursorFile;
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.Cursor_UnsupportedFormat , cursorFile));
            }
        }
        
        /// <summary>
        /// Cursor from Stream
        /// </summary>
        /// <param name="cursorStream"></param>
        public Cursor(Stream cursorStream):this(cursorStream, false)
        {
        }
        
        /// <summary>
        /// Cursor from Stream
        /// </summary>
        /// <param name="cursorStream"></param>
        /// <param name="scaleWithDpi"></param>
        public Cursor(Stream cursorStream, bool scaleWithDpi)
        {
            _scaleWithDpi = scaleWithDpi;
            if (cursorStream == null)
            {
                throw new ArgumentNullException("cursorStream");
            }
            LoadFromStream(cursorStream);
        }

        /// <summary>
        ///     Cursor from a SafeHandle to an HCURSOR
        /// </summary>
        /// <param name="cursorHandle"></param>
        /// <SecurityNote>
        ///    Critical: Sets _cursorHandle, which is of a SecurityCritical type. (V4 transparency enforcement)
        ///    TreatAsSafe: This code is safe to expose because in the worst case you change cursor for your app
        /// See SecurityNote on _cursorHandle.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        [FriendAccessAllowed] //used by ColumnHeader.GetCursor in PresentationFramework
        internal Cursor(SafeHandle cursorHandle ) 
        {
            if (! cursorHandle.IsInvalid )
            {
                this._cursorHandle = cursorHandle ; 
            }
        }

         /// <summary>
        /// Destructor (IDispose pattern)
        /// </summary>
        ~Cursor()
        {
            Dispose(false);
        }

        ///  <summary>
        ///     Cleans up the resources allocated by this object.  Once called, the cursor
        ///     object is no longer useful.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <SecurityNote>
        ///    Critical: SafeHandle code link demands on dispose. 
        ///    TreatAsSafe: Safe to dispose a cursor. 
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe ]         
        void Dispose(bool disposing)
        {
            if ( _cursorHandle != null ) 
            {            
                _cursorHandle.Dispose(); 
                _cursorHandle = null; 
            }                
        }



        /// <summary>
        /// CursorType - Cursor Type Enumeration
        /// </summary>
        /// <value></value>
        internal CursorType CursorType
        {
            get
            {
                return _cursorType;
            }
        }

        /// <summary>
        /// Handle - HCURSOR Interop
        /// </summary>
        /// <value></value>
        /// <SecurityNote>
        /// Critical: Returns a SafeHandle, which is a SecurityCritical type in v4.
        /// See SecurityNote on _cursorHandle.
        /// </SecurityNote>
        internal SafeHandle Handle
        {
            [SecurityCritical]
            get
            {
                return _cursorHandle ?? NativeMethods.CursorHandle.GetInvalidCursor();
            }
        }


        /// <summary>
        /// FileName - .ani or .cur files are allowed
        /// </summary>
        /// <value></value>
        internal String FileName
        {
            get
            {
                return _fileName;
            }
        }

        ///<SecurityNote>
        ///     Critical: 1) Access to a file. Calls Win32Exception ctor, which LinkDemands. 
        ///         Method success/failure could be used to do local file path probing.
        ///         2) Sets _cursorHandle, which is of a SecurityCritical type.
        ///     TreatAsSafe: 1) We demand FileIOPermission. Then it's okay to throw an exception about any failure.
        ///         2) Anyone is allowed to set the cursor. See SecurityNote on _cursorHandle.
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe] 
        private void LoadFromFile(string fileName)
        {
            SecurityHelper.DemandFileIOReadPermission(fileName);

            // Load a Custom Cursor
            _cursorHandle = UnsafeNativeMethods.LoadImageCursor(IntPtr.Zero,
                                                                fileName,
                                                                NativeMethods.IMAGE_CURSOR,
                                                                0, 0,  
                                                                NativeMethods.LR_DEFAULTCOLOR |
                                                                NativeMethods.LR_LOADFROMFILE |
                                                                (_scaleWithDpi? NativeMethods.LR_DEFAULTSIZE : 0x0000));

            int errorCode = Marshal.GetLastWin32Error();
            if (_cursorHandle == null || _cursorHandle.IsInvalid)
            {
                // Note: chandras 02/02/2005
                // Bug # 1016022: LoadImage returns a null handle but does not set 
                // the error condition when icon file is of an incorrect type (e.g., .bmp)
                //
                // LoadImage has a bug where it doesn't set the correct error code
                // when a file is given that is not an ico file.  Icon load fails 
                // but win32 error code is still zero (success).  Thus, we need to 
                // special case this scenario.              
                //
                if (errorCode != 0)
                {
                    if ((errorCode == NativeMethods.ERROR_FILE_NOT_FOUND) || (errorCode == NativeMethods.ERROR_PATH_NOT_FOUND))
                    {
                        throw new Win32Exception(errorCode, SR.Get(SRID.Cursor_LoadImageFailure, fileName));
                    }
                    else
                    {
                        throw new Win32Exception(errorCode);
                    }
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.Cursor_LoadImageFailure, fileName));
                }
            }
        }

        private const int BUFFERSIZE = 4096; // the maximum size of the buffer used for loading from stream
        
        /// <SecurityNote>
        /// Critical: Sets _cursorHandle, which is of a SecurityCritical type.
        /// TreatAsSafe: Anyone is allowed to set the cursor. See SecurityNote on _cursorHandle.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void LoadFromStream(Stream cursorStream)
        {
            //Generate a temporal file based on the memory stream.

            //







            string filePath = Path.GetTempFileName();
            try
            {
                using (BinaryReader reader = new BinaryReader(cursorStream))
                {
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        // Read the bytes from the stream, up to BUFFERSIZE
                        byte[] cursorData = reader.ReadBytes(BUFFERSIZE);
                        int dataSize;

                        // If the buffer is filled up, then write those bytes out and read more bytes up to BUFFERSIZE
                        for (dataSize = cursorData.Length; 
                             dataSize >= BUFFERSIZE;
                             dataSize = reader.Read(cursorData, 0 /*index in array*/, BUFFERSIZE /*bytes to read*/))
                        {
                            fileStream.Write(cursorData, 0 /*index in array*/, BUFFERSIZE /*bytes to write*/);
                        }

                        // Write any remaining bytes
                        fileStream.Write(cursorData, 0 /*index in array*/, dataSize /*bytes to write*/);
                    }
                }

                // This method is called with File Write permission still asserted.
                // However, this method just reads this file into an icon.
               _cursorHandle = UnsafeNativeMethods.LoadImageCursor(IntPtr.Zero,
                                                                    filePath,
                                                                    NativeMethods.IMAGE_CURSOR,
                                                                    0, 0,  
                                                                    NativeMethods.LR_DEFAULTCOLOR |
                                                                    NativeMethods.LR_LOADFROMFILE |
                                                                    (_scaleWithDpi? NativeMethods.LR_DEFAULTSIZE : 0x0000));
                if (_cursorHandle == null || _cursorHandle.IsInvalid)
                {
                     throw new ArgumentException(SR.Get(SRID.Cursor_InvalidStream));
                }
            }
            finally
            {
                try
                {
                    File.Delete(filePath);
                }
                catch(System.IO.IOException)
                {
                    // DDVSO 227517: We may not be able to delete the file if it's being used by some other process (e.g. Anti-virus check).
                    // There's nothing we can do in that case, so just eat the exception and leave the file behind
                }
            }
        }

        /// <SecurityNote>
        /// Critical: Sets _cursorHandle, which is of a SecurityCritical type.
        /// TreatAsSafe: Anyone is allowed to set the cursor. See SecurityNote on _cursorHandle.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void LoadCursorHelper(CursorType cursorType)
        {
            if (cursorType != CursorType.None)
            {
                // Load a Standard Cursor
                _cursorHandle = SafeNativeMethods.LoadCursor(new HandleRef(this,IntPtr.Zero), (IntPtr)(CursorTypes[(int)cursorType])); 
            }
            this._cursorType = cursorType;
        }

        /// <summary>
        /// String Output
        /// 

        public override string ToString()
        {
            if (_fileName != String.Empty)
                return _fileName;
            else
            {
                // Get the string representation fo the cursor type enumeration.
                return Enum.GetName(typeof(CursorType), _cursorType);
            }
        }

        private bool IsValidCursorType(CursorType cursorType)
        {
            return ((int)cursorType >= (int)CursorType.None && (int)cursorType <= (int)CursorType.ArrowCD);
        }

        private string      _fileName     = String.Empty;
        private CursorType  _cursorType   = CursorType.None;
        private bool        _scaleWithDpi = false;

        /// <SecurityNote>
        /// In v4, SafeHandle is marked as SecurityCritical. According to the new transparency enforcement model,
        /// any access to this field must be done from a SecurityCritical method (even though our assemblies still
        /// use the v2 model). However, we don't consider setting/replacing the cursor a critical operation. 
        /// (Effect is only within the application's UI.) That's why TreatAsSafe methods are allowed to set this
        /// field. Individual methods on SafeHandle still guard access to the OS handle.
        /// </SecurityNote>
        [SecurityCritical]
        private SafeHandle  _cursorHandle; 

        private static readonly int[] CursorTypes = {
            0, // None
            NativeMethods.IDC_NO,
            NativeMethods.IDC_ARROW,
            NativeMethods.IDC_APPSTARTING,
            NativeMethods.IDC_CROSS,
            NativeMethods.IDC_HELP,
            NativeMethods.IDC_IBEAM,
            NativeMethods.IDC_SIZEALL,
            NativeMethods.IDC_SIZENESW,
            NativeMethods.IDC_SIZENS,
            NativeMethods.IDC_SIZENWSE,
            NativeMethods.IDC_SIZEWE,
            NativeMethods.IDC_UPARROW,
            NativeMethods.IDC_WAIT,
            NativeMethods.IDC_HAND,
            NativeMethods.IDC_ARROW + 119, // PenCursor
            NativeMethods.IDC_ARROW + 140, // ScrollNSCursor
            NativeMethods.IDC_ARROW + 141, // ScrollWECursor
            NativeMethods.IDC_ARROW + 142, // ScrollAllCursor
            NativeMethods.IDC_ARROW + 143, // ScrollNCursor
            NativeMethods.IDC_ARROW + 144, // ScrollSCursor
            NativeMethods.IDC_ARROW + 145, // ScrollWCursor
            NativeMethods.IDC_ARROW + 146, // ScrollECursor
            NativeMethods.IDC_ARROW + 147, // ScrollNWCursor
            NativeMethods.IDC_ARROW + 148, // ScrollNECursor
            NativeMethods.IDC_ARROW + 149, // ScrollSWCursor
            NativeMethods.IDC_ARROW + 150, // ScrollSECursor
            NativeMethods.IDC_ARROW + 151 // ArrowCDCursor
       };
    }
}
