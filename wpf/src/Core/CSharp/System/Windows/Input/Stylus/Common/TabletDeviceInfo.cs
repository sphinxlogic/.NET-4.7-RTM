using System;
using System.Collections.ObjectModel;
using System.Security;
using MS.Internal;
using MS.Win32.Penimc;
using System.Windows.Media;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The struct is used to store the tablet device size information.
    /// </summary>
    internal struct TabletDeviceSizeInfo
    {
        public Size TabletSize;
        public Size ScreenSize;

        // Constructor
        internal TabletDeviceSizeInfo(Size tabletSize, Size screenSize)
        {
            TabletSize = tabletSize;
            ScreenSize = screenSize;
        }
    }


    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The class is used to store tablet device information.
    /// </summary>
    internal class TabletDeviceInfo
    {
        /// <SecurityNote>
        ///     This data is not safe to expose as it holds refrence to IPimcTablet
        /// </SecurityNote>
        [SecurityCritical]
        public SecurityCriticalDataClass<IPimcTablet2> PimcTablet;
        public int Id;
        public string Name;
        public string PlugAndPlayId;
        public TabletDeviceSizeInfo SizeInfo;
        public TabletHardwareCapabilities HardwareCapabilities;
        public TabletDeviceType DeviceType;
        public ReadOnlyCollection<StylusPointProperty> StylusPointProperties;
        public int PressureIndex;
        public StylusDeviceInfo[] StylusDevicesInfo;

        /// <summary>
        /// The GIT key for a WISP tablet COM object.
        /// </summary>
        /// <SecurityNote>
        /// Critical:   This data can be used to manipulate COM objects in the GIT.
        /// </SecurityNote>
        public UInt32 WispTabletKey { [SecurityCritical] get; [SecurityCritical] set; }
    }    
}

