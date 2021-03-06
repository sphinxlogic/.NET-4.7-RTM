using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Win32;
using MS.Win32;
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.KnownBoxes;

// Disable pragma warnings to enable PREsharp pragmas
#pragma warning disable 1634, 1691

namespace System.Windows
{
    /// <summary>
    /// Indicates whether the system power is online, or that the system power status is unknown.
    /// </summary>
    public enum PowerLineStatus
    {
        /// <summary>
        /// The system is offline.
        /// </summary>
        Offline = 0x00,

        /// <summary>
        /// The system is online.
        /// </summary>
        Online = 0x01,

        /// <summary>
        /// The power status of the system is unknown.
        /// </summary>
        Unknown = 0xFF,
    }

    /// <summary>
    ///     Contains properties that are queries into the system's various settings.
    /// </summary>
    public static class SystemParameters
    {
        public static event System.ComponentModel.PropertyChangedEventHandler StaticPropertyChanged;

        private static void OnPropertiesChanged(params string[] propertyNames)
        {
            if (StaticPropertyChanged != null)
            {
                for (int i=0; i<propertyNames.Length; ++i)
                {
                    StaticPropertyChanged(null, new System.ComponentModel.PropertyChangedEventArgs(propertyNames[i]));
                }
            }
        }

        private static bool InvalidateProperty(int slot, string name)
        {
            if (!SystemResources.ClearSlot(_cacheValid, slot))
                return false;

            OnPropertiesChanged(name);
            return true;
        }

// Disable Warning 6503 Property get methods should not throw exceptions.
// By design properties below throw Win32Exception if there is an error when calling the native method
#pragma warning disable 6503

// Win32Exception will get the last Win32 error code in case of errors, so we don't have to.
#pragma warning disable 6523

        #region Accessibility Parameters

