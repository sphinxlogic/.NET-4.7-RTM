// <copyright file="StylusDeviceCollection.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>

using System;
using System.Windows;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// Collection of the stylus devices that are available on the tablet.
    /// </summary>
    public class StylusDeviceCollection : ReadOnlyCollection<StylusDevice>
    {
        /// <summary>
        /// DDVSO:197685
        /// This was changed to IEnumerable since the collection is exposed to
        /// developers.  Internally we use the inheritance hierarchy but externally
        /// we use the wrapper classes, requiring us to build the list dynamically.
        /// </summary>
        /// <param name="styluses">The collection of stylus objects</param>
        internal StylusDeviceCollection(IEnumerable<StylusDeviceBase> styluses)
            : base(new List<StylusDevice>())
        {
            foreach (var stylusDevice in styluses)
            {
                Items.Add(stylusDevice.StylusDevice);
            }
        }

        /// <SecurityNote>
        ///     Critical: calls SecurityCritical method stylusDevice.Dispose.
        /// </SecurityNote>
        [SecurityCritical]
        internal void Dispose()
        {
            foreach (StylusDevice stylusDevice in this.Items)
            {
                stylusDevice.StylusDeviceImpl.Dispose();
            }
        }

        internal void AddStylusDevice(int index, StylusDeviceBase stylusDevice)
        {
            base.Items.Insert(index, stylusDevice.StylusDevice); // add it to our list.
        }
    }
}
