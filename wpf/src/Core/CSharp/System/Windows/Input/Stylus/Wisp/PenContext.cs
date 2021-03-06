using System;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using MS.Internal;
using MS.Win32.Penimc;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////

    internal sealed class PenContext
    {
        /// <SecurityNote>
        /// Critical - Handles security critical data pimcContext, hwnd, and commHandle that
        ///             is created under elavated priviledge.
        ///             Called directly by TabletDevice.CreateContext().
        ///             TreatAsSafe boundry is Stylus.EnableCore, Stylus.RegisterHwndForInput
        ///                and HwndWrapperHook class (via HwndSource.InputFilterMessage).
        /// </SecurityNote>
        [SecurityCritical]
        internal PenContext(IPimcContext2 pimcContext, IntPtr hwnd, 
                                PenContexts contexts, bool supportInRange, bool isIntegrated,
                                int id, IntPtr commHandle, int tabletDeviceId, UInt32 wispContextKey)
        {
            _contexts = contexts;
            _pimcContext = new SecurityCriticalDataClass<IPimcContext2>(pimcContext);
            _id = id;
            _tabletDeviceId = tabletDeviceId;
            _commHandle = new SecurityCriticalData<IntPtr>(commHandle);
            _hwnd = new SecurityCriticalData<IntPtr>(hwnd);
            _supportInRange = supportInRange;
            _isIntegrated = isIntegrated;
            WispContextKey = wispContextKey;
            UpdateScreenMeasurementsPending = false;
        }

        /////////////////////////////////////////////////////////////////////

        ~PenContext()
        {
            Dispose();
        }

        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles clean up of internal object data.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Calls critical code Disable.
        ///          Called by Finalizer.
        /// TreatAsSafe -  Handles critical data but does not expose it.
        ///             -  No critical data returned or accepted as input.
        /// </SecurityNote>
        [SecuritySafeCritical]
        void Dispose()
        {
            Disable(false /*shutdownWorkerThread*/); // Make sure we remove the context from the active list.
           
            _pimcContext = null;
            _contexts = null;

            GC.KeepAlive(this);
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        ///     Critical: Accesses and returns SecurityCritical data _contexts.
        /// </SecurityNote>
        internal PenContexts Contexts
        {
            [SecurityCritical]
            get
            {
                return _contexts;
            }
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Critical since it returns critical data.
        /// </SecurityNote>
        internal IntPtr CommHandle
        {
            [SecurityCritical]
            get
            {
                return _commHandle.Value;
            }
        }

        /////////////////////////////////////////////////////////////////////

        internal int Id
        {
            get
            {
                return _id;
            }
        }

        /////////////////////////////////////////////////////////////////////

        internal int TabletDeviceId
        {
            get
            {
                return _tabletDeviceId;
            }
        }

        /////////////////////////////////////////////////////////////////////////

        internal StylusPointDescription StylusPointDescription
        {
            get
            {
                if (_stylusPointDescription == null)
                {
                    InitStylusPointDescription(); // init _stylusPointDescription.
                }

                return _stylusPointDescription;
            }
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Critical since it calls into unmanaged code (GetPacketPropertyInfo
        ///             GetPacketButtonInfo and GetPacketDescriptionInfo PenImc.dll 
        ///             methods on IPimcContext2 COM interfaces stored in _pimcContext) that
        ///             is SecurityCritical with SUC attribute.
        /// TreatAsSafe - Takes no input and returns StylusPointDescription that is handed out publically
        ///                on StylusEvents.  Nothing inside StylusPointDescription is security critical.
        /// </SecurityNote>
        [SecuritySafeCritical]
        private void InitStylusPointDescription()
        {
            int cProps;
            int cButtons;
            int pressureIndex = -1;

            // Make sure we are never called on the application thread when we need to talk
            // to penimc or else we can cause reentrancy!
            Debug.Assert(!_contexts._inputSource.Value.CheckAccess());

            // We should always have a valid IPimcContext2 interface pointer.
            Debug.Assert(_pimcContext != null && _pimcContext.Value != null);
            
            _pimcContext.Value.GetPacketDescriptionInfo(out cProps, out cButtons); // Calls Unmanaged code - SecurityCritical with SUC.

            List<StylusPointPropertyInfo> propertyInfos = new List<StylusPointPropertyInfo>(cProps + cButtons + 3);
            for (int i = 0; i < cProps; i++)
            {
                Guid guid;
                int min, max;
                int units;
                float res;
                _pimcContext.Value.GetPacketPropertyInfo(i, out guid, out min, out max, out units, out res); // Calls Unmanaged code - SecurityCritical with SUC.

                if (pressureIndex == -1 && guid == StylusPointPropertyIds.NormalPressure)
                {
                    pressureIndex = i;
                }
                
                if (_statusPropertyIndex == -1 && guid == StylusPointPropertyIds.PacketStatus)
                {
                    _statusPropertyIndex = i;
                }

                StylusPointPropertyInfo propertyInfo = new StylusPointPropertyInfo(new StylusPointProperty(guid, false), min, max, (StylusPointPropertyUnit)units, res);
                
                propertyInfos.Add(propertyInfo);
            }

            Debug.Assert(_statusPropertyIndex != -1);  // We should always see this.
            
            // Make sure we actually created propertyInfos OK
            if (propertyInfos != null)
            {
                for (int i = 0; i < cButtons; i++)
                {
                    Guid buttonGuid;
                    _pimcContext.Value.GetPacketButtonInfo(i, out buttonGuid); // Calls Unmanaged code - SecurityCritical with SUC.

                    StylusPointProperty buttonProperty = new StylusPointProperty(buttonGuid, true);
                    StylusPointPropertyInfo buttonInfo = new StylusPointPropertyInfo(buttonProperty);
                    propertyInfos.Add(buttonInfo);
                }

                //validate we can never get X, Y at index != 0, 1
                Debug.Assert(propertyInfos[StylusPointDescription.RequiredXIndex /*0*/].Id == StylusPointPropertyIds.X, "X isn't where we expect it! Fix PenImc to ask for X at index 0");
                Debug.Assert(propertyInfos[StylusPointDescription.RequiredYIndex /*0*/].Id == StylusPointPropertyIds.Y, "Y isn't where we expect it! Fix PenImc to ask for Y at index 1");
                Debug.Assert(pressureIndex == -1 || pressureIndex == StylusPointDescription.RequiredPressureIndex /*2*/, 
                    "Fix PenImc to ask for NormalPressure at index 2!");
                if (pressureIndex == -1)
                {
                    //pressure wasn't found.  Add it
                    propertyInfos.Insert(StylusPointDescription.RequiredPressureIndex /*2*/, StylusPointPropertyInfoDefaults.NormalPressure);
                }
                _infoX = propertyInfos[0];
                _infoY = propertyInfos[1];
                
                _stylusPointDescription = new StylusPointDescription(propertyInfos, pressureIndex);
            }

        }


        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenThreadPool.GetPenThreadForPenContext().
        ///            Accesses SecurityCriticalData _pimcContext.Value and sets _penThreadPenContext.
        ///             Called by PenContexts.Enable().
        ///             TreatAsSafe boundry is Stylus.EnableCore, Stylus.RegisterHwndForInput
        ///                and HwndWrapperHook class (via HwndSource.InputFilterMessage).
        /// </SecurityNote>
        [SecurityCritical]
        internal void Enable()
        {
            if (_pimcContext != null && _pimcContext.Value != null)
            {
                _penThreadPenContext = PenThreadPool.GetPenThreadForPenContext(this);
            }
        }

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code (PenThread.RemovePenContext).
        ///             Called by PenContexts.RemoveContext and PenContexts.Disable.
        /// </SecurityNote>
        [SecurityCritical]
        internal void Disable(bool shutdownWorkerThread)
        {
            // DDVSO:202023
            // There was a prior assumption here that this would always be called under Dispatcher.DisableProcessing.
            // This assumption has not been valid for some time, leading to re-entrancy.
            if (_penThreadPenContext != null)
            {
                if (_penThreadPenContext.RemovePenContext(this))
                {
                    // Check if we need to shut down our pen thread.
                    if (shutdownWorkerThread)
                    {
                        // DDVSO:202023
                        // A re-entrant call might find us with a null penThreadContext so we 
                        // need to guard the call to Dispose against a null even though we already 
                        // have a check above.
                        _penThreadPenContext?.Dispose();
                    }

                    _penThreadPenContext = null; // Can't free this ref until we can remove this context or else we won't see the stylus OutOfRange.
                }
            }
        }

        /////////////////////////////////////////////////////////////////////

        internal bool SupportInRange
        {
            get
            {
                return _supportInRange;
            }
        }

        internal bool IsInRange(int stylusPointerId)
        {
            // zero is a special case where we want to know if any stylus devices are in range.
            if (stylusPointerId == 0)
                return _stylusDevicesInRange != null && _stylusDevicesInRange.Count > 0;
            else
                return (_stylusDevicesInRange != null && _stylusDevicesInRange.Contains(stylusPointerId));
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenContexts.OnPenDown.
        ///             Called by PenThread.FireEvent.
        ///             TreatAsSafe boundary is PenThread.ThreadProc.
        /// </SecurityNote>
        [SecurityCritical]
        internal void FirePenDown (int stylusPointerId, int[] data, int timestamp)
        {
            timestamp = EnsureTimestampUnique(timestamp);
            _lastInRangeTime = timestamp;

            // make sure this gets initialized on the penthread!!
            if (_stylusPointDescription == null)
            {
                InitStylusPointDescription(); // init _stylusPointDescription.
            }
            
            _contexts.OnPenDown(this, _tabletDeviceId, stylusPointerId, data, timestamp);
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenContext.OnPenUp.
        ///             Called by PenThread.FireEvent.
        ///             TreatAsSafe boundary is PenThread.ThreadProc.
        /// </SecurityNote>
        [SecurityCritical]
        internal void FirePenUp (int stylusPointerId, int[] data, int timestamp)
        {
            timestamp = EnsureTimestampUnique(timestamp);
            _lastInRangeTime = timestamp;
            
            // make sure this gets initialized on the penthread!!
            if (_stylusPointDescription == null)
            {
                InitStylusPointDescription(); // init _stylusPointDescription.
            }
            
            _contexts.OnPenUp(this, _tabletDeviceId, stylusPointerId, data, timestamp);
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenContexts.OnPackets and PenContexts.OnInAirPackets.
        ///             Called by PenThread.FireEvent.
        ///             TreatAsSafe boundary is PenThread.ThreadProc.
        /// </SecurityNote>
        [SecurityCritical]
        internal void FirePackets(int stylusPointerId, int[] data, int timestamp)
        {
            timestamp = EnsureTimestampUnique(timestamp);
            _lastInRangeTime = timestamp;
            
            // make sure this gets initialized on the penthread!!
            if (_stylusPointDescription == null)
            {
                InitStylusPointDescription(); // init _stylusPointDescription.
            }
            
            bool fDownPackets = false;
            if (_statusPropertyIndex != -1)
            {
                int status = data[_statusPropertyIndex]; // (we take status of the first packet for status of all of them)
                fDownPackets = (status & 1/*IP_CURSOR_DOWN*/) != 0;
            }

            if (fDownPackets)
            {
                _contexts.OnPackets(this, _tabletDeviceId, stylusPointerId, data, timestamp);
            }
            else
            {
                _contexts.OnInAirPackets(this, _tabletDeviceId, stylusPointerId, data, timestamp);
            }
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenContexts.OnPenInRange.
        ///             Called by PenThread.FireEvent.
        ///             TreatAsSafe boundary is PenThread.ThreadProc.
        /// </SecurityNote>
        [SecurityCritical]
        internal void FirePenInRange(int stylusPointerId, int[] data, int timestamp)
        {
            // make sure this gets initialized on the penthread!!
            if (_stylusPointDescription == null)
            {
                InitStylusPointDescription(); // init _stylusPointDescription.
            }
            
            // Special case where we want to forward this to the application early (this is the real
            // stylus InRange event we don't currently use).
            if (data == null)
            {
                _lastInRangeTime = timestamp; // Always reset timestamp on InRange!!  Don't call EnsureTimestampUnique.

                _queuedInRangeCount++;
                _contexts.OnPenInRange(this, _tabletDeviceId, stylusPointerId, data, timestamp);
                return;
            }

            // This should not be called if this stylus ID is 0
            System.Diagnostics.Debug.Assert(stylusPointerId != 0);

            if (!IsInRange(stylusPointerId))
            {
                _lastInRangeTime = timestamp; // Always reset timestamp on InRange!!  Don't call EnsureTimestampUnique.

                if (_stylusDevicesInRange == null)
                {
                    _stylusDevicesInRange = new List<int>(); // create it as needed.
                }
                _stylusDevicesInRange.Add(stylusPointerId);
                _contexts.OnPenInRange(this, _tabletDeviceId, stylusPointerId, data, timestamp);
            }
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code PenContexts.OnPenOutOfRange.
        ///             Called by PenThread.FireEvent.
        ///             TreatAsSafe boundary is PenThread.ThreadProc.
        /// </SecurityNote>
        [SecurityCritical]
        internal void FirePenOutOfRange(int stylusPointerId, int timestamp)
        {
            // We only do work here if we truly had a stylus in range.
            if (stylusPointerId != 0)
            {
                if (IsInRange(stylusPointerId))
                {
                    timestamp = EnsureTimestampUnique(timestamp);
                    _lastInRangeTime = timestamp;

                    // make sure this gets initialized on the penthread!!
                    if (_stylusPointDescription == null)
                    {
                        InitStylusPointDescription(); // init _stylusPointDescription.
                    }

                    _stylusDevicesInRange.Remove(stylusPointerId);
                    _contexts.OnPenOutOfRange(this, _tabletDeviceId, stylusPointerId, timestamp);
                    if (_stylusDevicesInRange.Count == 0)
                    {
                        _stylusDevicesInRange = null; // not needed anymore.
                    }
                }
            }
            else if (_stylusDevicesInRange != null)
            {
                timestamp = EnsureTimestampUnique(timestamp);
                _lastInRangeTime = timestamp;

                // make sure this gets initialized on the penthread!!
                if (_stylusPointDescription == null)
                {
                    InitStylusPointDescription(); // init _stylusPointDescription.
                }

                // Send event for each StylusDevice being out of range, then clear out the map.
                for(int i=0; i < _stylusDevicesInRange.Count; i++)
                {
                    _contexts.OnPenOutOfRange(this, _tabletDeviceId, _stylusDevicesInRange[i], timestamp);
                }
                _stylusDevicesInRange = null; // nothing in range now.
            }
        }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code MS.Win32.Penimc.UnsafeNativeMethods.GetLastSystemEventData
        ///             and PenContexts.OnSystemEvent.  Also handles SecurityCritical data _commHandle.
        ///             Called by PenThread.FireEvent.
        ///             TreatAsSafe boundary is PenThread.ThreadProc.
        /// </SecurityNote>
        [SecurityCritical]
        internal void FireSystemGesture(int stylusPointerId, int timestamp)
        {
            timestamp = EnsureTimestampUnique(timestamp);
            _lastInRangeTime = timestamp;
            
            // make sure this gets initialized on the penthread!!
            if (_stylusPointDescription == null)
            {
                InitStylusPointDescription(); // init _stylusPointDescription.
            }
            
            int id;
            int modifier;
            int character;
            int stylusMode;
            int x, y, buttonState; // (these are not used)

            MS.Win32.Penimc.UnsafeNativeMethods.GetLastSystemEventData(
                _commHandle.Value,
                out id, out modifier, out character,
                out x, out y, out stylusMode, out buttonState);

            _contexts.OnSystemEvent(
                    this, _tabletDeviceId, stylusPointerId, timestamp,
                    (SystemGesture)id, x, y, buttonState);
        }

        /////////////////////////////////////////////////////////////////////
        //
        // Iterates through data for every packet, checks packet status and invalidates
        // screen measurements accordingly
        //
        internal void CheckForRectMappingChanged(int[] data, int numPackets)
        {
            const int ipRectMappingChangedFlag = 0x10;
            if (UpdateScreenMeasurementsPending)
            {
                return;
            }

            Debug.Assert(numPackets != 0);
            Debug.Assert(data != null);
            
            if (_stylusPointDescription == null)
            {
                InitStylusPointDescription(); // init _stylusPointDescription.
            }

            if (_statusPropertyIndex == -1)
            {
                return;
            }

            // Check each packet to see if the rects have changed.
            int itemsPerPacket = data.Length / numPackets;
            for (int i = 0; i < numPackets; i++)
            {
                int status = data[(i * itemsPerPacket) + _statusPropertyIndex];
                if ((status & ipRectMappingChangedFlag) != 0) // Is IP_RECT_MAPPING_CHANGED (0x00000010) set?
                {
                    UpdateScreenMeasurementsPending = true;
                    break;
                }
            }
        }

        internal bool UpdateScreenMeasurementsPending
        {
            get;
            set;
        }
        
        /////////////////////////////////////////////////////////////////////
        //
        // Make sure timestamp is unique for each event.  Note that on each InRange event the
        // timestamp _lastInRangeTime is reset to the real event time so we don't have to worry
        // about really stale timestamps due to lack of use of a stylus device.
        //
        private int EnsureTimestampUnique(int timestamp)
        {
            int delta = unchecked(_lastInRangeTime - timestamp);

            // Is last time more current?  If so we need to increment and return this.
            // NOTE: This deals with wrapping from MaxInt to MinInt.
            //  Here's some info on how this works...
            //      int.MaxValue - int.MinValue = -1 (subtracting any negative # from MaxValue keeps this negative)
            //      int.MinValue - int.MaxValue = 1 (subtracting any positive # from MinValue keeps this positive)
            //  So as _lastInRangeTime approaches MaxInt if subtracting timestamp is positive then timestamp
            //   is older and we want to use _lastInRangeTime + 1 to keep the time unique.
            //  and if _lastInRangeTime is near MinInt then if subtracting timestamp is positive the
            //   same condition is true in that timestamp is older.
            if (delta >= 0)
            {
                timestamp = unchecked(_lastInRangeTime + 1);
            }
            
            return timestamp;
        }
        
        // This keeps track of the last time we saw an InRange/OutOfRange event on the pen thread.
        internal int LastInRangeTime
        {
            get { return _lastInRangeTime; }
        }

        // This returns the count of queued special InRange reports that we use to know if we are
        // potentially going inrange.
        internal int QueuedInRangeCount
        {
            get { return _queuedInRangeCount; }
        }

        // The application uses this to decrement the queued InRange count when it arrives on the app thread.
        internal void DecrementQueuedInRangeCount()
        {
            _queuedInRangeCount--;
        }

        /// <summary>
        /// The GIT key for a WISP context COM object.
        /// </summary>
        internal UInt32 WispContextKey { get; private set; }

        /////////////////////////////////////////////////////////////////////

        /// <SecurityNote>
        ///     SecurityCritical - This is got under an elevation and is hence critical.
        /// </SecurityNote>
        [SecurityCritical]
        internal SecurityCriticalDataClass<IPimcContext2> _pimcContext;
        
        /// <SecurityNote>
        ///     SecurityCritical - This is got under an elevation and is hence critical.
        /// </SecurityNote>
        [SecurityCritical]
        SecurityCriticalData<IntPtr> _hwnd;
        
        /// <SecurityNote>
        ///     SecurityCritical - This is got under an elevation and is hence critical.
        /// </SecurityNote>
        [SecurityCritical]
        SecurityCriticalData<IntPtr> _commHandle;
        
        /// <SecurityNote>
        ///     Critical to prevent accidental spread to transparent code
        /// </SecurityNote>
        [SecurityCritical]
        PenContexts             _contexts;
        
        /// <SecurityNote>
        ///     Critical to prevent accidental spread to transparent code
        /// </SecurityNote>
        [SecurityCritical]
        PenThread               _penThreadPenContext;
        int                     _id;
        int                     _tabletDeviceId;
        StylusPointPropertyInfo _infoX;
        StylusPointPropertyInfo _infoY;
        bool                    _supportInRange;
        List<int>               _stylusDevicesInRange;
        bool                    _isIntegrated;

        StylusPointDescription  _stylusPointDescription;
        int                     _statusPropertyIndex = -1;

        int                     _lastInRangeTime;
        int                     _queuedInRangeCount;
    }
}