        /// <summary>
        ///     Maps to SPI_GETFOCUSBORDERWIDTH
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK -- Determined safe: getting the size of the dotted rectangle around a selected obj
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FocusBorderWidth
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FocusBorderWidth])
                    {
                        int focusBorderWidth = 0;

                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFOCUSBORDERWIDTH, 0, ref focusBorderWidth, 0))
                        {
                            _focusBorderWidth = ConvertPixel(focusBorderWidth);
                            _cacheValid[(int)CacheSlot.FocusBorderWidth] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _focusBorderWidth;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETFOCUSBORDERHEIGHT
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK -- Determined safe: getting the size of the dotted rectangle around a selected obj
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FocusBorderHeight
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FocusBorderHeight])
                    {
                        int focusBorderHeight = 0;

                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFOCUSBORDERHEIGHT, 0, ref focusBorderHeight, 0))
                        {
                            _focusBorderHeight = ConvertPixel(focusBorderHeight);
                            _cacheValid[(int)CacheSlot.FocusBorderHeight] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _focusBorderHeight;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETHIGHCONTRAST -> HCF_HIGHCONTRASTON
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        ///  PublicOK - considered ok to expose since the method doesn't take user input and only
        ///                returns a boolean value which indicates the current high contrast mode.
        /// </SecurityNote>
        public static bool HighContrast
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.HighContrast])
                    {
                        NativeMethods.HIGHCONTRAST_I highContrast = new NativeMethods.HIGHCONTRAST_I();

                        highContrast.cbSize = Marshal.SizeOf(typeof(NativeMethods.HIGHCONTRAST_I));
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETHIGHCONTRAST, highContrast.cbSize, ref highContrast, 0))
                        {
                            _highContrast = (highContrast.dwFlags & NativeMethods.HCF_HIGHCONTRASTON) == NativeMethods.HCF_HIGHCONTRASTON;
                            _cacheValid[(int)CacheSlot.HighContrast] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _highContrast;
            }
        }

        /// <summary>
        /// Maps to SPI_GETMOUSEVANISH.
        /// </summary>
        /// <SecurityNote>
        ///  Critical -- calling UnsafeNativeMethods
        ///  PublicOK - considered ok to expose.
        /// </SecurityNote>
        // 
        internal static bool MouseVanish
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MouseVanish])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSEVANISH, 0, ref _mouseVanish, 0))
                        {
                            _cacheValid[(int)CacheSlot.MouseVanish] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _mouseVanish;
            }
        }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static SystemResourceKey CreateInstance(SystemResourceKeyID KeyId)
        {
            return new SystemResourceKey(KeyId);
        }

        #region Accessibility Keys

        /// <summary>
        ///     FocusBorderWidth System Resource Key
        /// </summary>
        public static ResourceKey FocusBorderWidthKey
        {
            get
            {
                if (_cacheFocusBorderWidth == null)
                {
                    _cacheFocusBorderWidth = CreateInstance(SystemResourceKeyID.FocusBorderWidth);
                }

                return _cacheFocusBorderWidth;
            }
        }

        /// <summary>
        ///     FocusBorderHeight System Resource Key
        /// </summary>
        public static ResourceKey FocusBorderHeightKey
        {
            get
            {
                if (_cacheFocusBorderHeight == null)
                {
                    _cacheFocusBorderHeight = CreateInstance(SystemResourceKeyID.FocusBorderHeight);
                }

                return _cacheFocusBorderHeight;
            }
        }

        /// <summary>
        ///     HighContrast System Resource Key
        /// </summary>
        public static ResourceKey HighContrastKey
        {
            get
            {
                if (_cacheHighContrast == null)
                {
                    _cacheHighContrast = CreateInstance(SystemResourceKeyID.HighContrast);
                }

                return _cacheHighContrast;
            }
        }

        #endregion

        #region Desktop Parameters

        /// <summary>
        ///     Maps to SPI_GETDROPSHADOW
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK: This information is ok to give out
        /// </SecurityNote>
        public static bool DropShadow
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.DropShadow])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETDROPSHADOW, 0, ref _dropShadow, 0))
                        {
                            _cacheValid[(int)CacheSlot.DropShadow] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _dropShadow;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETFLATMENU
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool FlatMenu
        {

            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FlatMenu])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFLATMENU, 0, ref _flatMenu, 0))
                        {
                            _cacheValid[(int)CacheSlot.FlatMenu] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _flatMenu;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETWORKAREA
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  TreatAsSafe - Okay to expose info to internet callers.
        /// </SecurityNote>
        internal static NativeMethods.RECT WorkAreaInternal
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WorkAreaInternal])
                    {
                        _workAreaInternal = new NativeMethods.RECT();
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref _workAreaInternal, 0))
                        {
                            _cacheValid[(int)CacheSlot.WorkAreaInternal] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _workAreaInternal;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETWORKAREA
        /// </summary>
        public static Rect WorkArea
        {
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WorkArea])
                    {
                        NativeMethods.RECT workArea = WorkAreaInternal;

                        _workArea = new Rect(ConvertPixel(workArea.left), ConvertPixel(workArea.top), ConvertPixel(workArea.Width), ConvertPixel(workArea.Height));
                        _cacheValid[(int)CacheSlot.WorkArea] = true;
                    }
                }

                return _workArea;
            }
        }

        #endregion

        #region Desktop Keys

        /// <summary>
        ///     DropShadow System Resource Key
        /// </summary>
        public static ResourceKey DropShadowKey
        {
            get
            {
                if (_cacheDropShadow == null)
                {
                    _cacheDropShadow = CreateInstance(SystemResourceKeyID.DropShadow);
                }

                return _cacheDropShadow;
            }
        }

        /// <summary>
        ///     FlatMenu System Resource Key
        /// </summary>
        public static ResourceKey FlatMenuKey
        {
            get
            {
                if (_cacheFlatMenu == null)
                {
                    _cacheFlatMenu = CreateInstance(SystemResourceKeyID.FlatMenu);
                }

                return _cacheFlatMenu;
            }
        }

        /// <summary>
        ///     WorkArea System Resource Key
        /// </summary>
        public static ResourceKey WorkAreaKey
        {
            get
            {
                if (_cacheWorkArea == null)
                {
                    _cacheWorkArea = CreateInstance(SystemResourceKeyID.WorkArea);
                }

                return _cacheWorkArea;
            }
        }

        #endregion

        #region Icon Parameters

        /// <summary>
        ///     Maps to SPI_GETICONMETRICS
        /// </summary>
        ///<SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  TreatAsSafe - Okay to expose info to internet callers.
        ///</SecurityNote>
        internal static NativeMethods.ICONMETRICS IconMetrics
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IconMetrics])
                    {
                        _iconMetrics = new NativeMethods.ICONMETRICS();
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETICONMETRICS, _iconMetrics.cbSize, _iconMetrics, 0))
                        {
                            _cacheValid[(int)CacheSlot.IconMetrics] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _iconMetrics;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETICONMETRICS -> iHorzSpacing or SPI_ICONHORIZONTALSPACING
        /// </summary>
        public static double IconHorizontalSpacing
        {
            get
            {
                return ConvertPixel(IconMetrics.iHorzSpacing);
            }
        }

        /// <summary>
        ///     Maps to SPI_GETICONMETRICS -> iVertSpacing or SPI_ICONVERTICALSPACING
        /// </summary>
        public static double IconVerticalSpacing
        {
            get
            {
                return ConvertPixel(IconMetrics.iVertSpacing);
            }
        }

        /// <summary>
        ///     Maps to SPI_GETICONMETRICS -> iTitleWrap or SPI_GETICONTITLEWRAP
        /// </summary>
        public static bool IconTitleWrap
        {
            get
            {
                return IconMetrics.iTitleWrap != 0;
            }
        }

        #endregion

        #region Icon Keys

        /// <summary>
        ///     IconHorizontalSpacing System Resource Key
        /// </summary>
        public static ResourceKey IconHorizontalSpacingKey
        {
            get
            {
                if (_cacheIconHorizontalSpacing == null)
                {
                    _cacheIconHorizontalSpacing = CreateInstance(SystemResourceKeyID.IconHorizontalSpacing);
                }

                return _cacheIconHorizontalSpacing;
            }
        }

        /// <summary>
        ///     IconVerticalSpacing System Resource Key
        /// </summary>
        public static ResourceKey IconVerticalSpacingKey
        {
            get
            {
                if (_cacheIconVerticalSpacing == null)
                {
                    _cacheIconVerticalSpacing = CreateInstance(SystemResourceKeyID.IconVerticalSpacing);
                }

                return _cacheIconVerticalSpacing;
            }
        }

        /// <summary>
        ///     IconTitleWrap System Resource Key
        /// </summary>
        public static ResourceKey IconTitleWrapKey
        {
            get
            {
                if (_cacheIconTitleWrap == null)
                {
                    _cacheIconTitleWrap = CreateInstance(SystemResourceKeyID.IconTitleWrap);
                }

                return _cacheIconTitleWrap;
            }
        }

        #endregion

        #region Input Parameters

        /// <summary>
        ///     Maps to SPI_GETKEYBOARDCUES
        /// </summary>
        ///
        /// <SecurityNote>
        /// Demanding unmanaged code permission because calling an unsafe native method.
        ///  SecurityCritical because it calls an unsafe native method.  PublicOK because is demanding unmanaged code perm.
        /// PublicOK: This information is ok to give out
        /// </SecurityNote>
        public static bool KeyboardCues
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.KeyboardCues])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETKEYBOARDCUES, 0, ref _keyboardCues, 0))
                        {
                            _cacheValid[(int)CacheSlot.KeyboardCues] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _keyboardCues;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETKEYBOARDDELAY
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK -- Determined safe: getting keyboard repeat delay
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static int KeyboardDelay
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.KeyboardDelay])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETKEYBOARDDELAY, 0, ref _keyboardDelay, 0))
                        {
                            _cacheValid[(int)CacheSlot.KeyboardDelay] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _keyboardDelay;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETKEYBOARDPREF
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool KeyboardPreference
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.KeyboardPreference])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETKEYBOARDPREF, 0, ref _keyboardPref, 0))
                        {
                            _cacheValid[(int)CacheSlot.KeyboardPreference] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _keyboardPref;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETKEYBOARDSPEED
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK -- Determined safe: getting keyboard repeat-speed
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static int KeyboardSpeed
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.KeyboardSpeed])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETKEYBOARDSPEED, 0, ref _keyboardSpeed, 0))
                        {
                            _cacheValid[(int)CacheSlot.KeyboardSpeed] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _keyboardSpeed;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETSNAPTODEFBUTTON
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool SnapToDefaultButton
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.SnapToDefaultButton])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETSNAPTODEFBUTTON, 0, ref _snapToDefButton, 0))
                        {
                            _cacheValid[(int)CacheSlot.SnapToDefaultButton] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _snapToDefButton;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETWHEELSCROLLLINES
        /// </summary>
        /// <SecurityNote>
        ///     Get is PublicOK -- Determined safe: Geting the number of lines to scroll when the mouse wheel is rotated. \
        ///     Get is Critical -- Calling unsafe native methods.
        /// </SecurityNote>
        public static int WheelScrollLines
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WheelScrollLines])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWHEELSCROLLLINES, 0, ref _wheelScrollLines, 0))
                        {
                            _cacheValid[(int)CacheSlot.WheelScrollLines] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _wheelScrollLines;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETMOUSEHOVERTIME.
        /// </summary>
        public static TimeSpan MouseHoverTime
        {
            get
            {
                return TimeSpan.FromMilliseconds(MouseHoverTimeMilliseconds);
            }
        }

        /// <SecurityNote>
        ///    TreatAsSafe -- Determined safe: getting time mouse pointer has to stay in the hover rectangle for TrackMouseEvent to generate a WM_MOUSEHOVER message.
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        internal static int MouseHoverTimeMilliseconds
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MouseHoverTime])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSEHOVERTIME, 0, ref _mouseHoverTime, 0))
                        {
                            _cacheValid[(int)CacheSlot.MouseHoverTime] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _mouseHoverTime;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETMOUSEHOVERHEIGHT.
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK -- Determined safe: gettingthe height, in pixels, of the rectangle within which the mouse pointer has to stay for TrackMouseEvent to generate a WM_MOUSEHOVER message
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MouseHoverHeight
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MouseHoverHeight])
                    {
                        int mouseHoverHeight = 0;

                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSEHOVERHEIGHT, 0, ref mouseHoverHeight, 0))
                        {
                            _mouseHoverHeight = ConvertPixel(mouseHoverHeight);
                            _cacheValid[(int)CacheSlot.MouseHoverHeight] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _mouseHoverHeight;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETMOUSEHOVERWIDTH.
        /// </summary>
        ///
        /// <SecurityNote>
        ///    PublicOK -- Determined safe: getting the width, in pixels, of the rectangle within which the mouse pointer has to stay for TrackMouseEvent to generate a WM_MOUSEHOVER message
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MouseHoverWidth
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MouseHoverWidth])
                    {
                        int mouseHoverWidth = 0;

                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMOUSEHOVERWIDTH, 0, ref mouseHoverWidth, 0))
                        {
                            _mouseHoverWidth = ConvertPixel(mouseHoverWidth);
                            _cacheValid[(int)CacheSlot.MouseHoverWidth] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _mouseHoverWidth;
            }
        }


        #endregion

        #region Input Keys

        /// <summary>
        ///     KeyboardCues System Resource Key
        /// </summary>
        public static ResourceKey KeyboardCuesKey
        {
            get
            {
                if (_cacheKeyboardCues == null)
                {
                    _cacheKeyboardCues = CreateInstance(SystemResourceKeyID.KeyboardCues);
                }

                return _cacheKeyboardCues;
            }
        }

        /// <summary>
        ///     KeyboardDelay System Resource Key
        /// </summary>
        public static ResourceKey KeyboardDelayKey
        {
            get
            {
                if (_cacheKeyboardDelay == null)
                {
                    _cacheKeyboardDelay = CreateInstance(SystemResourceKeyID.KeyboardDelay);
                }

                return _cacheKeyboardDelay;
            }
        }

        /// <summary>
        ///     KeyboardPreference System Resource Key
        /// </summary>
        public static ResourceKey KeyboardPreferenceKey
        {
            get
            {
                if (_cacheKeyboardPreference == null)
                {
                    _cacheKeyboardPreference = CreateInstance(SystemResourceKeyID.KeyboardPreference);
                }

                return _cacheKeyboardPreference;
            }
        }

        /// <summary>
        ///     KeyboardSpeed System Resource Key
        /// </summary>
        public static ResourceKey KeyboardSpeedKey
        {
            get
            {
                if (_cacheKeyboardSpeed == null)
                {
                    _cacheKeyboardSpeed = CreateInstance(SystemResourceKeyID.KeyboardSpeed);
                }

                return _cacheKeyboardSpeed;
            }
        }

        /// <summary>
        ///     SnapToDefaultButton System Resource Key
        /// </summary>
        public static ResourceKey SnapToDefaultButtonKey
        {
            get
            {
                if (_cacheSnapToDefaultButton == null)
                {
                    _cacheSnapToDefaultButton = CreateInstance(SystemResourceKeyID.SnapToDefaultButton);
                }

                return _cacheSnapToDefaultButton;
            }
        }

        /// <summary>
        ///     WheelScrollLines System Resource Key
        /// </summary>
        public static ResourceKey WheelScrollLinesKey
        {
            get
            {
                if (_cacheWheelScrollLines == null)
                {
                    _cacheWheelScrollLines = CreateInstance(SystemResourceKeyID.WheelScrollLines);
                }

                return _cacheWheelScrollLines;
            }
        }

        /// <summary>
        ///     MouseHoverTime System Resource Key
        /// </summary>
        public static ResourceKey MouseHoverTimeKey
        {
            get
            {
                if (_cacheMouseHoverTime == null)
                {
                    _cacheMouseHoverTime = CreateInstance(SystemResourceKeyID.MouseHoverTime);
                }

                return _cacheMouseHoverTime;
            }
        }

        /// <summary>
        ///     MouseHoverHeight System Resource Key
        /// </summary>
        public static ResourceKey MouseHoverHeightKey
        {
            get
            {
                if (_cacheMouseHoverHeight == null)
                {
                    _cacheMouseHoverHeight = CreateInstance(SystemResourceKeyID.MouseHoverHeight);
                }

                return _cacheMouseHoverHeight;
            }
        }

        /// <summary>
        ///     MouseHoverWidth System Resource Key
        /// </summary>
        public static ResourceKey MouseHoverWidthKey
        {
            get
            {
                if (_cacheMouseHoverWidth == null)
                {
                    _cacheMouseHoverWidth = CreateInstance(SystemResourceKeyID.MouseHoverWidth);
                }

                return _cacheMouseHoverWidth;
            }
        }

        #endregion

        #region Menu Parameters

        /// <summary>
        ///     Maps to SPI_GETMENUDROPALIGNMENT
        /// </summary>
        /// <SecurityNote>
        /// Demanding unmanaged code permission because calling an unsafe native method.
        /// Critical - get: it calls an unsafe native method
        /// PublicOK - get: it's safe to expose a menu drop alignment of a system.
        /// </SecurityNote>
        public static bool MenuDropAlignment
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuDropAlignment])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMENUDROPALIGNMENT, 0, ref _menuDropAlignment, 0))
                        {
                            _cacheValid[(int)CacheSlot.MenuDropAlignment] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }
                return _menuDropAlignment;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETMENUFADE
        /// </summary>
        /// <SecurityNote>
        /// Critical - because it calls an unsafe native method
        /// PublicOK - ok to return menu fade data
        /// </SecurityNote>
        public static bool MenuFade
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuFade])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMENUFADE, 0, ref _menuFade, 0))
                        {
                            _cacheValid[(int)CacheSlot.MenuFade] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _menuFade;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETMENUSHOWDELAY
        /// </summary>
        /// <SecurityNote>
        ///     Critical - calls a method that perfoms an elevation.
        ///     PublicOK - considered ok to expose in partial trust.
        /// </SecurityNote>
        public static int MenuShowDelay
        {

            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuShowDelay])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMENUSHOWDELAY, 0, ref _menuShowDelay, 0))
                        {
                            _cacheValid[(int)CacheSlot.MenuShowDelay] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _menuShowDelay;
            }
        }

        #endregion

        #region Menu Keys

        /// <summary>
        ///     MenuDropAlignment System Resource Key
        /// </summary>
        public static ResourceKey MenuDropAlignmentKey
        {
            get
            {
                if (_cacheMenuDropAlignment == null)
                {
                    _cacheMenuDropAlignment = CreateInstance(SystemResourceKeyID.MenuDropAlignment);
                }

                return _cacheMenuDropAlignment;
            }
        }

        /// <summary>
        ///     MenuFade System Resource Key
        /// </summary>
        public static ResourceKey MenuFadeKey
        {
            get
            {
                if (_cacheMenuFade == null)
                {
                    _cacheMenuFade = CreateInstance(SystemResourceKeyID.MenuFade);
                }

                return _cacheMenuFade;
            }
        }

        /// <summary>
        ///     MenuShowDelay System Resource Key
        /// </summary>
        public static ResourceKey MenuShowDelayKey
        {
            get
            {
                if (_cacheMenuShowDelay == null)
                {
                    _cacheMenuShowDelay = CreateInstance(SystemResourceKeyID.MenuShowDelay);
                }

                return _cacheMenuShowDelay;
            }
        }

        #endregion

        #region UI Effects Parameters

        /// <summary>
        ///     Returns the system value of PopupAnimation for ComboBoxes.
        /// </summary>
        public static PopupAnimation ComboBoxPopupAnimation
        {
            get
            {
                if (ComboBoxAnimation)
                {
                    return PopupAnimation.Slide;
                }

                return PopupAnimation.None;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETCOMBOBOXANIMATION
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK: This information is ok to give out
        /// </SecurityNote>

        public static bool ComboBoxAnimation
        {

            [SecurityCritical ]
            get
            {

                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ComboBoxAnimation])
                    {

                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETCOMBOBOXANIMATION, 0, ref _comboBoxAnimation, 0))
                        {
                            _cacheValid[(int)CacheSlot.ComboBoxAnimation] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _comboBoxAnimation;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETCLIENTAREAANIMATION
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK: This information is ok to give out
        /// </SecurityNote>
        public static bool ClientAreaAnimation
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ClientAreaAnimation])
                    {
                        // This parameter is only available on Windows Versions >= 0x0600 (Vista)
                        if (System.Environment.OSVersion.Version.Major >= 6)
                        {
                            if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETCLIENTAREAANIMATION, 0, ref _clientAreaAnimation, 0))
                            {
                                _cacheValid[(int)CacheSlot.ClientAreaAnimation] = true;
                            }
                            else
                            {
                                throw new Win32Exception();
                            }
                        }
                        else  // Windows XP, assume value is true
                        {
                            _clientAreaAnimation = true;
                            _cacheValid[(int)CacheSlot.ClientAreaAnimation] = true;
                        }
                    }
                }

                return _clientAreaAnimation;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETCURSORSHADOW
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool CursorShadow
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.CursorShadow])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETCURSORSHADOW, 0, ref _cursorShadow, 0))
                        {
                            _cacheValid[(int)CacheSlot.CursorShadow] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _cursorShadow;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETGRADIENTCAPTIONS
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool GradientCaptions
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.GradientCaptions])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETGRADIENTCAPTIONS, 0, ref _gradientCaptions, 0))
                        {
                            _cacheValid[(int)CacheSlot.GradientCaptions] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _gradientCaptions;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETHOTTRACKING
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool HotTracking
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.HotTracking])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETHOTTRACKING, 0, ref _hotTracking, 0))
                        {
                            _cacheValid[(int)CacheSlot.HotTracking] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _hotTracking;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETLISTBOXSMOOTHSCROLLING
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool ListBoxSmoothScrolling
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ListBoxSmoothScrolling])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETLISTBOXSMOOTHSCROLLING, 0, ref _listBoxSmoothScrolling, 0))
                        {
                            _cacheValid[(int)CacheSlot.ListBoxSmoothScrolling] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _listBoxSmoothScrolling;
            }
        }

        /// <summary>
        ///     Returns the PopupAnimation value for Menus.
        /// </summary>
        public static PopupAnimation MenuPopupAnimation
        {
            get
            {
                if (MenuAnimation)
                {
                    if (MenuFade)
                    {
                        return PopupAnimation.Fade;
                    }
                    else
                    {
                        return PopupAnimation.Scroll;
                    }
                }

                return PopupAnimation.None;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETMENUANIMATION
        /// </summary>
        /// <SecurityNote>
        ///     Critical - calls SystemParametersInfo
        ///     PublicOK - net information returned is whether menu-animation is enabled. Considered safe.
        /// </SecurityNote>
        public static bool MenuAnimation
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuAnimation])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMENUANIMATION, 0, ref _menuAnimation, 0))
                        {
                            _cacheValid[(int)CacheSlot.MenuAnimation] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _menuAnimation;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETSELECTIONFADE
        /// </summary>
        ///<SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        ///</SecurityNote>
        public static bool SelectionFade
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.SelectionFade])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETSELECTIONFADE, 0, ref _selectionFade, 0))
                        {
                            _cacheValid[(int)CacheSlot.SelectionFade] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _selectionFade;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETSTYLUSHOTTRACKING
        /// </summary>
        ///<SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        ///</SecurityNote>
        public static bool StylusHotTracking
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.StylusHotTracking])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETSTYLUSHOTTRACKING, 0, ref _stylusHotTracking, 0))
                        {
                            _cacheValid[(int)CacheSlot.StylusHotTracking] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _stylusHotTracking;
            }
        }

        /// <summary>
        ///     Returns the PopupAnimation value for ToolTips.
        /// </summary>
        public static PopupAnimation ToolTipPopupAnimation
        {
            get
            {
                // Win32 ToolTips do not appear to scroll, only fade
                if (ToolTipAnimation && ToolTipFade)
                {
                    return PopupAnimation.Fade;
                }

                return PopupAnimation.None;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETTOOLTIPANIMATION
        /// </summary>
        ///<SecurityNote>
        /// Critical as this code elevates.
        /// PublicOK - as we think this is ok to expose.
        ///</SecurityNote>
        public static bool ToolTipAnimation
        {
            [SecurityCritical ]
            get
            {


                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ToolTipAnimation])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETTOOLTIPANIMATION, 0, ref _toolTipAnimation, 0))
                        {
                            _cacheValid[(int)CacheSlot.ToolTipAnimation] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _toolTipAnimation;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETTOOLTIPFADE
        /// </summary>
        ///<SecurityNote>
        /// Critical as this code elevates.
        /// PublicOK - as we think this is ok to expose.
        ///</SecurityNote>
        public static bool ToolTipFade
        {
            [SecurityCritical ]
            get
            {

                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ToolTipFade])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETTOOLTIPFADE, 0, ref _tooltipFade, 0))
                        {
                            _cacheValid[(int)CacheSlot.ToolTipFade] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _tooltipFade;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETUIEFFECTS
        /// </summary>
        ///<SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        ///</SecurityNote>
        public static bool UIEffects
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.UIEffects])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETUIEFFECTS, 0, ref _uiEffects, 0))
                        {
                            _cacheValid[(int)CacheSlot.UIEffects] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _uiEffects;
            }
        }

        #endregion

        #region UI Effects Keys

        /// <summary>
        ///     ComboBoxAnimation System Resource Key
        /// </summary>
        public static ResourceKey ComboBoxAnimationKey
        {
            get
            {
                if (_cacheComboBoxAnimation == null)
                {
                    _cacheComboBoxAnimation = CreateInstance(SystemResourceKeyID.ComboBoxAnimation);
                }

                return _cacheComboBoxAnimation;
            }
        }

        /// <summary>
        ///     ClientAreaAnimation System Resource Key
        /// </summary>
        public static ResourceKey ClientAreaAnimationKey
        {
            get
            {
                if (_cacheClientAreaAnimation == null)
                {
                    _cacheClientAreaAnimation = CreateInstance(SystemResourceKeyID.ClientAreaAnimation);
                }

                return _cacheClientAreaAnimation;
            }
        }

        /// <summary>
        ///     CursorShadow System Resource Key
        /// </summary>
        public static ResourceKey CursorShadowKey
        {
            get
            {
                if (_cacheCursorShadow == null)
                {
                    _cacheCursorShadow = CreateInstance(SystemResourceKeyID.CursorShadow);
                }

                return _cacheCursorShadow;
            }
        }

        /// <summary>
        ///     GradientCaptions System Resource Key
        /// </summary>
        public static ResourceKey GradientCaptionsKey
        {
            get
            {
                if (_cacheGradientCaptions == null)
                {
                    _cacheGradientCaptions = CreateInstance(SystemResourceKeyID.GradientCaptions);
                }

                return _cacheGradientCaptions;
            }
        }

        /// <summary>
        ///     HotTracking System Resource Key
        /// </summary>
        public static ResourceKey HotTrackingKey
        {
            get
            {
                if (_cacheHotTracking == null)
                {
                    _cacheHotTracking = CreateInstance(SystemResourceKeyID.HotTracking);
                }

                return _cacheHotTracking;
            }
        }

        /// <summary>
        ///     ListBoxSmoothScrolling System Resource Key
        /// </summary>
        public static ResourceKey ListBoxSmoothScrollingKey
        {
            get
            {
                if (_cacheListBoxSmoothScrolling == null)
                {
                    _cacheListBoxSmoothScrolling = CreateInstance(SystemResourceKeyID.ListBoxSmoothScrolling);
                }

                return _cacheListBoxSmoothScrolling;
            }
        }

        /// <summary>
        ///     MenuAnimation System Resource Key
        /// </summary>
        public static ResourceKey MenuAnimationKey
        {
            get
            {
                if (_cacheMenuAnimation == null)
                {
                    _cacheMenuAnimation = CreateInstance(SystemResourceKeyID.MenuAnimation);
                }

                return _cacheMenuAnimation;
            }
        }

        /// <summary>
        ///     SelectionFade System Resource Key
        /// </summary>
        public static ResourceKey SelectionFadeKey
        {
            get
            {
                if (_cacheSelectionFade == null)
                {
                    _cacheSelectionFade = CreateInstance(SystemResourceKeyID.SelectionFade);
                }

                return _cacheSelectionFade;
            }
        }

        /// <summary>
        ///     StylusHotTracking System Resource Key
        /// </summary>
        public static ResourceKey StylusHotTrackingKey
        {
            get
            {
                if (_cacheStylusHotTracking == null)
                {
                    _cacheStylusHotTracking = CreateInstance(SystemResourceKeyID.StylusHotTracking);
                }

                return _cacheStylusHotTracking;
            }
        }

        /// <summary>
        ///     ToolTipAnimation System Resource Key
        /// </summary>
        public static ResourceKey ToolTipAnimationKey
        {
            get
            {
                if (_cacheToolTipAnimation == null)
                {
                    _cacheToolTipAnimation = CreateInstance(SystemResourceKeyID.ToolTipAnimation);
                }

                return _cacheToolTipAnimation;
            }
        }

        /// <summary>
        ///     ToolTipFade System Resource Key
        /// </summary>
        public static ResourceKey ToolTipFadeKey
        {
            get
            {
                if (_cacheToolTipFade == null)
                {
                    _cacheToolTipFade = CreateInstance(SystemResourceKeyID.ToolTipFade);
                }

                return _cacheToolTipFade;
            }
        }

        /// <summary>
        ///     UIEffects System Resource Key
        /// </summary>
        public static ResourceKey UIEffectsKey
        {
            get
            {
                if (_cacheUIEffects == null)
                {
                    _cacheUIEffects = CreateInstance(SystemResourceKeyID.UIEffects);
                }

                return _cacheUIEffects;
            }
        }

        /// <summary>
        ///     ComboBoxPopupAnimation System Resource Key
        /// </summary>
        public static ResourceKey ComboBoxPopupAnimationKey
        {
            get
            {
                if (_cacheComboBoxPopupAnimation == null)
                {
                    _cacheComboBoxPopupAnimation = CreateInstance(SystemResourceKeyID.ComboBoxPopupAnimation);
                }

                return _cacheComboBoxPopupAnimation;
            }
        }

        /// <summary>
        ///     MenuPopupAnimation System Resource Key
        /// </summary>
        public static ResourceKey MenuPopupAnimationKey
        {
            get
            {
                if (_cacheMenuPopupAnimation == null)
                {
                    _cacheMenuPopupAnimation = CreateInstance(SystemResourceKeyID.MenuPopupAnimation);
                }

                return _cacheMenuPopupAnimation;
            }
        }

        /// <summary>
        ///     ToolTipPopupAnimation System Resource Key
        /// </summary>
        public static ResourceKey ToolTipPopupAnimationKey
        {
            get
            {
                if (_cacheToolTipPopupAnimation == null)
                {
                    _cacheToolTipPopupAnimation = CreateInstance(SystemResourceKeyID.ToolTipPopupAnimation);
                }

                return _cacheToolTipPopupAnimation;
            }
        }

        #endregion

        #region Window Parameters

        /// <summary>
        ///     Maps to SPI_GETANIMATION
        /// </summary>
        ///<SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        ///</SecurityNote>
        public static bool MinimizeAnimation
        {
            [SecurityCritical ]
            get
            {

                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimizeAnimation])
                    {
                        NativeMethods.ANIMATIONINFO animInfo = new NativeMethods.ANIMATIONINFO();

                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETANIMATION, animInfo.cbSize, animInfo, 0))
                        {
                            _minAnimation = animInfo.iMinAnimate != 0;
                            _cacheValid[(int)CacheSlot.MinimizeAnimation] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _minAnimation;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETBORDER
        /// </summary>
        ///<SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        ///</SecurityNote>
        public static int Border
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {

                    if (!_cacheValid[(int)CacheSlot.Border])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETBORDER, 0, ref _border, 0))
                        {
                            _cacheValid[(int)CacheSlot.Border] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _border;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETCARETWIDTH
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK -- Determined safe: getting width of caret
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double CaretWidth
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.CaretWidth])
                    {
                        int caretWidth = 0;

                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETCARETWIDTH, 0, ref caretWidth, 0))
                        {
                            _caretWidth = ConvertPixel(caretWidth);
                            _cacheValid[(int)CacheSlot.CaretWidth] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _caretWidth;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETDRAGFULLWINDOWS
        /// </summary>
        /// <SecurityNote>
        ///  SecurityCritical because it calls an unsafe native method.
        ///  PublicOK - Okay to expose info to internet callers.
        /// </SecurityNote>
        public static bool DragFullWindows
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.DragFullWindows])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETDRAGFULLWINDOWS, 0, ref _dragFullWindows, 0))
                        {
                            _cacheValid[(int)CacheSlot.DragFullWindows] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _dragFullWindows;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETFOREGROUNDFLASHCOUNT
        /// </summary>
        /// <SecurityNote>
        ///     Get is PublicOK -- Getting # of times taskbar button will flash when rejecting a forecground switch request.
        ///     Get is Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>

        public static int ForegroundFlashCount
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ForegroundFlashCount])
                    {
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFOREGROUNDFLASHCOUNT, 0, ref _foregroundFlashCount, 0))
                        {
                            _cacheValid[(int)CacheSlot.ForegroundFlashCount] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _foregroundFlashCount;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETNONCLIENTMETRICS
        /// </summary>
        /// <SecurityNote>
        ///      SecurityCritical because it calls an unsafe native method.
        ///      SecurityTreatAsSafe as we think this would be ok to expose publically - and this is ok for consumption in partial trust.
        /// </SecurityNote>
        internal static NativeMethods.NONCLIENTMETRICS NonClientMetrics
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.NonClientMetrics])
                    {
                        _ncm = new NativeMethods.NONCLIENTMETRICS();
                        if (UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, _ncm.cbSize, _ncm, 0))
                        {
                            _cacheValid[(int)CacheSlot.NonClientMetrics] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _ncm;
            }
        }

        /// <summary>
        ///     From SPI_GETNONCLIENTMETRICS
        /// </summary>
        public static double BorderWidth
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iBorderWidth);
            }
        }

        /// <summary>
        ///     From SPI_GETNONCLIENTMETRICS
        /// </summary>
        public static double ScrollWidth
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iScrollWidth);
            }
        }

        /// <summary>
        ///     From SPI_GETNONCLIENTMETRICS
        /// </summary>
        public static double ScrollHeight
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iScrollHeight);
            }
        }

        /// <summary>
        ///     From SPI_GETNONCLIENTMETRICS
        /// </summary>
        public static double CaptionWidth
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iCaptionWidth);
            }
        }

        /// <summary>
        ///     From SPI_GETNONCLIENTMETRICS
        /// </summary>
        public static double CaptionHeight
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iCaptionHeight);
            }
        }

        /// <summary>
        ///     From SPI_GETNONCLIENTMETRICS
        /// </summary>
        public static double SmallCaptionWidth
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iSmCaptionWidth);
            }
        }

        /// <summary>
        ///     From SPI_NONCLIENTMETRICS
        /// </summary>
        public static double SmallCaptionHeight
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iSmCaptionHeight);
            }
        }

        /// <summary>
        ///     From SPI_NONCLIENTMETRICS
        /// </summary>
        public static double MenuWidth
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iMenuWidth);
            }
        }

        /// <summary>
        ///     From SPI_NONCLIENTMETRICS
        /// </summary>
        public static double MenuHeight
        {
            get
            {
                return ConvertPixel(NonClientMetrics.iMenuHeight);
            }
        }

        #endregion

        #region Window Parameters Keys

        /// <summary>
        ///     MinimizeAnimation System Resource Key
        /// </summary>
        public static ResourceKey MinimizeAnimationKey
        {
            get
            {
                if (_cacheMinimizeAnimation == null)
                {
                    _cacheMinimizeAnimation = CreateInstance(SystemResourceKeyID.MinimizeAnimation);
                }

                return _cacheMinimizeAnimation;
            }
        }

        /// <summary>
        ///     Border System Resource Key
        /// </summary>
        public static ResourceKey BorderKey
        {
            get
            {
                if (_cacheBorder == null)
                {
                    _cacheBorder = CreateInstance(SystemResourceKeyID.Border);
                }

                return _cacheBorder;
            }
        }

        /// <summary>
        ///     CaretWidth System Resource Key
        /// </summary>
        public static ResourceKey CaretWidthKey
        {
            get
            {
                if (_cacheCaretWidth == null)
                {
                    _cacheCaretWidth = CreateInstance(SystemResourceKeyID.CaretWidth);
                }

                return _cacheCaretWidth;
            }
        }

        /// <summary>
        ///     ForegroundFlashCount System Resource Key
        /// </summary>
        public static ResourceKey ForegroundFlashCountKey
        {
            get
            {
                if (_cacheForegroundFlashCount == null)
                {
                    _cacheForegroundFlashCount = CreateInstance(SystemResourceKeyID.ForegroundFlashCount);
                }

                return _cacheForegroundFlashCount;
            }
        }

        /// <summary>
        ///     DragFullWindows System Resource Key
        /// </summary>
        public static ResourceKey DragFullWindowsKey
        {
            get
            {
                if (_cacheDragFullWindows == null)
                {
                    _cacheDragFullWindows = CreateInstance(SystemResourceKeyID.DragFullWindows);
                }

                return _cacheDragFullWindows;
            }
        }

        /// <summary>
        ///     BorderWidth System Resource Key
        /// </summary>
        public static ResourceKey BorderWidthKey
        {
            get
            {
                if (_cacheBorderWidth == null)
                {
                    _cacheBorderWidth = CreateInstance(SystemResourceKeyID.BorderWidth);
                }

                return _cacheBorderWidth;
            }
        }

        /// <summary>
        ///     ScrollWidth System Resource Key
        /// </summary>
        public static ResourceKey ScrollWidthKey
        {
            get
            {
                if (_cacheScrollWidth == null)
                {
                    _cacheScrollWidth = CreateInstance(SystemResourceKeyID.ScrollWidth);
                }

                return _cacheScrollWidth;
            }
        }

        /// <summary>
        ///     ScrollHeight System Resource Key
        /// </summary>
        public static ResourceKey ScrollHeightKey
        {
            get
            {
                if (_cacheScrollHeight == null)
                {
                    _cacheScrollHeight = CreateInstance(SystemResourceKeyID.ScrollHeight);
                }

                return _cacheScrollHeight;
            }
        }

        /// <summary>
        ///     CaptionWidth System Resource Key
        /// </summary>
        public static ResourceKey CaptionWidthKey
        {
            get
            {
                if (_cacheCaptionWidth == null)
                {
                    _cacheCaptionWidth = CreateInstance(SystemResourceKeyID.CaptionWidth);
                }

                return _cacheCaptionWidth;
            }
        }

        /// <summary>
        ///     CaptionHeight System Resource Key
        /// </summary>
        public static ResourceKey CaptionHeightKey
        {
            get
            {
                if (_cacheCaptionHeight == null)
                {
                    _cacheCaptionHeight = CreateInstance(SystemResourceKeyID.CaptionHeight);
                }

                return _cacheCaptionHeight;
            }
        }

        /// <summary>
        ///     SmallCaptionWidth System Resource Key
        /// </summary>
        public static ResourceKey SmallCaptionWidthKey
        {
            get
            {
                if (_cacheSmallCaptionWidth == null)
                {
                    _cacheSmallCaptionWidth = CreateInstance(SystemResourceKeyID.SmallCaptionWidth);
                }

                return _cacheSmallCaptionWidth;
            }
        }

        /// <summary>
        ///     MenuWidth System Resource Key
        /// </summary>
        public static ResourceKey MenuWidthKey
        {
            get
            {
                if (_cacheMenuWidth == null)
                {
                    _cacheMenuWidth = CreateInstance(SystemResourceKeyID.MenuWidth);
                }

                return _cacheMenuWidth;
            }
        }

        /// <summary>
        ///     MenuHeight System Resource Key
        /// </summary>
        public static ResourceKey MenuHeightKey
        {
            get
            {
                if (_cacheMenuHeight == null)
                {
                    _cacheMenuHeight = CreateInstance(SystemResourceKeyID.MenuHeight);
                }

                return _cacheMenuHeight;
            }
        }

        #endregion

        #region Metrics

        /// <summary>
        ///     Maps to SM_CXBORDER
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double ThinHorizontalBorderHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ThinHorizontalBorderHeight])
                    {
                        _thinHorizontalBorderHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXBORDER));
                        _cacheValid[(int)CacheSlot.ThinHorizontalBorderHeight] = true;
                    }
                }

                return _thinHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYBORDER
        /// </summary>
        ///
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double ThinVerticalBorderWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ThinVerticalBorderWidth])
                    {
                        _thinVerticalBorderWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYBORDER));
                        _cacheValid[(int)CacheSlot.ThinVerticalBorderWidth] = true;
                    }
                }

                return _thinVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CXCURSOR
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double CursorWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.CursorWidth])
                    {
                        _cursorWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXCURSOR));
                        _cacheValid[(int)CacheSlot.CursorWidth] = true;
                    }
                }

                return _cursorWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYCURSOR
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double CursorHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.CursorHeight])
                    {
                        _cursorHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYCURSOR));
                        _cacheValid[(int)CacheSlot.CursorHeight] = true;
                    }
                }

                return _cursorHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXEDGE
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double ThickHorizontalBorderHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ThickHorizontalBorderHeight])
                    {
                        _thickHorizontalBorderHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXEDGE));
                        _cacheValid[(int)CacheSlot.ThickHorizontalBorderHeight] = true;
                    }
                }

                return _thickHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYEDGE
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double ThickVerticalBorderWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ThickVerticalBorderWidth])
                    {
                        _thickVerticalBorderWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYEDGE));
                        _cacheValid[(int)CacheSlot.ThickVerticalBorderWidth] = true;
                    }
                }

                return _thickVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CXDRAG
        /// </summary>
        /// <SecurityNote>
        ///    Critical - calls into native code (GetSystemMetrics)
        ///    PublicOK - Safe data to expose
        /// </SecurityNote>
        public static double MinimumHorizontalDragDistance
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimumHorizontalDragDistance])
                    {
                        _minimumHorizontalDragDistance = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXDRAG));
                        _cacheValid[(int)CacheSlot.MinimumHorizontalDragDistance] = true;
                    }
                }

                return _minimumHorizontalDragDistance;
            }
        }

        /// <summary>
        ///     Maps to SM_CYDRAG
        /// </summary>
        /// <SecurityNote>
        ///    Critical - calls into native code (GetSystemMetrics)
        ///    PublicOK - Safe data to expose
        /// </SecurityNote>
        public static double MinimumVerticalDragDistance
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimumVerticalDragDistance])
                    {
                        _minimumVerticalDragDistance = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYDRAG));
                        _cacheValid[(int)CacheSlot.MinimumVerticalDragDistance] = true;
                    }
                }

                return _minimumVerticalDragDistance;
            }
        }

        /// <summary>
        ///     Maps to SM_CXFIXEDFRAME
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FixedFrameHorizontalBorderHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FixedFrameHorizontalBorderHeight])
                    {
                        _fixedFrameHorizontalBorderHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXFIXEDFRAME));
                        _cacheValid[(int)CacheSlot.FixedFrameHorizontalBorderHeight] = true;
                    }
                }

                return _fixedFrameHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYFIXEDFRAME
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FixedFrameVerticalBorderWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FixedFrameVerticalBorderWidth])
                    {
                        _fixedFrameVerticalBorderWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYFIXEDFRAME));
                        _cacheValid[(int)CacheSlot.FixedFrameVerticalBorderWidth] = true;
                    }
                }

                return _fixedFrameVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CXFOCUSBORDER
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FocusHorizontalBorderHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FocusHorizontalBorderHeight])
                    {
                        _focusHorizontalBorderHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXFOCUSBORDER));
                        _cacheValid[(int)CacheSlot.FocusHorizontalBorderHeight] = true;
                    }
                }

                return _focusHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYFOCUSBORDER
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FocusVerticalBorderWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FocusVerticalBorderWidth])
                    {
                        _focusVerticalBorderWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYFOCUSBORDER));
                        _cacheValid[(int)CacheSlot.FocusVerticalBorderWidth] = true;
                    }
                }

                return _focusVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CXFULLSCREEN
        /// </summary>
        ///
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FullPrimaryScreenWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FullPrimaryScreenWidth])
                    {
                        _fullPrimaryScreenWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXFULLSCREEN));
                        _cacheValid[(int)CacheSlot.FullPrimaryScreenWidth] = true;
                    }
                }

                return _fullPrimaryScreenWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYFULLSCREEN
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double FullPrimaryScreenHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.FullPrimaryScreenHeight])
                    {
                        _fullPrimaryScreenHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYFULLSCREEN));
                        _cacheValid[(int)CacheSlot.FullPrimaryScreenHeight] = true;
                    }
                }

                return _fullPrimaryScreenHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXHSCROLL
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double HorizontalScrollBarButtonWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.HorizontalScrollBarButtonWidth])
                    {
                        _horizontalScrollBarButtonWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXHSCROLL));
                        _cacheValid[(int)CacheSlot.HorizontalScrollBarButtonWidth] = true;
                    }
                }

                return _horizontalScrollBarButtonWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYHSCROLL
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double HorizontalScrollBarHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.HorizontalScrollBarHeight])
                    {
                        _horizontalScrollBarHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYHSCROLL));
                        _cacheValid[(int)CacheSlot.HorizontalScrollBarHeight] = true;
                    }
                }

                return _horizontalScrollBarHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXHTHUMB
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double HorizontalScrollBarThumbWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.HorizontalScrollBarThumbWidth])
                    {
                        _horizontalScrollBarThumbWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXHTHUMB));
                        _cacheValid[(int)CacheSlot.HorizontalScrollBarThumbWidth] = true;
                    }
                }

                return _horizontalScrollBarThumbWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CXICON
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double IconWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IconWidth])
                    {
                        _iconWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXICON));
                        _cacheValid[(int)CacheSlot.IconWidth] = true;
                    }
                }

                return _iconWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYICON
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double IconHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IconHeight])
                    {
                        _iconHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYICON));
                        _cacheValid[(int)CacheSlot.IconHeight] = true;
                    }
                }

                return _iconHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXICONSPACING
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double IconGridWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IconGridWidth])
                    {
                        _iconGridWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXICONSPACING));
                        _cacheValid[(int)CacheSlot.IconGridWidth] = true;
                    }
                }

                return _iconGridWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYICONSPACING
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double IconGridHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IconGridHeight])
                    {
                        _iconGridHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYICONSPACING));
                        _cacheValid[(int)CacheSlot.IconGridHeight] = true;
                    }
                }

                return _iconGridHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMAXIMIZED
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MaximizedPrimaryScreenWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MaximizedPrimaryScreenWidth])
                    {
                        _maximizedPrimaryScreenWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMAXIMIZED));
                        _cacheValid[(int)CacheSlot.MaximizedPrimaryScreenWidth] = true;
                    }
                }

                return _maximizedPrimaryScreenWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMAXIMIZED
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MaximizedPrimaryScreenHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MaximizedPrimaryScreenHeight])
                    {
                        _maximizedPrimaryScreenHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMAXIMIZED));
                        _cacheValid[(int)CacheSlot.MaximizedPrimaryScreenHeight] = true;
                    }
                }

                return _maximizedPrimaryScreenHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMAXTRACK
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MaximumWindowTrackWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MaximumWindowTrackWidth])
                    {
                        _maximumWindowTrackWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMAXTRACK));
                        _cacheValid[(int)CacheSlot.MaximumWindowTrackWidth] = true;
                    }
                }

                return _maximumWindowTrackWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMAXTRACK
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MaximumWindowTrackHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MaximumWindowTrackHeight])
                    {
                        _maximumWindowTrackHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMAXTRACK));
                        _cacheValid[(int)CacheSlot.MaximumWindowTrackHeight] = true;
                    }
                }

                return _maximumWindowTrackHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMENUCHECK
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MenuCheckmarkWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuCheckmarkWidth])
                    {
                        _menuCheckmarkWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMENUCHECK));
                        _cacheValid[(int)CacheSlot.MenuCheckmarkWidth] = true;
                    }
                }

                return _menuCheckmarkWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMENUCHECK
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MenuCheckmarkHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuCheckmarkHeight])
                    {
                        _menuCheckmarkHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMENUCHECK));
                        _cacheValid[(int)CacheSlot.MenuCheckmarkHeight] = true;
                    }
                }

                return _menuCheckmarkHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMENUSIZE
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MenuButtonWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuButtonWidth])
                    {
                        _menuButtonWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMENUSIZE));
                        _cacheValid[(int)CacheSlot.MenuButtonWidth] = true;
                    }
                }

                return _menuButtonWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMENUSIZE
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MenuButtonHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuButtonHeight])
                    {
                        _menuButtonHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMENUSIZE));
                        _cacheValid[(int)CacheSlot.MenuButtonHeight] = true;
                    }
                }

                return _menuButtonHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMIN
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimumWindowWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimumWindowWidth])
                    {
                        _minimumWindowWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMIN));
                        _cacheValid[(int)CacheSlot.MinimumWindowWidth] = true;
                    }
                }

                return _minimumWindowWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMIN
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimumWindowHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimumWindowHeight])
                    {
                        _minimumWindowHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMIN));
                        _cacheValid[(int)CacheSlot.MinimumWindowHeight] = true;
                    }
                }

                return _minimumWindowHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMINIMIZED
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK -- There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimizedWindowWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimizedWindowWidth])
                    {
                        _minimizedWindowWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMINIMIZED));
                        _cacheValid[(int)CacheSlot.MinimizedWindowWidth] = true;
                    }
                }

                return _minimizedWindowWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMINIMIZED
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimizedWindowHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimizedWindowHeight])
                    {
                        _minimizedWindowHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMINIMIZED));
                        _cacheValid[(int)CacheSlot.MinimizedWindowHeight] = true;
                    }
                }

                return _minimizedWindowHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMINSPACING
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimizedGridWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimizedGridWidth])
                    {
                        _minimizedGridWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMINSPACING));
                        _cacheValid[(int)CacheSlot.MinimizedGridWidth] = true;
                    }
                }

                return _minimizedGridWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMINSPACING
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimizedGridHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimizedGridHeight])
                    {
                        _minimizedGridHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMINSPACING));
                        _cacheValid[(int)CacheSlot.MinimizedGridHeight] = true;
                    }
                }

                return _minimizedGridHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXMINTRACK
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exist a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimumWindowTrackWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimumWindowTrackWidth])
                    {
                        _minimumWindowTrackWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXMINTRACK));
                        _cacheValid[(int)CacheSlot.MinimumWindowTrackWidth] = true;
                    }
                }

                return _minimumWindowTrackWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMINTRACK
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MinimumWindowTrackHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MinimumWindowTrackHeight])
                    {
                        _minimumWindowTrackHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMINTRACK));
                        _cacheValid[(int)CacheSlot.MinimumWindowTrackHeight] = true;
                    }
                }

                return _minimumWindowTrackHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXSCREEN
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --  This is safe to expose
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double PrimaryScreenWidth
        {
            [SecurityCritical ]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.PrimaryScreenWidth])
                    {
                        _primaryScreenWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXSCREEN));
                        _cacheValid[(int)CacheSlot.PrimaryScreenWidth] = true;
                    }
                }

                return _primaryScreenWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYSCREEN
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --This is safe to expose
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double PrimaryScreenHeight
        {
            [SecurityCritical]
            get
            {

                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.PrimaryScreenHeight])
                    {
                        _primaryScreenHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYSCREEN));
                        _cacheValid[(int)CacheSlot.PrimaryScreenHeight] = true;
                    }
                }

                return _primaryScreenHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXSIZE
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double WindowCaptionButtonWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowCaptionButtonWidth])
                    {
                        _windowCaptionButtonWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXSIZE));
                        _cacheValid[(int)CacheSlot.WindowCaptionButtonWidth] = true;
                    }
                }

                return _windowCaptionButtonWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYSIZE
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double WindowCaptionButtonHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowCaptionButtonHeight])
                    {
                        _windowCaptionButtonHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYSIZE));
                        _cacheValid[(int)CacheSlot.WindowCaptionButtonHeight] = true;
                    }
                }

                return _windowCaptionButtonHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXSIZEFRAME
        /// </summary>
        ///
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double ResizeFrameHorizontalBorderHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ResizeFrameHorizontalBorderHeight])
                    {
                        _resizeFrameHorizontalBorderHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXSIZEFRAME));
                        _cacheValid[(int)CacheSlot.ResizeFrameHorizontalBorderHeight] = true;
                    }
                }

                return _resizeFrameHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYSIZEFRAME
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double ResizeFrameVerticalBorderWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ResizeFrameVerticalBorderWidth])
                    {
                        _resizeFrameVerticalBorderWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYSIZEFRAME));
                        _cacheValid[(int)CacheSlot.ResizeFrameVerticalBorderWidth] = true;
                    }
                }

                return _resizeFrameVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CXSMICON
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double SmallIconWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.SmallIconWidth])
                    {
                        _smallIconWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXSMICON));
                        _cacheValid[(int)CacheSlot.SmallIconWidth] = true;
                    }
                }

                return _smallIconWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYSMICON
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double SmallIconHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.SmallIconHeight])
                    {
                        _smallIconHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYSMICON));
                        _cacheValid[(int)CacheSlot.SmallIconHeight] = true;
                    }
                }

                return _smallIconHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXSMSIZE
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double SmallWindowCaptionButtonWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.SmallWindowCaptionButtonWidth])
                    {
                        _smallWindowCaptionButtonWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXSMSIZE));
                        _cacheValid[(int)CacheSlot.SmallWindowCaptionButtonWidth] = true;
                    }
                }

                return _smallWindowCaptionButtonWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYSMSIZE
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double SmallWindowCaptionButtonHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.SmallWindowCaptionButtonHeight])
                    {
                        _smallWindowCaptionButtonHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYSMSIZE));
                        _cacheValid[(int)CacheSlot.SmallWindowCaptionButtonHeight] = true;
                    }
                }

                return _smallWindowCaptionButtonHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXVIRTUALSCREEN
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double VirtualScreenWidth
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.VirtualScreenWidth])
                    {
                        _virtualScreenWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXVIRTUALSCREEN));
                        _cacheValid[(int)CacheSlot.VirtualScreenWidth] = true;
                    }
                }

                return _virtualScreenWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYVIRTUALSCREEN
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double VirtualScreenHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.VirtualScreenHeight])
                    {
                        _virtualScreenHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYVIRTUALSCREEN));
                        _cacheValid[(int)CacheSlot.VirtualScreenHeight] = true;
                    }
                }

                return _virtualScreenHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CXVSCROLL
        /// </summary>
        ///
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double VerticalScrollBarWidth
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.VerticalScrollBarWidth])
                    {
                        _verticalScrollBarWidth = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CXVSCROLL));
                        _cacheValid[(int)CacheSlot.VerticalScrollBarWidth] = true;
                    }
                }

                return _verticalScrollBarWidth;
            }
        }

        /// <summary>
        ///     Maps to SM_CYVSCROLL
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double VerticalScrollBarButtonHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.VerticalScrollBarButtonHeight])
                    {
                        _verticalScrollBarButtonHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYVSCROLL));
                        _cacheValid[(int)CacheSlot.VerticalScrollBarButtonHeight] = true;
                    }
                }

                return _verticalScrollBarButtonHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYCAPTION
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double WindowCaptionHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowCaptionHeight])
                    {
                        _windowCaptionHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYCAPTION));
                        _cacheValid[(int)CacheSlot.WindowCaptionHeight] = true;
                    }
                }

                return _windowCaptionHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYKANJIWINDOW
        /// </summary>
        ///
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double KanjiWindowHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.KanjiWindowHeight])
                    {
                        _kanjiWindowHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYKANJIWINDOW));
                        _cacheValid[(int)CacheSlot.KanjiWindowHeight] = true;
                    }
                }

                return _kanjiWindowHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYMENU
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double MenuBarHeight
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.MenuBarHeight])
                    {
                        _menuBarHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYMENU));
                        _cacheValid[(int)CacheSlot.MenuBarHeight] = true;
                    }
                }

                return _menuBarHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_CYVTHUMB
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double VerticalScrollBarThumbHeight
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.VerticalScrollBarThumbHeight])
                    {
                        _verticalScrollBarThumbHeight = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.CYVTHUMB));
                        _cacheValid[(int)CacheSlot.VerticalScrollBarThumbHeight] = true;
                    }
                }

                return _verticalScrollBarThumbHeight;
            }
        }

        /// <summary>
        ///     Maps to SM_IMMENABLED
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand in this code.
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsImmEnabled
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsImmEnabled])
                    {
                        _isImmEnabled = UnsafeNativeMethods.GetSystemMetrics(SM.IMMENABLED) != 0;
                        _cacheValid[(int)CacheSlot.IsImmEnabled] = true;
                    }
                }

                return _isImmEnabled;
            }
        }

        /// <summary>
        ///     Maps to SM_MEDIACENTER
        /// </summary>
        ///
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsMediaCenter
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsMediaCenter])
                    {
                        _isMediaCenter = UnsafeNativeMethods.GetSystemMetrics(SM.MEDIACENTER) != 0;
                        _cacheValid[(int)CacheSlot.IsMediaCenter] = true;
                    }
                }

                return _isMediaCenter;
            }
        }

        /// <summary>
        ///     Maps to SM_MENUDROPALIGNMENT
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsMenuDropRightAligned
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsMenuDropRightAligned])
                    {
                        _isMenuDropRightAligned = UnsafeNativeMethods.GetSystemMetrics(SM.MENUDROPALIGNMENT) != 0;
                        _cacheValid[(int)CacheSlot.IsMenuDropRightAligned] = true;
                    }
                }

                return _isMenuDropRightAligned;
            }
        }

        /// <summary>
        ///     Maps to SM_MIDEASTENABLED
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --There exists a demand
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsMiddleEastEnabled
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsMiddleEastEnabled])
                    {
                        _isMiddleEastEnabled = UnsafeNativeMethods.GetSystemMetrics(SM.MIDEASTENABLED) != 0;
                        _cacheValid[(int)CacheSlot.IsMiddleEastEnabled] = true;
                    }
                }

                return _isMiddleEastEnabled;
            }
        }

        /// <summary>
        ///     Maps to SM_MOUSEPRESENT
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsMousePresent
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsMousePresent])
                    {
                        _isMousePresent = UnsafeNativeMethods.GetSystemMetrics(SM.MOUSEPRESENT) != 0;
                        _cacheValid[(int)CacheSlot.IsMousePresent] = true;
                    }
                }

                return _isMousePresent;
            }
        }

        /// <summary>
        ///     Maps to SM_MOUSEWHEELPRESENT
        /// </summary>
        /// <SecurityNote>
        ///    PublicOK --System Metrics are deemed safe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsMouseWheelPresent
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsMouseWheelPresent])
                    {
                        _isMouseWheelPresent = UnsafeNativeMethods.GetSystemMetrics(SM.MOUSEWHEELPRESENT) != 0;
                        _cacheValid[(int)CacheSlot.IsMouseWheelPresent] = true;
                    }
                }

                return _isMouseWheelPresent;
            }
        }

        /// <summary>
        ///     Maps to SM_PENWINDOWS
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Deemed as unsafe
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsPenWindows
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsPenWindows])
                    {
                        _isPenWindows = UnsafeNativeMethods.GetSystemMetrics(SM.PENWINDOWS) != 0;
                        _cacheValid[(int)CacheSlot.IsPenWindows] = true;
                    }
                }

                return _isPenWindows;
            }
        }

        /// <summary>
        ///     Maps to SM_REMOTECONTROL
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Demands unmanaged Code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsRemotelyControlled
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsRemotelyControlled])
                    {
                        _isRemotelyControlled = UnsafeNativeMethods.GetSystemMetrics(SM.REMOTECONTROL) != 0;
                        _cacheValid[(int)CacheSlot.IsRemotelyControlled] = true;
                    }
                }

                return _isRemotelyControlled;
            }
        }

        /// <summary>
        ///     Maps to SM_REMOTESESSION
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Demand Unmanaged Code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsRemoteSession
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsRemoteSession])
                    {
                        _isRemoteSession = UnsafeNativeMethods.GetSystemMetrics(SM.REMOTESESSION) != 0;
                        _cacheValid[(int)CacheSlot.IsRemoteSession] = true;
                    }
                }

                return _isRemoteSession;
            }
        }

        /// <summary>
        ///     Maps to SM_SHOWSOUNDS
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Demand Unmanaged Code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool ShowSounds
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.ShowSounds])
                    {
                        _showSounds = UnsafeNativeMethods.GetSystemMetrics(SM.SHOWSOUNDS) != 0;
                        _cacheValid[(int)CacheSlot.ShowSounds] = true;
                    }
                }

                return _showSounds;
            }
        }

        /// <summary>
        ///     Maps to SM_SLOWMACHINE
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Demands unmanaged code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsSlowMachine
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsSlowMachine])
                    {
                        _isSlowMachine = UnsafeNativeMethods.GetSystemMetrics(SM.SLOWMACHINE) != 0;
                        _cacheValid[(int)CacheSlot.IsSlowMachine] = true;
                    }
                }

                return _isSlowMachine;
            }
        }

        /// <summary>
        ///     Maps to SM_SWAPBUTTON
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Demands unmanaged code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool SwapButtons
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.SwapButtons])
                    {
                        _swapButtons = UnsafeNativeMethods.GetSystemMetrics(SM.SWAPBUTTON) != 0;
                        _cacheValid[(int)CacheSlot.SwapButtons] = true;
                    }
                }

                return _swapButtons;
            }
        }

        /// <summary>
        ///     Maps to SM_TABLETPC
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK -- Demands unmanaged code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static bool IsTabletPC
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsTabletPC])
                    {
                        _isTabletPC = UnsafeNativeMethods.GetSystemMetrics(SM.TABLETPC) != 0;
                        _cacheValid[(int)CacheSlot.IsTabletPC] = true;
                    }
                }

                return _isTabletPC;
            }
        }

        /// <summary>
        ///     Maps to SM_XVIRTUALSCREEN
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Demands unmanaged code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double VirtualScreenLeft
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.VirtualScreenLeft])
                    {
                        _virtualScreenLeft = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.XVIRTUALSCREEN));
                        _cacheValid[(int)CacheSlot.VirtualScreenLeft] = true;
                    }
                }

                return _virtualScreenLeft;
            }
        }

        /// <summary>
        ///     Maps to SM_YVIRTUALSCREEN
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///    PublicOK --Demands unmanaged code
        ///    Security Critical -- Calling UnsafeNativeMethods
        /// </SecurityNote>
        public static double VirtualScreenTop
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.VirtualScreenTop])
                    {
                        _virtualScreenTop = SystemParameters.ConvertPixel(UnsafeNativeMethods.GetSystemMetrics(SM.YVIRTUALSCREEN));
                        _cacheValid[(int)CacheSlot.VirtualScreenTop] = true;
                    }
                }

                return _virtualScreenTop;
            }
        }

        #endregion

        #region Metrics Keys

        /// <summary>
        ///     ThinHorizontalBorderHeight System Resource Key
        /// </summary>
        public static ResourceKey ThinHorizontalBorderHeightKey
        {
            get
            {
                if (_cacheThinHorizontalBorderHeight == null)
                {
                    _cacheThinHorizontalBorderHeight = CreateInstance(SystemResourceKeyID.ThinHorizontalBorderHeight);
                }

                return _cacheThinHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     ThinVerticalBorderWidth System Resource Key
        /// </summary>
        public static ResourceKey ThinVerticalBorderWidthKey
        {
            get
            {
                if (_cacheThinVerticalBorderWidth == null)
                {
                    _cacheThinVerticalBorderWidth = CreateInstance(SystemResourceKeyID.ThinVerticalBorderWidth);
                }

                return _cacheThinVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     CursorWidth System Resource Key
        /// </summary>
        public static ResourceKey CursorWidthKey
        {
            get
            {
                if (_cacheCursorWidth == null)
                {
                    _cacheCursorWidth = CreateInstance(SystemResourceKeyID.CursorWidth);
                }

                return _cacheCursorWidth;
            }
        }

        /// <summary>
        ///     CursorHeight System Resource Key
        /// </summary>
        public static ResourceKey CursorHeightKey
        {
            get
            {
                if (_cacheCursorHeight == null)
                {
                    _cacheCursorHeight = CreateInstance(SystemResourceKeyID.CursorHeight);
                }

                return _cacheCursorHeight;
            }
        }

        /// <summary>
        ///     ThickHorizontalBorderHeight System Resource Key
        /// </summary>
        public static ResourceKey ThickHorizontalBorderHeightKey
        {
            get
            {
                if (_cacheThickHorizontalBorderHeight == null)
                {
                    _cacheThickHorizontalBorderHeight = CreateInstance(SystemResourceKeyID.ThickHorizontalBorderHeight);
                }

                return _cacheThickHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     ThickVerticalBorderWidth System Resource Key
        /// </summary>
        public static ResourceKey ThickVerticalBorderWidthKey
        {
            get
            {
                if (_cacheThickVerticalBorderWidth == null)
                {
                    _cacheThickVerticalBorderWidth = CreateInstance(SystemResourceKeyID.ThickVerticalBorderWidth);
                }

                return _cacheThickVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     FixedFrameHorizontalBorderHeight System Resource Key
        /// </summary>
        public static ResourceKey FixedFrameHorizontalBorderHeightKey
        {
            get
            {
                if (_cacheFixedFrameHorizontalBorderHeight == null)
                {
                    _cacheFixedFrameHorizontalBorderHeight = CreateInstance(SystemResourceKeyID.FixedFrameHorizontalBorderHeight);
                }

                return _cacheFixedFrameHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     FixedFrameVerticalBorderWidth System Resource Key
        /// </summary>
        public static ResourceKey FixedFrameVerticalBorderWidthKey
        {
            get
            {
                if (_cacheFixedFrameVerticalBorderWidth == null)
                {
                    _cacheFixedFrameVerticalBorderWidth = CreateInstance(SystemResourceKeyID.FixedFrameVerticalBorderWidth);
                }

                return _cacheFixedFrameVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     FocusHorizontalBorderHeight System Resource Key
        /// </summary>
        public static ResourceKey FocusHorizontalBorderHeightKey
        {
            get
            {
                if (_cacheFocusHorizontalBorderHeight == null)
                {
                    _cacheFocusHorizontalBorderHeight = CreateInstance(SystemResourceKeyID.FocusHorizontalBorderHeight);
                }

                return _cacheFocusHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     FocusVerticalBorderWidth System Resource Key
        /// </summary>
        public static ResourceKey FocusVerticalBorderWidthKey
        {
            get
            {
                if (_cacheFocusVerticalBorderWidth == null)
                {
                    _cacheFocusVerticalBorderWidth = CreateInstance(SystemResourceKeyID.FocusVerticalBorderWidth);
                }

                return _cacheFocusVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     FullPrimaryScreenWidth System Resource Key
        /// </summary>
        public static ResourceKey FullPrimaryScreenWidthKey
        {
            get
            {
                if (_cacheFullPrimaryScreenWidth == null)
                {
                    _cacheFullPrimaryScreenWidth = CreateInstance(SystemResourceKeyID.FullPrimaryScreenWidth);
                }

                return _cacheFullPrimaryScreenWidth;
            }
        }

        /// <summary>
        ///     FullPrimaryScreenHeight System Resource Key
        /// </summary>
        public static ResourceKey FullPrimaryScreenHeightKey
        {
            get
            {
                if (_cacheFullPrimaryScreenHeight == null)
                {
                    _cacheFullPrimaryScreenHeight = CreateInstance(SystemResourceKeyID.FullPrimaryScreenHeight);
                }

                return _cacheFullPrimaryScreenHeight;
            }
        }

        /// <summary>
        ///     HorizontalScrollBarButtonWidth System Resource Key
        /// </summary>
        public static ResourceKey HorizontalScrollBarButtonWidthKey
        {
            get
            {
                if (_cacheHorizontalScrollBarButtonWidth == null)
                {
                    _cacheHorizontalScrollBarButtonWidth = CreateInstance(SystemResourceKeyID.HorizontalScrollBarButtonWidth);
                }

                return _cacheHorizontalScrollBarButtonWidth;
            }
        }

        /// <summary>
        ///     HorizontalScrollBarHeight System Resource Key
        /// </summary>
        public static ResourceKey HorizontalScrollBarHeightKey
        {
            get
            {
                if (_cacheHorizontalScrollBarHeight == null)
                {
                    _cacheHorizontalScrollBarHeight = CreateInstance(SystemResourceKeyID.HorizontalScrollBarHeight);
                }

                return _cacheHorizontalScrollBarHeight;
            }
        }

        /// <summary>
        ///     HorizontalScrollBarThumbWidth System Resource Key
        /// </summary>
        public static ResourceKey HorizontalScrollBarThumbWidthKey
        {
            get
            {
                if (_cacheHorizontalScrollBarThumbWidth == null)
                {
                    _cacheHorizontalScrollBarThumbWidth = CreateInstance(SystemResourceKeyID.HorizontalScrollBarThumbWidth);
                }

                return _cacheHorizontalScrollBarThumbWidth;
            }
        }

        /// <summary>
        ///     IconWidth System Resource Key
        /// </summary>
        public static ResourceKey IconWidthKey
        {
            get
            {
                if (_cacheIconWidth == null)
                {
                    _cacheIconWidth = CreateInstance(SystemResourceKeyID.IconWidth);
                }

                return _cacheIconWidth;
            }
        }

        /// <summary>
        ///     IconHeight System Resource Key
        /// </summary>
        public static ResourceKey IconHeightKey
        {
            get
            {
                if (_cacheIconHeight == null)
                {
                    _cacheIconHeight = CreateInstance(SystemResourceKeyID.IconHeight);
                }

                return _cacheIconHeight;
            }
        }

        /// <summary>
        ///     IconGridWidth System Resource Key
        /// </summary>
        public static ResourceKey IconGridWidthKey
        {
            get
            {
                if (_cacheIconGridWidth == null)
                {
                    _cacheIconGridWidth = CreateInstance(SystemResourceKeyID.IconGridWidth);
                }

                return _cacheIconGridWidth;
            }
        }

        /// <summary>
        ///     IconGridHeight System Resource Key
        /// </summary>
        public static ResourceKey IconGridHeightKey
        {
            get
            {
                if (_cacheIconGridHeight == null)
                {
                    _cacheIconGridHeight = CreateInstance(SystemResourceKeyID.IconGridHeight);
                }

                return _cacheIconGridHeight;
            }
        }

        /// <summary>
        ///     MaximizedPrimaryScreenWidth System Resource Key
        /// </summary>
        public static ResourceKey MaximizedPrimaryScreenWidthKey
        {
            get
            {
                if (_cacheMaximizedPrimaryScreenWidth == null)
                {
                    _cacheMaximizedPrimaryScreenWidth = CreateInstance(SystemResourceKeyID.MaximizedPrimaryScreenWidth);
                }

                return _cacheMaximizedPrimaryScreenWidth;
            }
        }

        /// <summary>
        ///     MaximizedPrimaryScreenHeight System Resource Key
        /// </summary>
        public static ResourceKey MaximizedPrimaryScreenHeightKey
        {
            get
            {
                if (_cacheMaximizedPrimaryScreenHeight == null)
                {
                    _cacheMaximizedPrimaryScreenHeight = CreateInstance(SystemResourceKeyID.MaximizedPrimaryScreenHeight);
                }

                return _cacheMaximizedPrimaryScreenHeight;
            }
        }

        /// <summary>
        ///     MaximumWindowTrackWidth System Resource Key
        /// </summary>
        public static ResourceKey MaximumWindowTrackWidthKey
        {
            get
            {
                if (_cacheMaximumWindowTrackWidth == null)
                {
                    _cacheMaximumWindowTrackWidth = CreateInstance(SystemResourceKeyID.MaximumWindowTrackWidth);
                }

                return _cacheMaximumWindowTrackWidth;
            }
        }

        /// <summary>
        ///     MaximumWindowTrackHeight System Resource Key
        /// </summary>
        public static ResourceKey MaximumWindowTrackHeightKey
        {
            get
            {
                if (_cacheMaximumWindowTrackHeight == null)
                {
                    _cacheMaximumWindowTrackHeight = CreateInstance(SystemResourceKeyID.MaximumWindowTrackHeight);
                }

                return _cacheMaximumWindowTrackHeight;
            }
        }

        /// <summary>
        ///     MenuCheckmarkWidth System Resource Key
        /// </summary>
        public static ResourceKey MenuCheckmarkWidthKey
        {
            get
            {
                if (_cacheMenuCheckmarkWidth == null)
                {
                    _cacheMenuCheckmarkWidth = CreateInstance(SystemResourceKeyID.MenuCheckmarkWidth);
                }

                return _cacheMenuCheckmarkWidth;
            }
        }

        /// <summary>
        ///     MenuCheckmarkHeight System Resource Key
        /// </summary>
        public static ResourceKey MenuCheckmarkHeightKey
        {
            get
            {
                if (_cacheMenuCheckmarkHeight == null)
                {
                    _cacheMenuCheckmarkHeight = CreateInstance(SystemResourceKeyID.MenuCheckmarkHeight);
                }

                return _cacheMenuCheckmarkHeight;
            }
        }

        /// <summary>
        ///     MenuButtonWidth System Resource Key
        /// </summary>
        public static ResourceKey MenuButtonWidthKey
        {
            get
            {
                if (_cacheMenuButtonWidth == null)
                {
                    _cacheMenuButtonWidth = CreateInstance(SystemResourceKeyID.MenuButtonWidth);
                }

                return _cacheMenuButtonWidth;
            }
        }

        /// <summary>
        ///     MenuButtonHeight System Resource Key
        /// </summary>
        public static ResourceKey MenuButtonHeightKey
        {
            get
            {
                if (_cacheMenuButtonHeight == null)
                {
                    _cacheMenuButtonHeight = CreateInstance(SystemResourceKeyID.MenuButtonHeight);
                }

                return _cacheMenuButtonHeight;
            }
        }

        /// <summary>
        ///     MinimumWindowWidth System Resource Key
        /// </summary>
        public static ResourceKey MinimumWindowWidthKey
        {
            get
            {
                if (_cacheMinimumWindowWidth == null)
                {
                    _cacheMinimumWindowWidth = CreateInstance(SystemResourceKeyID.MinimumWindowWidth);
                }

                return _cacheMinimumWindowWidth;
            }
        }

        /// <summary>
        ///     MinimumWindowHeight System Resource Key
        /// </summary>
        public static ResourceKey MinimumWindowHeightKey
        {
            get
            {
                if (_cacheMinimumWindowHeight == null)
                {
                    _cacheMinimumWindowHeight = CreateInstance(SystemResourceKeyID.MinimumWindowHeight);
                }

                return _cacheMinimumWindowHeight;
            }
        }

        /// <summary>
        ///     MinimizedWindowWidth System Resource Key
        /// </summary>
        public static ResourceKey MinimizedWindowWidthKey
        {
            get
            {
                if (_cacheMinimizedWindowWidth == null)
                {
                    _cacheMinimizedWindowWidth = CreateInstance(SystemResourceKeyID.MinimizedWindowWidth);
                }

                return _cacheMinimizedWindowWidth;
            }
        }

        /// <summary>
        ///     MinimizedWindowHeight System Resource Key
        /// </summary>
        public static ResourceKey MinimizedWindowHeightKey
        {
            get
            {
                if (_cacheMinimizedWindowHeight == null)
                {
                    _cacheMinimizedWindowHeight = CreateInstance(SystemResourceKeyID.MinimizedWindowHeight);
                }

                return _cacheMinimizedWindowHeight;
            }
        }

        /// <summary>
        ///     MinimizedGridWidth System Resource Key
        /// </summary>
        public static ResourceKey MinimizedGridWidthKey
        {
            get
            {
                if (_cacheMinimizedGridWidth == null)
                {
                    _cacheMinimizedGridWidth = CreateInstance(SystemResourceKeyID.MinimizedGridWidth);
                }

                return _cacheMinimizedGridWidth;
            }
        }

        /// <summary>
        ///     MinimizedGridHeight System Resource Key
        /// </summary>
        public static ResourceKey MinimizedGridHeightKey
        {
            get
            {
                if (_cacheMinimizedGridHeight == null)
                {
                    _cacheMinimizedGridHeight = CreateInstance(SystemResourceKeyID.MinimizedGridHeight);
                }

                return _cacheMinimizedGridHeight;
            }
        }

        /// <summary>
        ///     MinimumWindowTrackWidth System Resource Key
        /// </summary>
        public static ResourceKey MinimumWindowTrackWidthKey
        {
            get
            {
                if (_cacheMinimumWindowTrackWidth == null)
                {
                    _cacheMinimumWindowTrackWidth = CreateInstance(SystemResourceKeyID.MinimumWindowTrackWidth);
                }

                return _cacheMinimumWindowTrackWidth;
            }
        }

        /// <summary>
        ///     MinimumWindowTrackHeight System Resource Key
        /// </summary>
        public static ResourceKey MinimumWindowTrackHeightKey
        {
            get
            {
                if (_cacheMinimumWindowTrackHeight == null)
                {
                    _cacheMinimumWindowTrackHeight = CreateInstance(SystemResourceKeyID.MinimumWindowTrackHeight);
                }

                return _cacheMinimumWindowTrackHeight;
            }
        }

        /// <summary>
        ///     PrimaryScreenWidth System Resource Key
        /// </summary>
        public static ResourceKey PrimaryScreenWidthKey
        {
            get
            {
                if (_cachePrimaryScreenWidth == null)
                {
                    _cachePrimaryScreenWidth = CreateInstance(SystemResourceKeyID.PrimaryScreenWidth);
                }

                return _cachePrimaryScreenWidth;
            }
        }

        /// <summary>
        ///     PrimaryScreenHeight System Resource Key
        /// </summary>
        public static ResourceKey PrimaryScreenHeightKey
        {
            get
            {
                if (_cachePrimaryScreenHeight == null)
                {
                    _cachePrimaryScreenHeight = CreateInstance(SystemResourceKeyID.PrimaryScreenHeight);
                }

                return _cachePrimaryScreenHeight;
            }
        }

        /// <summary>
        ///     WindowCaptionButtonWidth System Resource Key
        /// </summary>
        public static ResourceKey WindowCaptionButtonWidthKey
        {
            get
            {
                if (_cacheWindowCaptionButtonWidth == null)
                {
                    _cacheWindowCaptionButtonWidth = CreateInstance(SystemResourceKeyID.WindowCaptionButtonWidth);
                }

                return _cacheWindowCaptionButtonWidth;
            }
        }

        /// <summary>
        ///     WindowCaptionButtonHeight System Resource Key
        /// </summary>
        public static ResourceKey WindowCaptionButtonHeightKey
        {
            get
            {
                if (_cacheWindowCaptionButtonHeight == null)
                {
                    _cacheWindowCaptionButtonHeight = CreateInstance(SystemResourceKeyID.WindowCaptionButtonHeight);
                }

                return _cacheWindowCaptionButtonHeight;
            }
        }

        /// <summary>
        ///     ResizeFrameHorizontalBorderHeight System Resource Key
        /// </summary>
        public static ResourceKey ResizeFrameHorizontalBorderHeightKey
        {
            get
            {
                if (_cacheResizeFrameHorizontalBorderHeight == null)
                {
                    _cacheResizeFrameHorizontalBorderHeight = CreateInstance(SystemResourceKeyID.ResizeFrameHorizontalBorderHeight);
                }

                return _cacheResizeFrameHorizontalBorderHeight;
            }
        }

        /// <summary>
        ///     ResizeFrameVerticalBorderWidth System Resource Key
        /// </summary>
        public static ResourceKey ResizeFrameVerticalBorderWidthKey
        {
            get
            {
                if (_cacheResizeFrameVerticalBorderWidth == null)
                {
                    _cacheResizeFrameVerticalBorderWidth = CreateInstance(SystemResourceKeyID.ResizeFrameVerticalBorderWidth);
                }

                return _cacheResizeFrameVerticalBorderWidth;
            }
        }

        /// <summary>
        ///     SmallIconWidth System Resource Key
        /// </summary>
        public static ResourceKey SmallIconWidthKey
        {
            get
            {
                if (_cacheSmallIconWidth == null)
                {
                    _cacheSmallIconWidth = CreateInstance(SystemResourceKeyID.SmallIconWidth);
                }

                return _cacheSmallIconWidth;
            }
        }

        /// <summary>
        ///     SmallIconHeight System Resource Key
        /// </summary>
        public static ResourceKey SmallIconHeightKey
        {
            get
            {
                if (_cacheSmallIconHeight == null)
                {
                    _cacheSmallIconHeight = CreateInstance(SystemResourceKeyID.SmallIconHeight);
                }

                return _cacheSmallIconHeight;
            }
        }

        /// <summary>
        ///     SmallWindowCaptionButtonWidth System Resource Key
        /// </summary>
        public static ResourceKey SmallWindowCaptionButtonWidthKey
        {
            get
            {
                if (_cacheSmallWindowCaptionButtonWidth == null)
                {
                    _cacheSmallWindowCaptionButtonWidth = CreateInstance(SystemResourceKeyID.SmallWindowCaptionButtonWidth);
                }

                return _cacheSmallWindowCaptionButtonWidth;
            }
        }

        /// <summary>
        ///     SmallWindowCaptionButtonHeight System Resource Key
        /// </summary>
        public static ResourceKey SmallWindowCaptionButtonHeightKey
        {
            get
            {
                if (_cacheSmallWindowCaptionButtonHeight == null)
                {
                    _cacheSmallWindowCaptionButtonHeight = CreateInstance(SystemResourceKeyID.SmallWindowCaptionButtonHeight);
                }

                return _cacheSmallWindowCaptionButtonHeight;
            }
        }

        /// <summary>
        ///     VirtualScreenWidth System Resource Key
        /// </summary>
        public static ResourceKey VirtualScreenWidthKey
        {
            get
            {
                if (_cacheVirtualScreenWidth == null)
                {
                    _cacheVirtualScreenWidth = CreateInstance(SystemResourceKeyID.VirtualScreenWidth);
                }

                return _cacheVirtualScreenWidth;
            }
        }

        /// <summary>
        ///     VirtualScreenHeight System Resource Key
        /// </summary>
        public static ResourceKey VirtualScreenHeightKey
        {
            get
            {
                if (_cacheVirtualScreenHeight == null)
                {
                    _cacheVirtualScreenHeight = CreateInstance(SystemResourceKeyID.VirtualScreenHeight);
                }

                return _cacheVirtualScreenHeight;
            }
        }

        /// <summary>
        ///     VerticalScrollBarWidth System Resource Key
        /// </summary>
        public static ResourceKey VerticalScrollBarWidthKey
        {
            get
            {
                if (_cacheVerticalScrollBarWidth == null)
                {
                    _cacheVerticalScrollBarWidth = CreateInstance(SystemResourceKeyID.VerticalScrollBarWidth);
                }

                return _cacheVerticalScrollBarWidth;
            }
        }

        /// <summary>
        ///     VerticalScrollBarButtonHeight System Resource Key
        /// </summary>
        public static ResourceKey VerticalScrollBarButtonHeightKey
        {
            get
            {
                if (_cacheVerticalScrollBarButtonHeight == null)
                {
                    _cacheVerticalScrollBarButtonHeight = CreateInstance(SystemResourceKeyID.VerticalScrollBarButtonHeight);
                }

                return _cacheVerticalScrollBarButtonHeight;
            }
        }

        /// <summary>
        ///     WindowCaptionHeight System Resource Key
        /// </summary>
        public static ResourceKey WindowCaptionHeightKey
        {
            get
            {
                if (_cacheWindowCaptionHeight == null)
                {
                    _cacheWindowCaptionHeight = CreateInstance(SystemResourceKeyID.WindowCaptionHeight);
                }

                return _cacheWindowCaptionHeight;
            }
        }

        /// <summary>
        ///     KanjiWindowHeight System Resource Key
        /// </summary>
        public static ResourceKey KanjiWindowHeightKey
        {
            get
            {
                if (_cacheKanjiWindowHeight == null)
                {
                    _cacheKanjiWindowHeight = CreateInstance(SystemResourceKeyID.KanjiWindowHeight);
                }

                return _cacheKanjiWindowHeight;
            }
        }

        /// <summary>
        ///     MenuBarHeight System Resource Key
        /// </summary>
        public static ResourceKey MenuBarHeightKey
        {
            get
            {
                if (_cacheMenuBarHeight == null)
                {
                    _cacheMenuBarHeight = CreateInstance(SystemResourceKeyID.MenuBarHeight);
                }

                return _cacheMenuBarHeight;
            }
        }

        /// <summary>
        ///     SmallCaptionHeight System Resource Key
        /// </summary>
        public static ResourceKey SmallCaptionHeightKey
        {
            get
            {
                if (_cacheSmallCaptionHeight == null)
                {
                    _cacheSmallCaptionHeight = CreateInstance(SystemResourceKeyID.SmallCaptionHeight);
                }

                return _cacheSmallCaptionHeight;
            }
        }

        /// <summary>
        ///     VerticalScrollBarThumbHeight System Resource Key
        /// </summary>
        public static ResourceKey VerticalScrollBarThumbHeightKey
        {
            get
            {
                if (_cacheVerticalScrollBarThumbHeight == null)
                {
                    _cacheVerticalScrollBarThumbHeight = CreateInstance(SystemResourceKeyID.VerticalScrollBarThumbHeight);
                }

                return _cacheVerticalScrollBarThumbHeight;
            }
        }

        /// <summary>
        ///     IsImmEnabled System Resource Key
        /// </summary>
        public static ResourceKey IsImmEnabledKey
        {
            get
            {
                if (_cacheIsImmEnabled == null)
                {
                    _cacheIsImmEnabled = CreateInstance(SystemResourceKeyID.IsImmEnabled);
                }

                return _cacheIsImmEnabled;
            }
        }

        /// <summary>
        ///     IsMediaCenter System Resource Key
        /// </summary>
        public static ResourceKey IsMediaCenterKey
        {
            get
            {
                if (_cacheIsMediaCenter == null)
                {
                    _cacheIsMediaCenter = CreateInstance(SystemResourceKeyID.IsMediaCenter);
                }

                return _cacheIsMediaCenter;
            }
        }

        /// <summary>
        ///     IsMenuDropRightAligned System Resource Key
        /// </summary>
        public static ResourceKey IsMenuDropRightAlignedKey
        {
            get
            {
                if (_cacheIsMenuDropRightAligned == null)
                {
                    _cacheIsMenuDropRightAligned = CreateInstance(SystemResourceKeyID.IsMenuDropRightAligned);
                }

                return _cacheIsMenuDropRightAligned;
            }
        }

        /// <summary>
        ///     IsMiddleEastEnabled System Resource Key
        /// </summary>
        public static ResourceKey IsMiddleEastEnabledKey
        {
            get
            {
                if (_cacheIsMiddleEastEnabled == null)
                {
                    _cacheIsMiddleEastEnabled = CreateInstance(SystemResourceKeyID.IsMiddleEastEnabled);
                }

                return _cacheIsMiddleEastEnabled;
            }
        }

        /// <summary>
        ///     IsMousePresent System Resource Key
        /// </summary>
        public static ResourceKey IsMousePresentKey
        {
            get
            {
                if (_cacheIsMousePresent == null)
                {
                    _cacheIsMousePresent = CreateInstance(SystemResourceKeyID.IsMousePresent);
                }

                return _cacheIsMousePresent;
            }
        }

        /// <summary>
        ///     IsMouseWheelPresent System Resource Key
        /// </summary>
        public static ResourceKey IsMouseWheelPresentKey
        {
            get
            {
                if (_cacheIsMouseWheelPresent == null)
                {
                    _cacheIsMouseWheelPresent = CreateInstance(SystemResourceKeyID.IsMouseWheelPresent);
                }

                return _cacheIsMouseWheelPresent;
            }
        }

        /// <summary>
        ///     IsPenWindows System Resource Key
        /// </summary>
        public static ResourceKey IsPenWindowsKey
        {
            get
            {
                if (_cacheIsPenWindows == null)
                {
                    _cacheIsPenWindows = CreateInstance(SystemResourceKeyID.IsPenWindows);
                }

                return _cacheIsPenWindows;
            }
        }

        /// <summary>
        ///     IsRemotelyControlled System Resource Key
        /// </summary>
        public static ResourceKey IsRemotelyControlledKey
        {
            get
            {
                if (_cacheIsRemotelyControlled == null)
                {
                    _cacheIsRemotelyControlled = CreateInstance(SystemResourceKeyID.IsRemotelyControlled);
                }

                return _cacheIsRemotelyControlled;
            }
        }

        /// <summary>
        ///     IsRemoteSession System Resource Key
        /// </summary>
        public static ResourceKey IsRemoteSessionKey
        {
            get
            {
                if (_cacheIsRemoteSession == null)
                {
                    _cacheIsRemoteSession = CreateInstance(SystemResourceKeyID.IsRemoteSession);
                }

                return _cacheIsRemoteSession;
            }
        }

        /// <summary>
        ///     ShowSounds System Resource Key
        /// </summary>
        public static ResourceKey ShowSoundsKey
        {
            get
            {
                if (_cacheShowSounds == null)
                {
                    _cacheShowSounds = CreateInstance(SystemResourceKeyID.ShowSounds);
                }

                return _cacheShowSounds;
            }
        }

        /// <summary>
        ///     IsSlowMachine System Resource Key
        /// </summary>
        public static ResourceKey IsSlowMachineKey
        {
            get
            {
                if (_cacheIsSlowMachine == null)
                {
                    _cacheIsSlowMachine = CreateInstance(SystemResourceKeyID.IsSlowMachine);
                }

                return _cacheIsSlowMachine;
            }
        }

        /// <summary>
        ///     SwapButtons System Resource Key
        /// </summary>
        public static ResourceKey SwapButtonsKey
        {
            get
            {
                if (_cacheSwapButtons == null)
                {
                    _cacheSwapButtons = CreateInstance(SystemResourceKeyID.SwapButtons);
                }

                return _cacheSwapButtons;
            }
        }

        /// <summary>
        ///     IsTabletPC System Resource Key
        /// </summary>
        public static ResourceKey IsTabletPCKey
        {
            get
            {
                if (_cacheIsTabletPC == null)
                {
                    _cacheIsTabletPC = CreateInstance(SystemResourceKeyID.IsTabletPC);
                }

                return _cacheIsTabletPC;
            }
        }

        /// <summary>
        ///     VirtualScreenLeft System Resource Key
        /// </summary>
        public static ResourceKey VirtualScreenLeftKey
        {
            get
            {
                if (_cacheVirtualScreenLeft == null)
                {
                    _cacheVirtualScreenLeft = CreateInstance(SystemResourceKeyID.VirtualScreenLeft);
                }

                return _cacheVirtualScreenLeft;
            }
        }

        /// <summary>
        ///     VirtualScreenTop System Resource Key
        /// </summary>
        public static ResourceKey VirtualScreenTopKey
        {
            get
            {
                if (_cacheVirtualScreenTop == null)
                {
                    _cacheVirtualScreenTop = CreateInstance(SystemResourceKeyID.VirtualScreenTop);
                }

                return _cacheVirtualScreenTop;
            }
        }

        #endregion

        #region Theme Style Keys

        /// <summary>
        ///     Resource Key for the FocusVisualStyle
        /// </summary>
        public static ResourceKey FocusVisualStyleKey
        {
            get
            {
                if (_cacheFocusVisualStyle == null)
                {
                    _cacheFocusVisualStyle = new SystemThemeKey(SystemResourceKeyID.FocusVisualStyle);
                }

                return _cacheFocusVisualStyle;
            }
        }

        /// <summary>
        /// Resource Key for the browser window style
        /// </summary>
        /// <value></value>
        public static ResourceKey NavigationChromeStyleKey
        {
            get
            {
                if (_cacheNavigationChromeStyle == null)
                {
                    _cacheNavigationChromeStyle = new SystemThemeKey(SystemResourceKeyID.NavigationChromeStyle);
                }

                return _cacheNavigationChromeStyle;
            }
        }

        /// <summary>
        /// Resource Key for the down level browser window style
        /// </summary>
        /// <value></value>
        public static ResourceKey NavigationChromeDownLevelStyleKey
        {
            get
            {
                if (_cacheNavigationChromeDownLevelStyle == null)
                {
                    _cacheNavigationChromeDownLevelStyle = new SystemThemeKey(SystemResourceKeyID.NavigationChromeDownLevelStyle);
                }

                return _cacheNavigationChromeDownLevelStyle;
            }
        }

        #endregion


        #region Power Parameters

        /// <summary>
        ///     Indicates current Power Status
        /// </summary>
        ///<SecurityNote>
        /// Critical as this code elevates.
        /// PublicOK - as we think this is ok to expose.
        ///</SecurityNote>
        public static PowerLineStatus PowerLineStatus
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.PowerLineStatus])
                    {
                        NativeMethods.SYSTEM_POWER_STATUS status = new NativeMethods.SYSTEM_POWER_STATUS();
                        if (UnsafeNativeMethods.GetSystemPowerStatus(ref status))
                        {
                            _powerLineStatus = (PowerLineStatus)status.ACLineStatus;
                            _cacheValid[(int)CacheSlot.PowerLineStatus] = true;
                        }
                        else
                        {
                            throw new Win32Exception();
                        }
                    }
                }

                return _powerLineStatus;
            }
        }
        #endregion

        #region PowerKeys
        /// <summary>
        /// Resource Key for the PowerLineStatus property
        /// </summary>
        /// <value></value>
        public static ResourceKey PowerLineStatusKey
        {
            get
            {
                if (_cachePowerLineStatus == null)
                {
                    _cachePowerLineStatus = CreateInstance(SystemResourceKeyID.PowerLineStatus);
                }

                return _cachePowerLineStatus;
            }
        }
        #endregion


#pragma warning restore 6523

#pragma warning restore 6503

        #region Cache and Implementation

        internal static void InvalidateCache()
        {
            // Invalidate all Parameters
            int[] param = {  NativeMethods.SPI_SETFOCUSBORDERWIDTH,
                             NativeMethods.SPI_SETFOCUSBORDERHEIGHT,
                             NativeMethods.SPI_SETHIGHCONTRAST,
                             NativeMethods.SPI_SETMOUSEVANISH,
                             NativeMethods.SPI_SETDROPSHADOW,
                             NativeMethods.SPI_SETFLATMENU,
                             NativeMethods.SPI_SETWORKAREA,
                             NativeMethods.SPI_SETICONMETRICS,
                             NativeMethods.SPI_SETKEYBOARDCUES,
                             NativeMethods.SPI_SETKEYBOARDDELAY,
                             NativeMethods.SPI_SETKEYBOARDPREF,
                             NativeMethods.SPI_SETKEYBOARDSPEED,
                             NativeMethods.SPI_SETSNAPTODEFBUTTON,
                             NativeMethods.SPI_SETWHEELSCROLLLINES,
                             NativeMethods.SPI_SETMOUSEHOVERTIME,
                             NativeMethods.SPI_SETMOUSEHOVERHEIGHT,
                             NativeMethods.SPI_SETMOUSEHOVERWIDTH,
                             NativeMethods.SPI_SETMENUDROPALIGNMENT,
                             NativeMethods.SPI_SETMENUFADE,
                             NativeMethods.SPI_SETMENUSHOWDELAY,
                             NativeMethods.SPI_SETCOMBOBOXANIMATION,
                             NativeMethods.SPI_SETCLIENTAREAANIMATION,
                             NativeMethods.SPI_SETCURSORSHADOW,
                             NativeMethods.SPI_SETGRADIENTCAPTIONS,
                             NativeMethods.SPI_SETHOTTRACKING,
                             NativeMethods.SPI_SETLISTBOXSMOOTHSCROLLING,
                             NativeMethods.SPI_SETMENUANIMATION,
                             NativeMethods.SPI_SETSELECTIONFADE,
                             NativeMethods.SPI_SETSTYLUSHOTTRACKING,
                             NativeMethods.SPI_SETTOOLTIPANIMATION,
                             NativeMethods.SPI_SETTOOLTIPFADE,
                             NativeMethods.SPI_SETUIEFFECTS,
                             NativeMethods.SPI_SETANIMATION,
                             NativeMethods.SPI_SETCARETWIDTH,
                             NativeMethods.SPI_SETFOREGROUNDFLASHCOUNT,
                             NativeMethods.SPI_SETDRAGFULLWINDOWS,
                             NativeMethods.SPI_SETBORDER,
                             NativeMethods.SPI_SETNONCLIENTMETRICS,
                             NativeMethods.SPI_SETDRAGWIDTH,
                             NativeMethods.SPI_SETDRAGHEIGHT,
                             NativeMethods.SPI_SETPENWINDOWS,
                             NativeMethods.SPI_SETSHOWSOUNDS,
                             NativeMethods.SPI_SETMOUSEBUTTONSWAP};

            for (int i = 0; i < param.Length; i++)
            {
                InvalidateCache(param[i]);
            }
        }

        internal static bool InvalidateDeviceDependentCache()
        {
            bool changed = false;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IsMousePresent))
                changed |= _isMousePresent != IsMousePresent;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IsMouseWheelPresent))
                changed |= _isMouseWheelPresent != IsMouseWheelPresent;

            if (changed)
                OnPropertiesChanged("IsMousePresent", "IsMouseWheelPresent");

            return changed;
        }

        internal static bool InvalidateDisplayDependentCache()
        {
            bool changed = false;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.WorkAreaInternal))
            {
                NativeMethods.RECT oldRect = _workAreaInternal;
                NativeMethods.RECT newRect = WorkAreaInternal;
                changed |= oldRect.left != newRect.left;
                changed |= oldRect.top != newRect.top;
                changed |= oldRect.right != newRect.right;
                changed |= oldRect.bottom != newRect.bottom;
            }

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.WorkArea))
                changed |= _workArea != WorkArea;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.FullPrimaryScreenWidth))
                changed |= _fullPrimaryScreenWidth != FullPrimaryScreenWidth;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.FullPrimaryScreenHeight))
                changed |= _fullPrimaryScreenHeight != FullPrimaryScreenHeight;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MaximizedPrimaryScreenWidth))
                changed |= _maximizedPrimaryScreenWidth != MaximizedPrimaryScreenWidth;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MaximizedPrimaryScreenHeight))
                changed |= _maximizedPrimaryScreenHeight != MaximizedPrimaryScreenHeight;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.PrimaryScreenWidth))
                changed |= _primaryScreenWidth != PrimaryScreenWidth;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.PrimaryScreenHeight))
                changed |= _primaryScreenHeight != PrimaryScreenHeight;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.VirtualScreenWidth))
                changed |= _virtualScreenWidth != VirtualScreenWidth;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.VirtualScreenHeight))
                changed |= _virtualScreenHeight != VirtualScreenHeight;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.VirtualScreenLeft))
                changed |= _virtualScreenLeft != VirtualScreenLeft;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.VirtualScreenTop))
                changed |= _virtualScreenTop != VirtualScreenTop;


            if (changed)
                OnPropertiesChanged("WorkArea",
                                "FullPrimaryScreenWidth", "FullPrimaryScreenHeight",
                                "MaximizedPrimaryScreenWidth", "MaximizedPrimaryScreenHeight",
                                "PrimaryScreenWidth", "PrimaryScreenHeight",
                                "VirtualScreenWidth", "VirtualScreenHeight",
                                "VirtualScreenLeft", "VirtualScreenTop");

            return changed;
        }

        internal static bool InvalidatePowerDependentCache()
        {
            bool changed = false;

            if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.PowerLineStatus))
                changed |= _powerLineStatus != PowerLineStatus;

            if (changed)
                OnPropertiesChanged("PowerLineStatus");

            return changed;
        }

        internal static bool InvalidateCache(int param)
        {
            // FxCop: Hashtable of callbacks would not be performant, using switch instead

            switch (param)
            {
                case NativeMethods.SPI_SETFOCUSBORDERWIDTH:
                    {
                        bool changed = SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.FocusBorderWidth);

                        // Invalidate SystemMetrics
                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.FocusHorizontalBorderHeight))
                            changed |= _focusHorizontalBorderHeight != FocusHorizontalBorderHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.FocusVerticalBorderWidth))
                            changed |= _focusVerticalBorderWidth != FocusVerticalBorderWidth;

                        if (changed)
                            OnPropertiesChanged("FocusBorderWidth", "FocusHorizontalBorderHeight", "FocusVerticalBorderWidth");

                        return changed;
                    }
                case NativeMethods.SPI_SETFOCUSBORDERHEIGHT:
                    return InvalidateProperty((int)CacheSlot.FocusBorderHeight, "FocusBorderHeight");
                case NativeMethods.SPI_SETHIGHCONTRAST:
                    return InvalidateProperty((int)CacheSlot.HighContrast, "HighContrast");
                case NativeMethods.SPI_SETMOUSEVANISH:
                    return InvalidateProperty((int)CacheSlot.MouseVanish, "MouseVanish");

                case NativeMethods.SPI_SETDROPSHADOW:
                    return InvalidateProperty((int)CacheSlot.DropShadow, "DropShadow");
                case NativeMethods.SPI_SETFLATMENU:
                    return InvalidateProperty((int)CacheSlot.FlatMenu, "FlatMenu");
                case NativeMethods.SPI_SETWORKAREA:
                    return InvalidateDisplayDependentCache();

                case NativeMethods.SPI_SETICONMETRICS:
                    {
                        bool changed = SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconMetrics);
                        if (changed)
                        {
                            SystemFonts.InvalidateIconMetrics();
                        }

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconWidth))
                            changed |= _iconWidth != IconWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconHeight))
                            changed |= _iconHeight != IconHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconGridWidth))
                            changed |= _iconGridWidth != IconGridWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconGridHeight))
                            changed |= _iconGridHeight != IconGridHeight;

                        if (changed)
                            OnPropertiesChanged("IconMetrics",
                                                "IconWidth", "IconHeight",
                                                "IconGridWidth", "IconGridHeight");

                        return changed;
                    }

                case NativeMethods.SPI_SETKEYBOARDCUES:
                    return InvalidateProperty((int)CacheSlot.KeyboardCues, "KeyboardCues");
                case NativeMethods.SPI_SETKEYBOARDDELAY:
                    return InvalidateProperty((int)CacheSlot.KeyboardDelay, "KeyboardDelay");
                case NativeMethods.SPI_SETKEYBOARDPREF:
                    return InvalidateProperty((int)CacheSlot.KeyboardPreference, "KeyboardPreference");
                case NativeMethods.SPI_SETKEYBOARDSPEED:
                    return InvalidateProperty((int)CacheSlot.KeyboardSpeed, "KeyboardSpeed");
                case NativeMethods.SPI_SETSNAPTODEFBUTTON:
                    return InvalidateProperty((int)CacheSlot.SnapToDefaultButton, "SnapToDefaultButton");
                case NativeMethods.SPI_SETWHEELSCROLLLINES:
                    return InvalidateProperty((int)CacheSlot.WheelScrollLines, "WheelScrollLines");
                case NativeMethods.SPI_SETMOUSEHOVERTIME:
                    return InvalidateProperty((int)CacheSlot.MouseHoverTime, "MouseHoverTime");
                case NativeMethods.SPI_SETMOUSEHOVERHEIGHT:
                    return InvalidateProperty((int)CacheSlot.MouseHoverHeight, "MouseHoverHeight");
                case NativeMethods.SPI_SETMOUSEHOVERWIDTH:
                    return InvalidateProperty((int)CacheSlot.MouseHoverWidth, "MouseHoverWidth");

                case NativeMethods.SPI_SETMENUDROPALIGNMENT:
                    {
                        bool changed = SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MenuDropAlignment);

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IsMenuDropRightAligned))
                            changed |= _isMenuDropRightAligned != IsMenuDropRightAligned;

                        if (changed)
                            OnPropertiesChanged("MenuDropAlignment", "IsMenuDropRightAligned");

                        return changed;
                    }
                case NativeMethods.SPI_SETMENUFADE:
                    return InvalidateProperty((int)CacheSlot.MenuFade, "MenuFade");
                case NativeMethods.SPI_SETMENUSHOWDELAY:
                    return InvalidateProperty((int)CacheSlot.MenuShowDelay, "MenuShowDelay");

                case NativeMethods.SPI_SETCOMBOBOXANIMATION:
                    return InvalidateProperty((int)CacheSlot.ComboBoxAnimation, "ComboBoxAnimation");
                case NativeMethods.SPI_SETCLIENTAREAANIMATION:
                    return InvalidateProperty((int)CacheSlot.ClientAreaAnimation, "ClientAreaAnimation");
                case NativeMethods.SPI_SETCURSORSHADOW:
                    return InvalidateProperty((int)CacheSlot.CursorShadow, "CursorShadow");
                case NativeMethods.SPI_SETGRADIENTCAPTIONS:
                    return InvalidateProperty((int)CacheSlot.GradientCaptions, "GradientCaptions");
                case NativeMethods.SPI_SETHOTTRACKING:
                    return InvalidateProperty((int)CacheSlot.HotTracking, "HotTracking");
                case NativeMethods.SPI_SETLISTBOXSMOOTHSCROLLING:
                    return InvalidateProperty((int)CacheSlot.ListBoxSmoothScrolling, "ListBoxSmoothScrolling");
                case NativeMethods.SPI_SETMENUANIMATION:
                    return InvalidateProperty((int)CacheSlot.MenuAnimation, "MenuAnimation");
                case NativeMethods.SPI_SETSELECTIONFADE:
                    return InvalidateProperty((int)CacheSlot.SelectionFade, "SelectionFade");
                case NativeMethods.SPI_SETSTYLUSHOTTRACKING:
                    return InvalidateProperty((int)CacheSlot.StylusHotTracking, "StylusHotTracking");
                case NativeMethods.SPI_SETTOOLTIPANIMATION:
                    return InvalidateProperty((int)CacheSlot.ToolTipAnimation, "ToolTipAnimation");
                case NativeMethods.SPI_SETTOOLTIPFADE:
                    return InvalidateProperty((int)CacheSlot.ToolTipFade, "ToolTipFade");
                case NativeMethods.SPI_SETUIEFFECTS:
                    return InvalidateProperty((int)CacheSlot.UIEffects, "UIEffects");

                case NativeMethods.SPI_SETANIMATION:
                    return InvalidateProperty((int)CacheSlot.MinimizeAnimation, "MinimizeAnimation");
                case NativeMethods.SPI_SETCARETWIDTH:
                    return InvalidateProperty((int)CacheSlot.CaretWidth, "CaretWidth");
                case NativeMethods.SPI_SETFOREGROUNDFLASHCOUNT:
                    return InvalidateProperty((int)CacheSlot.ForegroundFlashCount, "ForegroundFlashCount");
                case NativeMethods.SPI_SETDRAGFULLWINDOWS:
                    return InvalidateProperty((int)CacheSlot.DragFullWindows, "DragFullWindows");
                case NativeMethods.SPI_SETBORDER:
                case NativeMethods.SPI_SETNONCLIENTMETRICS:
                    {
                        bool changed = SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.NonClientMetrics);
                        if (changed)
                        {
                            SystemFonts.InvalidateNonClientMetrics();
                        }
                        changed |= SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.Border);

                        // Invalidate SystemMetrics
                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.ThinHorizontalBorderHeight))
                            changed |= _thinHorizontalBorderHeight != ThinHorizontalBorderHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.ThinVerticalBorderWidth))
                            changed |= _thinVerticalBorderWidth != ThinVerticalBorderWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.CursorWidth))
                            changed |= _cursorWidth != CursorWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.CursorHeight))
                            changed |= _cursorHeight != CursorHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.ThickHorizontalBorderHeight))
                            changed |= _thickHorizontalBorderHeight != ThickHorizontalBorderHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.ThickVerticalBorderWidth))
                            changed |= _thickVerticalBorderWidth != ThickVerticalBorderWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.FixedFrameHorizontalBorderHeight))
                            changed |= _fixedFrameHorizontalBorderHeight != FixedFrameHorizontalBorderHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.FixedFrameVerticalBorderWidth))
                            changed |= _fixedFrameVerticalBorderWidth != FixedFrameVerticalBorderWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.HorizontalScrollBarButtonWidth))
                            changed |= _horizontalScrollBarButtonWidth != HorizontalScrollBarButtonWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.HorizontalScrollBarHeight))
                            changed |= _horizontalScrollBarHeight != HorizontalScrollBarHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.HorizontalScrollBarThumbWidth))
                            changed |= _horizontalScrollBarThumbWidth != HorizontalScrollBarThumbWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconWidth))
                            changed |= _iconWidth != IconWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconHeight))
                            changed |= _iconHeight != IconHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconGridWidth))
                            changed |= _iconGridWidth != IconGridWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.IconGridHeight))
                            changed |= _iconGridHeight != IconGridHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MaximumWindowTrackWidth))
                            changed |= _maximumWindowTrackWidth != MaximumWindowTrackWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MaximumWindowTrackHeight))
                            changed |= _maximumWindowTrackHeight != MaximumWindowTrackHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MenuCheckmarkWidth))
                            changed |= _menuCheckmarkWidth != MenuCheckmarkWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MenuCheckmarkHeight))
                            changed |= _menuCheckmarkHeight != MenuCheckmarkHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MenuButtonWidth))
                            changed |= _menuButtonWidth != MenuButtonWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MenuButtonHeight))
                            changed |= _menuButtonHeight != MenuButtonHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimumWindowWidth))
                            changed |= _minimumWindowWidth != MinimumWindowWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimumWindowHeight))
                            changed |= _minimumWindowHeight != MinimumWindowHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimizedWindowWidth))
                            changed |= _minimizedWindowWidth != MinimizedWindowWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimizedWindowHeight))
                            changed |= _minimizedWindowHeight != MinimizedWindowHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimizedGridWidth))
                            changed |= _minimizedGridWidth != MinimizedGridWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimizedGridHeight))
                            changed |= _minimizedGridHeight != MinimizedGridHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimumWindowTrackWidth))
                            changed |= _minimumWindowTrackWidth != MinimumWindowTrackWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MinimumWindowTrackHeight))
                            changed |= _minimumWindowTrackHeight != MinimumWindowTrackHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.WindowCaptionButtonWidth))
                            changed |= _windowCaptionButtonWidth != WindowCaptionButtonWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.WindowCaptionButtonHeight))
                            changed |= _windowCaptionButtonHeight != WindowCaptionButtonHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.ResizeFrameHorizontalBorderHeight))
                            changed |= _resizeFrameHorizontalBorderHeight != ResizeFrameHorizontalBorderHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.ResizeFrameVerticalBorderWidth))
                            changed |= _resizeFrameVerticalBorderWidth != ResizeFrameVerticalBorderWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.SmallIconWidth))
                            changed |= _smallIconWidth != SmallIconWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.SmallIconHeight))
                            changed |= _smallIconHeight != SmallIconHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.SmallWindowCaptionButtonWidth))
                            changed |= _smallWindowCaptionButtonWidth != SmallWindowCaptionButtonWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.SmallWindowCaptionButtonHeight))
                            changed |= _smallWindowCaptionButtonHeight != SmallWindowCaptionButtonHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.VerticalScrollBarWidth))
                            changed |= _verticalScrollBarWidth != VerticalScrollBarWidth;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.VerticalScrollBarButtonHeight))
                            changed |= _verticalScrollBarButtonHeight != VerticalScrollBarButtonHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.WindowCaptionHeight))
                            changed |= _windowCaptionHeight != WindowCaptionHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.MenuBarHeight))
                            changed |= _menuBarHeight != MenuBarHeight;

                        if (SystemResources.ClearSlot(_cacheValid, (int)CacheSlot.VerticalScrollBarThumbHeight))
                            changed |= _verticalScrollBarThumbHeight != VerticalScrollBarThumbHeight;

                        if (changed)
                            OnPropertiesChanged("NonClientMetrics", "Border",
                                                "ThinHorizontalBorderHeight", "ThinVerticalBorderWidth",
                                                "CursorWidth", "CursorHeight",
                                                "ThickHorizontalBorderHeight", "ThickVerticalBorderWidth",
                                                "FixedFrameHorizontalBorderHeight", "FixedFrameVerticalBorderWidth",
                                                "HorizontalScrollBarButtonWidth", "HorizontalScrollBarHeight",
                                                "HorizontalScrollBarThumbWidth",
                                                "IconWidth", "IconHeight", "IconGridWidth", "IconGridHeight",
                                                "MaximumWindowTrackWidth", "MaximumWindowTrackHeight",
                                                "MenuCheckmarkWidth", "MenuCheckmarkHeight",
                                                "MenuButtonWidth", "MenuButtonHeight",
                                                "MinimumWindowWidth", "MinimumWindowHeight",
                                                "MinimizedWindowWidth", "MinimizedWindowHeight",
                                                "MinimizedGridWidth", "MinimizedGridHeight",
                                                "MinimumWindowTrackWidth", "MinimumWindowTrackHeight",
                                                "WindowCaptionButtonWidth", "WindowCaptionButtonHeight",
                                                "ResizeFrameHorizontalBorderHeight", "ResizeFrameVerticalBorderWidth",
                                                "SmallIconWidth", "SmallIconHeight",
                                                "SmallWindowCaptionButtonWidth", "SmallWindowCaptionButtonHeight",
                                                "VerticalScrollBarWidth", "VerticalScrollBarButtonHeight",
                                                "MenuBarHeight", "VerticalScrollBarThumbHeight");

                        changed |= InvalidateDisplayDependentCache();

                        return changed;
                    }

                case NativeMethods.SPI_SETDRAGWIDTH:
                    return InvalidateProperty((int)CacheSlot.MinimumHorizontalDragDistance, "MinimumHorizontalDragDistance");
                case NativeMethods.SPI_SETDRAGHEIGHT:
                    return InvalidateProperty((int)CacheSlot.MinimumVerticalDragDistance, "MinimumVerticalDragDistance");
                case NativeMethods.SPI_SETPENWINDOWS:
                    return InvalidateProperty((int)CacheSlot.IsPenWindows, "IsPenWindows");
                case NativeMethods.SPI_SETSHOWSOUNDS:
                    return InvalidateProperty((int)CacheSlot.ShowSounds, "ShowSounds");
                case NativeMethods.SPI_SETMOUSEBUTTONSWAP:
                    return InvalidateProperty((int)CacheSlot.SwapButtons, "SwapButtons");
            }

            return false;
        }

        internal static bool InvalidateIsGlassEnabled()
        {
            return InvalidateProperty((int)CacheSlot.IsGlassEnabled, "IsGlassEnabled");
        }

        // Several properties exposed here are not true system parameters but emerge
        // as logical properties when the system theme changes. 
        internal static void InvalidateDerivedThemeRelatedProperties()
        {
            InvalidateProperty((int)CacheSlot.UxThemeName, "UxThemeName");
            InvalidateProperty((int)CacheSlot.UxThemeColor, "UxThemeColor");
            InvalidateProperty((int)CacheSlot.WindowCornerRadius, "WindowCornerRadius");
        }

        internal static void InvalidateWindowGlassColorizationProperties()
        {
            InvalidateProperty((int)CacheSlot.WindowGlassColor, "WindowGlassColor");
            InvalidateProperty((int)CacheSlot.WindowGlassBrush, "WindowGlassBrush");
        }

        // We could possibly inspect wParam from the WM_SettingChange message and factor this logic into
        // InvalidateCache(wParam), but a separate method is easier for now.
        internal static void InvalidateWindowFrameThicknessProperties()
        {
            InvalidateProperty((int)CacheSlot.WindowNonClientFrameThickness, "WindowNonClientFrameThickness");
            InvalidateProperty((int)CacheSlot.WindowResizeBorderThickness, "WindowResizeBorderThickness");
        }

        /// <summary>
        ///     Whether DWM composition is turned on.
        ///     May change when WM.DWMNCRENDERINGCHANGED or WM.DWMCOMPOSITIONCHANGED is received.
        ///     
        ///     It turns out there may be some lag between someone asking this
        ///     and the window getting updated.  It's not too expensive, just always do the check
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        public static bool IsGlassEnabled
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.IsGlassEnabled])
                    {
                        _isGlassEnabled = Standard.NativeMethods.DwmIsCompositionEnabled();
                        _cacheValid[(int)CacheSlot.IsGlassEnabled] = true;
                    }
                }

                return _isGlassEnabled;
            }
        }

        /// <summary>
        ///     The current Windows system theme's name.
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ux")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ux")]
        public static string UxThemeName
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.UxThemeName])
                    {
                        if (!Standard.NativeMethods.IsThemeActive())
                        {
                            _uxThemeName = "Classic";
                        }
                        else
                        {
                            string name;
                            string color;
                            string size;

                            Standard.NativeMethods.GetCurrentThemeName(out name, out color, out size);
                            _uxThemeName = System.IO.Path.GetFileNameWithoutExtension(name);
                        }

                        _cacheValid[(int)CacheSlot.UxThemeName] = true;
                    }
                }

                return _uxThemeName;
            }
        }

        /// <summary>
        ///     The current Windows system theme's color.
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ux")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ux")]
        public static string UxThemeColor
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.UxThemeColor])
                    {
                        if (!Standard.NativeMethods.IsThemeActive())
                        {
                            _uxThemeColor = "";
                        }
                        else
                        {
                            string name;
                            string color;
                            string size;

                            Standard.NativeMethods.GetCurrentThemeName(out name, out color, out size);
                            _uxThemeColor = color;
                        }

                        _cacheValid[(int)CacheSlot.UxThemeColor] = true;
                    }
                }

                return _uxThemeColor;
            }
        }

        /// <summary>
        ///     The radius of window corners isn't exposed as a true system parameter.
        ///     It instead is a logical size that we're approximating based on the current theme.
        ///     There aren't any known variations based on theme color.
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        public static CornerRadius WindowCornerRadius
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowCornerRadius])
                    {
                        Standard.Assert.IsNeitherNullNorEmpty(UxThemeName);

                        // These radii are approximate.  The way WPF does rounding is different than how
                        //     rounded-rectangle HRGNs are created, which is also different than the actual
                        //     round corners on themed Windows.  For now we're not exposing anything to
                        //     mitigate the differences.
                        CornerRadius cornerRadius = default(CornerRadius);

                        // This list is known to be incomplete and very much not future-proof.
                        // On XP there are at least a couple of shipped themes that this won't catch,
                        // "Zune" and "Royale", but WPF doesn't know about these either.
                        // If a new theme was to replace Aero, then this will fall back on "classic" behaviors.
                        // This isn't ideal, but it's not the end of the world.  WPF will generally have problems anyways.
                        switch (UxThemeName.ToUpperInvariant())
                        {
                            case "LUNA":
                                cornerRadius = new CornerRadius(6, 6, 0, 0);
                                break;
                            case "AERO":
                                // Aero has two cases.  One with glass and one without...
                                if (Standard.NativeMethods.DwmIsCompositionEnabled())
                                {
                                    cornerRadius = new CornerRadius(8);
                                }
                                else
                                {
                                    cornerRadius = new CornerRadius(6, 6, 0, 0);
                                }
                                break;
                            case "CLASSIC":
                            case "ZUNE":
                            case "ROYALE":
                            default:
                                cornerRadius = new CornerRadius(0);
                                break;
                        }

                        _windowCornerRadius = cornerRadius;
                        _cacheValid[(int)CacheSlot.WindowCornerRadius] = true;
                    }
                }

                return _windowCornerRadius;
            }
        }

        /// <summary>
        ///     Color representing the DWM glass for windows in the Aero theme.
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        public static Color WindowGlassColor
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowGlassColor])
                    {
                        bool isOpaque;
                        uint color;
                        Standard.NativeMethods.DwmGetColorizationColor(out color, out isOpaque);
                        color |= isOpaque ? 0xFF000000 : 0;

                        _windowGlassColor = Standard.Utility.ColorFromArgbDword(color);
                        _cacheValid[(int)CacheSlot.WindowGlassColor] = true;
                    }
                }

                return _windowGlassColor;
            }
        }

        /// <summary>
        ///     Brush representing the DWM glass for windows in the Aero theme.
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        public static Brush WindowGlassBrush
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowGlassBrush])
                    {
                        var glassBrush = new SolidColorBrush(WindowGlassColor);
                        glassBrush.Freeze();

                        _windowGlassBrush = glassBrush;
                        _cacheValid[(int)CacheSlot.WindowGlassBrush] = true;
                    }
                }

                return _windowGlassBrush;
            }
        }

        /// <summary>
        ///     Standard thickness of the resize border of a window.
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        public static Thickness WindowResizeBorderThickness
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowResizeBorderThickness])
                    {
                        Size frameSize = new Size(Standard.NativeMethods.GetSystemMetrics(Standard.SM.CXSIZEFRAME),
                                                  Standard.NativeMethods.GetSystemMetrics(Standard.SM.CYSIZEFRAME));
                        Size frameSizeInDips = Standard.DpiHelper.DeviceSizeToLogical(frameSize, SystemParameters.DpiX / 96.0, SystemParameters.Dpi / 96.0);

                        _windowResizeBorderThickness = new Thickness(frameSizeInDips.Width, frameSizeInDips.Height, frameSizeInDips.Width, frameSizeInDips.Height);
                        _cacheValid[(int)CacheSlot.WindowResizeBorderThickness] = true;
                    }
                }

                return _windowResizeBorderThickness;
            }
        }

        /// <summary>
        ///     Standard thickness of the non-client frame around a window.
        /// </summary>
        /// <SecurityNote>
        ///  Critical as this code does an elevation.
        /// </SecurityNote>
        public static Thickness WindowNonClientFrameThickness
        {
            [SecurityCritical]
            get
            {
                lock (_cacheValid)
                {
                    if (!_cacheValid[(int)CacheSlot.WindowNonClientFrameThickness])
                    {
                        Size frameSize = new Size(Standard.NativeMethods.GetSystemMetrics(Standard.SM.CXSIZEFRAME),
                                                  Standard.NativeMethods.GetSystemMetrics(Standard.SM.CYSIZEFRAME));
                        Size frameSizeInDips = Standard.DpiHelper.DeviceSizeToLogical(frameSize, SystemParameters.DpiX / 96.0, SystemParameters.Dpi / 96.0);
                        int captionHeight = Standard.NativeMethods.GetSystemMetrics(Standard.SM.CYCAPTION);
                        double captionHeightInDips = Standard.DpiHelper.DevicePixelsToLogical(new Point(0, captionHeight), SystemParameters.DpiX / 96.0, SystemParameters.Dpi / 96.0).Y;
                        _windowNonClientFrameThickness = new Thickness(frameSizeInDips.Width, frameSizeInDips.Height + captionHeightInDips, frameSizeInDips.Width, frameSizeInDips.Height);
                        _cacheValid[(int)CacheSlot.WindowNonClientFrameThickness] = true;
                    }
                }

                return _windowNonClientFrameThickness;
            }
        }

        internal static int Dpi
        {
            get
            {
                return MS.Internal.FontCache.Util.Dpi;
            }
        }

        ///<SecurityNote>
        ///  Critical as this accesses Native methods.
        ///  TreatAsSafe - it would be ok to expose this information - DPI in partial trust
        ///</SecurityNote>
        internal static int DpiX
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                if (_setDpiX)
                {
                    lock (_cacheValid)
                    {
                        if (_setDpiX)
                        {
                            _setDpiX = false;
                            HandleRef desktopWnd = new HandleRef(null, IntPtr.Zero);

                            // Win32Exception will get the Win32 error code so we don't have to
#pragma warning disable 6523
                            IntPtr dc = UnsafeNativeMethods.GetDC(desktopWnd);

                            // Detecting error case from unmanaged call, required by PREsharp to throw a Win32Exception
#pragma warning disable 6503
                            if (dc == IntPtr.Zero)
                            {
                                throw new Win32Exception();
                            }
#pragma warning restore 6503
#pragma warning restore 6523

                            try
                            {
                                _dpiX = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), NativeMethods.LOGPIXELSX);
                                _cacheValid[(int)CacheSlot.DpiX] = true;
                            }
                            finally
                            {
                                UnsafeNativeMethods.ReleaseDC(desktopWnd, new HandleRef(null, dc));
                            }
                        }
                    }
                }

                return _dpiX;
            }
        }


        internal static double ConvertPixel(int pixel)
        {
            int dpi = Dpi;

            if (dpi != 0)
            {
                return (double)pixel * 96 / dpi;
            }

            return pixel;
        }

        private enum CacheSlot : int
        {
            DpiX,

            FocusBorderWidth,
            FocusBorderHeight,
            HighContrast,
            MouseVanish,

            DropShadow,
            FlatMenu,
            WorkAreaInternal,
            WorkArea,

            IconMetrics,

            KeyboardCues,
            KeyboardDelay,
            KeyboardPreference,
            KeyboardSpeed,
            SnapToDefaultButton,
            WheelScrollLines,
            MouseHoverTime,
            MouseHoverHeight,
            MouseHoverWidth,

            MenuDropAlignment,
            MenuFade,
            MenuShowDelay,

            ComboBoxAnimation,
            ClientAreaAnimation,
            CursorShadow,
            GradientCaptions,
            HotTracking,
            ListBoxSmoothScrolling,
            MenuAnimation,
            SelectionFade,
            StylusHotTracking,
            ToolTipAnimation,
            ToolTipFade,
            UIEffects,

            MinimizeAnimation,
            Border,
            CaretWidth,
            ForegroundFlashCount,
            DragFullWindows,
            NonClientMetrics,

            ThinHorizontalBorderHeight,
            ThinVerticalBorderWidth,
            CursorWidth,
            CursorHeight,
            ThickHorizontalBorderHeight,
            ThickVerticalBorderWidth,
            MinimumHorizontalDragDistance,
            MinimumVerticalDragDistance,
            FixedFrameHorizontalBorderHeight,
            FixedFrameVerticalBorderWidth,
            FocusHorizontalBorderHeight,
            FocusVerticalBorderWidth,
            FullPrimaryScreenWidth,
            FullPrimaryScreenHeight,
            HorizontalScrollBarButtonWidth,
            HorizontalScrollBarHeight,
            HorizontalScrollBarThumbWidth,
            IconWidth,
            IconHeight,
            IconGridWidth,
            IconGridHeight,
            MaximizedPrimaryScreenWidth,
            MaximizedPrimaryScreenHeight,
            MaximumWindowTrackWidth,
            MaximumWindowTrackHeight,
            MenuCheckmarkWidth,
            MenuCheckmarkHeight,
            MenuButtonWidth,
            MenuButtonHeight,
            MinimumWindowWidth,
            MinimumWindowHeight,
            MinimizedWindowWidth,
            MinimizedWindowHeight,
            MinimizedGridWidth,
            MinimizedGridHeight,
            MinimumWindowTrackWidth,
            MinimumWindowTrackHeight,
            PrimaryScreenWidth,
            PrimaryScreenHeight,
            WindowCaptionButtonWidth,
            WindowCaptionButtonHeight,
            ResizeFrameHorizontalBorderHeight,
            ResizeFrameVerticalBorderWidth,
            SmallIconWidth,
            SmallIconHeight,
            SmallWindowCaptionButtonWidth,
            SmallWindowCaptionButtonHeight,
            VirtualScreenWidth,
            VirtualScreenHeight,
            VerticalScrollBarWidth,
            VerticalScrollBarButtonHeight,
            WindowCaptionHeight,
            KanjiWindowHeight,
            MenuBarHeight,
            VerticalScrollBarThumbHeight,
            IsImmEnabled,
            IsMediaCenter,
            IsMenuDropRightAligned,
            IsMiddleEastEnabled,
            IsMousePresent,
            IsMouseWheelPresent,
            IsPenWindows,
            IsRemotelyControlled,
            IsRemoteSession,
            ShowSounds,
            IsSlowMachine,
            SwapButtons,
            IsTabletPC,
            VirtualScreenLeft,
            VirtualScreenTop,

            PowerLineStatus,

            IsGlassEnabled,
            UxThemeName,
            UxThemeColor,
            WindowCornerRadius,
            WindowGlassColor,
            WindowGlassBrush,
            WindowNonClientFrameThickness,
            WindowResizeBorderThickness,

            NumSlots
        }

        private static BitArray _cacheValid = new BitArray((int)CacheSlot.NumSlots);

        private static bool _isGlassEnabled;
        private static string _uxThemeName;
        private static string _uxThemeColor;
        private static CornerRadius _windowCornerRadius;
        private static Color _windowGlassColor;
        private static Brush _windowGlassBrush;
        private static Thickness _windowNonClientFrameThickness;
        private static Thickness _windowResizeBorderThickness;

        private static int _dpiX;
        private static bool _setDpiX = true;

        private static double _focusBorderWidth;
        private static double _focusBorderHeight;
        private static bool _highContrast;
        private static bool _mouseVanish;

        private static bool _dropShadow;
        private static bool _flatMenu;
        private static NativeMethods.RECT _workAreaInternal;
        private static Rect _workArea;

        private static NativeMethods.ICONMETRICS _iconMetrics;

        private static bool _keyboardCues;
        private static int _keyboardDelay;
        private static bool _keyboardPref;
        private static int _keyboardSpeed;
        private static bool _snapToDefButton;
        private static int _wheelScrollLines;
        private static int _mouseHoverTime;
        private static double _mouseHoverHeight;
        private static double _mouseHoverWidth;

        private static bool _menuDropAlignment;
        private static bool _menuFade;
        private static int _menuShowDelay;

        private static bool _comboBoxAnimation;
        private static bool _clientAreaAnimation;
        private static bool _cursorShadow;
        private static bool _gradientCaptions;
        private static bool _hotTracking;
        private static bool _listBoxSmoothScrolling;
        private static bool _menuAnimation;
        private static bool _selectionFade;
        private static bool _stylusHotTracking;
        private static bool _toolTipAnimation;
        private static bool _tooltipFade;
        private static bool _uiEffects;

        private static bool _minAnimation;
        private static int _border;
        private static double _caretWidth;
        private static bool _dragFullWindows;
        private static int _foregroundFlashCount;
        private static NativeMethods.NONCLIENTMETRICS _ncm;

        private static double _thinHorizontalBorderHeight;
        private static double _thinVerticalBorderWidth;
        private static double _cursorWidth;
        private static double _cursorHeight;
        private static double _thickHorizontalBorderHeight;
        private static double _thickVerticalBorderWidth;
        private static double _minimumHorizontalDragDistance;
        private static double _minimumVerticalDragDistance;
        private static double _fixedFrameHorizontalBorderHeight;
        private static double _fixedFrameVerticalBorderWidth;
        private static double _focusHorizontalBorderHeight;
        private static double _focusVerticalBorderWidth;
        private static double _fullPrimaryScreenHeight;
        private static double _fullPrimaryScreenWidth;
        private static double _horizontalScrollBarHeight;
        private static double _horizontalScrollBarButtonWidth;
        private static double _horizontalScrollBarThumbWidth;
        private static double _iconWidth;
        private static double _iconHeight;
        private static double _iconGridWidth;
        private static double _iconGridHeight;
        private static double _maximizedPrimaryScreenWidth;
        private static double _maximizedPrimaryScreenHeight;
        private static double _maximumWindowTrackWidth;
        private static double _maximumWindowTrackHeight;
        private static double _menuCheckmarkWidth;
        private static double _menuCheckmarkHeight;
        private static double _menuButtonWidth;
        private static double _menuButtonHeight;
        private static double _minimumWindowWidth;
        private static double _minimumWindowHeight;
        private static double _minimizedWindowWidth;
        private static double _minimizedWindowHeight;
        private static double _minimizedGridWidth;
        private static double _minimizedGridHeight;
        private static double _minimumWindowTrackWidth;
        private static double _minimumWindowTrackHeight;
        private static double _primaryScreenWidth;
        private static double _primaryScreenHeight;
        private static double _windowCaptionButtonWidth;
        private static double _windowCaptionButtonHeight;
        private static double _resizeFrameHorizontalBorderHeight;
        private static double _resizeFrameVerticalBorderWidth;
        private static double _smallIconWidth;
        private static double _smallIconHeight;
        private static double _smallWindowCaptionButtonWidth;
        private static double _smallWindowCaptionButtonHeight;
        private static double _virtualScreenWidth;
        private static double _virtualScreenHeight;
        private static double _verticalScrollBarWidth;
        private static double _verticalScrollBarButtonHeight;
        private static double _windowCaptionHeight;
        private static double _kanjiWindowHeight;
        private static double _menuBarHeight;
        private static double _verticalScrollBarThumbHeight;
        private static bool _isImmEnabled;
        private static bool _isMediaCenter;
        private static bool _isMenuDropRightAligned;
        private static bool _isMiddleEastEnabled;
        private static bool _isMousePresent;
        private static bool _isMouseWheelPresent;
        private static bool _isPenWindows;
        private static bool _isRemotelyControlled;
        private static bool _isRemoteSession;
        private static bool _showSounds;
        private static bool _isSlowMachine;
        private static bool _swapButtons;
        private static bool _isTabletPC;
        private static double _virtualScreenLeft;
        private static double _virtualScreenTop;
        private static PowerLineStatus _powerLineStatus;

        private static SystemResourceKey _cacheThinHorizontalBorderHeight;
        private static SystemResourceKey _cacheThinVerticalBorderWidth;
        private static SystemResourceKey _cacheCursorWidth;
        private static SystemResourceKey _cacheCursorHeight;
        private static SystemResourceKey _cacheThickHorizontalBorderHeight;
        private static SystemResourceKey _cacheThickVerticalBorderWidth;
        private static SystemResourceKey _cacheFixedFrameHorizontalBorderHeight;
        private static SystemResourceKey _cacheFixedFrameVerticalBorderWidth;
        private static SystemResourceKey _cacheFocusHorizontalBorderHeight;
        private static SystemResourceKey _cacheFocusVerticalBorderWidth;
        private static SystemResourceKey _cacheFullPrimaryScreenWidth;
        private static SystemResourceKey _cacheFullPrimaryScreenHeight;
        private static SystemResourceKey _cacheHorizontalScrollBarButtonWidth;
        private static SystemResourceKey _cacheHorizontalScrollBarHeight;
        private static SystemResourceKey _cacheHorizontalScrollBarThumbWidth;
        private static SystemResourceKey _cacheIconWidth;
        private static SystemResourceKey _cacheIconHeight;
        private static SystemResourceKey _cacheIconGridWidth;
        private static SystemResourceKey _cacheIconGridHeight;
        private static SystemResourceKey _cacheMaximizedPrimaryScreenWidth;
        private static SystemResourceKey _cacheMaximizedPrimaryScreenHeight;
        private static SystemResourceKey _cacheMaximumWindowTrackWidth;
        private static SystemResourceKey _cacheMaximumWindowTrackHeight;
        private static SystemResourceKey _cacheMenuCheckmarkWidth;
        private static SystemResourceKey _cacheMenuCheckmarkHeight;
        private static SystemResourceKey _cacheMenuButtonWidth;
        private static SystemResourceKey _cacheMenuButtonHeight;
        private static SystemResourceKey _cacheMinimumWindowWidth;
        private static SystemResourceKey _cacheMinimumWindowHeight;
        private static SystemResourceKey _cacheMinimizedWindowWidth;
        private static SystemResourceKey _cacheMinimizedWindowHeight;
        private static SystemResourceKey _cacheMinimizedGridWidth;
        private static SystemResourceKey _cacheMinimizedGridHeight;
        private static SystemResourceKey _cacheMinimumWindowTrackWidth;
        private static SystemResourceKey _cacheMinimumWindowTrackHeight;
        private static SystemResourceKey _cachePrimaryScreenWidth;
        private static SystemResourceKey _cachePrimaryScreenHeight;
        private static SystemResourceKey _cacheWindowCaptionButtonWidth;
        private static SystemResourceKey _cacheWindowCaptionButtonHeight;
        private static SystemResourceKey _cacheResizeFrameHorizontalBorderHeight;
        private static SystemResourceKey _cacheResizeFrameVerticalBorderWidth;
        private static SystemResourceKey _cacheSmallIconWidth;
        private static SystemResourceKey _cacheSmallIconHeight;
        private static SystemResourceKey _cacheSmallWindowCaptionButtonWidth;
        private static SystemResourceKey _cacheSmallWindowCaptionButtonHeight;
        private static SystemResourceKey _cacheVirtualScreenWidth;
        private static SystemResourceKey _cacheVirtualScreenHeight;
        private static SystemResourceKey _cacheVerticalScrollBarWidth;
        private static SystemResourceKey _cacheVerticalScrollBarButtonHeight;
        private static SystemResourceKey _cacheWindowCaptionHeight;
        private static SystemResourceKey _cacheKanjiWindowHeight;
        private static SystemResourceKey _cacheMenuBarHeight;
        private static SystemResourceKey _cacheSmallCaptionHeight;
        private static SystemResourceKey _cacheVerticalScrollBarThumbHeight;
        private static SystemResourceKey _cacheIsImmEnabled;
        private static SystemResourceKey _cacheIsMediaCenter;
        private static SystemResourceKey _cacheIsMenuDropRightAligned;
        private static SystemResourceKey _cacheIsMiddleEastEnabled;
        private static SystemResourceKey _cacheIsMousePresent;
        private static SystemResourceKey _cacheIsMouseWheelPresent;
        private static SystemResourceKey _cacheIsPenWindows;
        private static SystemResourceKey _cacheIsRemotelyControlled;
        private static SystemResourceKey _cacheIsRemoteSession;
        private static SystemResourceKey _cacheShowSounds;
        private static SystemResourceKey _cacheIsSlowMachine;
        private static SystemResourceKey _cacheSwapButtons;
        private static SystemResourceKey _cacheIsTabletPC;
        private static SystemResourceKey _cacheVirtualScreenLeft;
        private static SystemResourceKey _cacheVirtualScreenTop;
        private static SystemResourceKey _cacheFocusBorderWidth;
        private static SystemResourceKey _cacheFocusBorderHeight;
        private static SystemResourceKey _cacheHighContrast;
        private static SystemResourceKey _cacheDropShadow;
        private static SystemResourceKey _cacheFlatMenu;
        private static SystemResourceKey _cacheWorkArea;
        private static SystemResourceKey _cacheIconHorizontalSpacing;
        private static SystemResourceKey _cacheIconVerticalSpacing;
        private static SystemResourceKey _cacheIconTitleWrap;
        private static SystemResourceKey _cacheKeyboardCues;
        private static SystemResourceKey _cacheKeyboardDelay;
        private static SystemResourceKey _cacheKeyboardPreference;
        private static SystemResourceKey _cacheKeyboardSpeed;
        private static SystemResourceKey _cacheSnapToDefaultButton;
        private static SystemResourceKey _cacheWheelScrollLines;
        private static SystemResourceKey _cacheMouseHoverTime;
        private static SystemResourceKey _cacheMouseHoverHeight;
        private static SystemResourceKey _cacheMouseHoverWidth;
        private static SystemResourceKey _cacheMenuDropAlignment;
        private static SystemResourceKey _cacheMenuFade;
        private static SystemResourceKey _cacheMenuShowDelay;
        private static SystemResourceKey _cacheComboBoxAnimation;
        private static SystemResourceKey _cacheClientAreaAnimation;
        private static SystemResourceKey _cacheCursorShadow;
        private static SystemResourceKey _cacheGradientCaptions;
        private static SystemResourceKey _cacheHotTracking;
        private static SystemResourceKey _cacheListBoxSmoothScrolling;
        private static SystemResourceKey _cacheMenuAnimation;
        private static SystemResourceKey _cacheSelectionFade;
        private static SystemResourceKey _cacheStylusHotTracking;
        private static SystemResourceKey _cacheToolTipAnimation;
        private static SystemResourceKey _cacheToolTipFade;
        private static SystemResourceKey _cacheUIEffects;
        private static SystemResourceKey _cacheMinimizeAnimation;
        private static SystemResourceKey _cacheBorder;
        private static SystemResourceKey _cacheCaretWidth;
        private static SystemResourceKey _cacheForegroundFlashCount;
        private static SystemResourceKey _cacheDragFullWindows;
        private static SystemResourceKey _cacheBorderWidth;
        private static SystemResourceKey _cacheScrollWidth;
        private static SystemResourceKey _cacheScrollHeight;
        private static SystemResourceKey _cacheCaptionWidth;
        private static SystemResourceKey _cacheCaptionHeight;
        private static SystemResourceKey _cacheSmallCaptionWidth;
        private static SystemResourceKey _cacheMenuWidth;
        private static SystemResourceKey _cacheMenuHeight;
        private static SystemResourceKey _cacheComboBoxPopupAnimation;
        private static SystemResourceKey _cacheMenuPopupAnimation;
        private static SystemResourceKey _cacheToolTipPopupAnimation;
        private static SystemResourceKey _cachePowerLineStatus;

        private static SystemThemeKey _cacheFocusVisualStyle;
        private static SystemThemeKey _cacheNavigationChromeStyle;
        private static SystemThemeKey _cacheNavigationChromeDownLevelStyle;

        #endregion
    }
}

