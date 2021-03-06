using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Input.StylusWisp;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using MS.Win32;
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Utility;
using System.Security;
using System.Security.Permissions;


using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Interop
{
    /////////////////////////////////////////////////////////////////////////

    internal sealed class HwndStylusInputProvider : DispatcherObject, IStylusInputProvider
    {
        private const uint TABLET_PRESSANDHOLD_DISABLED = 0x00000001;
        private const uint TABLET_TAPFEEDBACK_DISABLED  = 0x00000008;
        private const uint TABLET_TOUCHUI_FORCEON       = 0x00000100;
        private const uint TABLET_TOUCHUI_FORCEOFF      = 0x00000200;
        private const uint TABLET_FLICKS_DISABLED       = 0x00010000;

        private const int MultiTouchEnabledFlag         = 0x01000000;

        /////////////////////////////////////////////////////////////////////
        /// <SecurityNote>
        ///     Accesses and stores critical data (_site, _source, _stylusLogic).
        /// </SecurityNote>
        [SecurityCritical]
        internal HwndStylusInputProvider(HwndSource source)
        {
            InputManager inputManager = InputManager.Current;
            _stylusLogic = new SecurityCriticalDataClass<WispLogic>(StylusLogic.GetCurrentStylusLogicAs<WispLogic>());

            IntPtr sourceHandle;

            (new UIPermission(PermissionState.Unrestricted)).Assert();
            try //Blessed Assert this is for RegisterInputManager and RegisterHwndforinput
            {
                // Register ourselves as an input provider with the input manager.
                _site = new SecurityCriticalDataClass<InputProviderSite>(inputManager.RegisterInputProvider(this));

                sourceHandle = source.Handle;
            }
            finally
            {
                UIPermission.RevertAssert();
            }

            _stylusLogic.Value.RegisterHwndForInput(inputManager, source);
            _source = new SecurityCriticalDataClass<HwndSource>(source);

            // Enables multi-touch input
            UnsafeNativeMethods.SetProp(new HandleRef(this, sourceHandle), "MicrosoftTabletPenServiceProperty", new HandleRef(null, new IntPtr(MultiTouchEnabledFlag)));
        }

        /////////////////////////////////////////////////////////////////////


        /// <SecurityNote>
        ///     Critical:This class accesses critical data, _site
        ///     TreatAsSafe: This class does not expose the critical data.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        public void Dispose()
        {
            if(_site != null)
            {
                _site.Value.Dispose();
                _site = null;

                _stylusLogic.Value.UnRegisterHwndForInput(_source.Value);
                _stylusLogic = null;
                _source = null;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <SecurityNote>
        ///     Critical: This method acceses critical data hwndsource
        ///     TreatAsSafe:Information about whether a given input provider services
        ///     a visual is safe to expose. This method does not expose the critical data either.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        bool IInputProvider.ProvidesInputForRootVisual(Visual v)
        {
            Debug.Assert( null != _source );
            return _source.Value.RootVisual == v;
        }

        void IInputProvider.NotifyDeactivate() {}

        /////////////////////////////////////////////////////////////////////
        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        /// <SecurityNote>
        ///     Critical: This code is critical since it handles all stylus messages and could be used to spoof input
        /// </SecurityNote>
        [SecurityCritical]
        IntPtr IStylusInputProvider.FilterMessage(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;

            // It is possible to be re-entered during disposal.  Just return.
            if(null == _source || null == _source.Value)
            {
                return result;
            }

            switch(msg)
            {
                case WindowMessage.WM_ENABLE:
                    _stylusLogic.Value.OnWindowEnableChanged(hwnd, (int)NativeMethods.IntPtrToInt32(wParam) == 0);
                    break;

                case WindowMessage.WM_TABLET_QUERYSYSTEMGESTURESTATUS:
                    handled = true;

                    NativeMethods.POINT pt1 = new NativeMethods.POINT(
                                            NativeMethods.SignedLOWORD(lParam),
                                            NativeMethods.SignedHIWORD(lParam));
                    SafeNativeMethods.ScreenToClient(new HandleRef(this, hwnd), pt1);
                    Point ptClient1 = new Point(pt1.x, pt1.y);

                    IInputElement inputElement = StylusDevice.LocalHitTest(_source.Value, ptClient1);
                    if (inputElement != null)
                    {
                        // walk up the parent chain
                        DependencyObject elementCur = (DependencyObject)inputElement;
                        bool isPressAndHoldEnabled = Stylus.GetIsPressAndHoldEnabled(elementCur);
                        bool isFlicksEnabled = Stylus.GetIsFlicksEnabled(elementCur);
                        bool isTapFeedbackEnabled = Stylus.GetIsTapFeedbackEnabled(elementCur);
                        bool isTouchFeedbackEnabled = Stylus.GetIsTouchFeedbackEnabled(elementCur);

                        uint flags = 0;

                        if (!isPressAndHoldEnabled)
                        {
                            flags |= TABLET_PRESSANDHOLD_DISABLED;
                        }

                        if (!isTapFeedbackEnabled)
                        {
                            flags |= TABLET_TAPFEEDBACK_DISABLED;
                        }

                        if (isTouchFeedbackEnabled)
                        {
                            flags |= TABLET_TOUCHUI_FORCEON;
                        }
                        else
                        {
                            flags |= TABLET_TOUCHUI_FORCEOFF;
                        }

                        if (!isFlicksEnabled)
                        {
                            flags |= TABLET_FLICKS_DISABLED;
                        }

                        result = new IntPtr(flags);
                    }
                    break;

                case WindowMessage.WM_TABLET_FLICK:
                    handled = true;

                    int flickData = NativeMethods.IntPtrToInt32(wParam);

                    // We always handle any scroll actions if we are enabled.  We do this when we see the SystemGesture Flick come through.
                    // Note: Scrolling happens on window flicked on even if it is not the active window.
                    if(_stylusLogic != null && _stylusLogic.Value.Enabled && (WispLogic.GetFlickAction(flickData) == StylusLogic.FlickAction.Scroll))
                    {
                        result = new IntPtr(0x0001); // tell UIHub the flick has already been handled.
                    }
                    break;
            }

            if (handled && EventTrace.IsEnabled(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientInputMessage, EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info,
                                                    (_source.Value.CompositionTarget != null ? _source.Value.CompositionTarget.Dispatcher.GetHashCode() : 0),
                                                     hwnd.ToInt64(),
                                                     msg,
                                                     (int)wParam,
                                                     (int)lParam);
            }

            return result;
        }

        /////////////////////////////////////////////////////////////////////

        private SecurityCriticalDataClass<WispLogic>         _stylusLogic;
        /// <SecurityNote>
        ///     Critical: This is the HwndSurce object , not ok to expose
        /// </SecurityNote>
        private SecurityCriticalDataClass<HwndSource>        _source;
        /// <SecurityNote>
        ///     This data is critical and should never be exposed
        /// </SecurityNote>
        private SecurityCriticalDataClass<InputProviderSite> _site;
    }
}
