//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Security;
using System.Security.Permissions;

using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Input.StylusWisp
{
    /// <summary>
    ///     The implementation of a TouchDevice specific to StylusDevice.
    /// </summary>
    internal sealed class WispStylusTouchDevice : StylusTouchDeviceBase
    {
        #region TouchDevice Implementation

        /// <SecurityNote>
        ///     Critical: Accesses critical _stylusLogic and InputManager
        ///     TreatAsSafe: This constructor handles critical data but does not expose it
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal WispStylusTouchDevice(StylusDeviceBase stylusDevice)
            : base(stylusDevice)
        {
            _stylusLogic = StylusLogic.GetCurrentStylusLogicAs<WispLogic>();
            PromotingToOther = true;
        }

        /// <summary>
        /// Get the width or height of the stylus point's bounding box.
        /// </summary>
        /// <param name="stylusPoint">The point for which the width or height is being calculated</param>
        /// <param name="isWidth">True if this should calculate width, false for height</param>
        /// <returns>The width or height of the stylus poing</returns>
        /// <remarks>
        /// Note that this is not DPI aware.  This implementation has never been aware of DPI changes and
        /// changing that now could cause issues with people who depended on this to be based on 96 DPI.
        /// </remarks>
        protected override double GetStylusPointWidthOrHeight(StylusPoint stylusPoint, bool isWidth)
        {
            double pixelsPerInch = DpiUtil.DefaultPixelsPerInch;

            StylusPointProperty property = (isWidth ? StylusPointProperties.Width : StylusPointProperties.Height);

            double value = 0d;

            if (stylusPoint.HasProperty(property))
            {
                // Get the property value in the corresponding units
                value = (double)stylusPoint.GetPropertyValue(property);

                StylusPointPropertyInfo propertyInfo = stylusPoint.Description.GetPropertyInfo(property);

                if (!DoubleUtil.AreClose(propertyInfo.Resolution, 0d))
                {
                    value /= propertyInfo.Resolution;
                }
                else
                {
                    value = 0;
                }

                // Convert the value to Inches
                if (propertyInfo.Unit == StylusPointPropertyUnit.Centimeters)
                {
                    value /= CentimetersPerInch;
                }

                // Convert the value to pixels
                value *= pixelsPerInch;
            }

            return value;
        }

        #endregion

        #region StylusLogic Related

        protected override void OnManipulationStarted()
        {
            base.OnManipulationStarted();
            PromotingToOther = false;
        }

        /// <SecurityNote>
        ///     Critical: Accesses _stylusLogic and _storedStagingAreaItems
        ///                and calls StylusLogic.PromoteStoredItemsToMouse
        ///     PublicOK: This method has a demand on it.
        ///     Demand:   Technically the demand is not needed because the
        ///               user can already do this indirectly by canceling the
        ///               manipulation. But the decision is to limit the scope
        ///               of this raw method to full trust.
        /// </SecurityNote>
        [SecurityCritical, UIPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        protected override void OnManipulationEnded(bool cancel)
        {
            base.OnManipulationEnded(cancel);
            if (cancel)
            {
                // Set PromotingToOther to true because the
                // the manipulation has been canceled and hence
                // we want the promotions to happen.
                PromotingToOther = true;
                _stylusLogic.PromoteStoredItemsToMouse(this);
            }
            else
            {
                // Set PromotingToOther to false because we
                // dont want any mouse promotions to happen for
                // rest of this touch cycle.
                PromotingToOther = false;
            }
            if (_storedStagingAreaItems != null)
            {
                _storedStagingAreaItems.Clear();
            }
        }

        /// <summary>
        ///     Stored items that occurred during the time before we knew where to promote to.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: The items stored in this collection will be promoted to mouse.
        /// </SecurityNote>
        internal WispLogic.StagingAreaInputItemList StoredStagingAreaItems
        {
            [SecurityCritical]
            get
            {
                if (_storedStagingAreaItems == null)
                {
                    _storedStagingAreaItems = new WispLogic.StagingAreaInputItemList();
                }
                return _storedStagingAreaItems;
            }
        }

        /// <SecurityNote>
        ///     Critical: Accesses _stylusLogic. Could be TreatAsSafe
        ///               because doesn't expose _stylusLogic itself.
        /// </SecurityNote>
        [SecurityCritical]
        protected override void OnActivateImpl()
        {
            if (ActiveDeviceCount == 1)
            {
                _stylusLogic.CurrentMousePromotionStylusDevice = StylusDevice;
            }
        }

        /// <SecurityNote>
        ///     Critical: Accesses _stylusLogic and _storedStagingAreaItems.
        ///               Clears _storedStagingAreaItems collection which
        ///               stores the staging items to be promoted to mouse.
        ///               Could be TreatAsSafe because doesn't expose the
        ///               objects themselves.
        /// </SecurityNote>
        [SecurityCritical]
        protected override void OnDeactivateImpl()
        {
            if (_storedStagingAreaItems != null)
            {
                _storedStagingAreaItems.Clear();
            }

            if (ActiveDeviceCount == 0)
            {
                _stylusLogic.CurrentMousePromotionStylusDevice = null;
            }
            else if (IsPrimary)
            {
                _stylusLogic.CurrentMousePromotionStylusDevice = NoMousePromotionStylusDevice;
            }
        }

        #endregion

        #region Data

        /// <SecurityNote>
        ///     Critical: To prevent accidental spread to transparent code
        /// </SecurityNote>
        [SecurityCritical]
        private WispLogic _stylusLogic;

        /// <SecurityNote>
        ///     Critical: Holds the staging items to be promoted to mouse.
        /// </SecurityNote>
        [SecurityCritical]
        private WispLogic.StagingAreaInputItemList _storedStagingAreaItems;

        private static object NoMousePromotionStylusDevice = new object();

        #endregion
    }
}
