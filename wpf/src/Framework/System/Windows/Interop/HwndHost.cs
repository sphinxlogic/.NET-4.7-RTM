using System;
using System.Diagnostics;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Collections;
using MS.Win32;
using MS.Internal;
using MS.Internal.Interop;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Diagnostics.CodeAnalysis;

// Disable pragma warnings to enable PREsharp pragmas
#pragma warning disable 1634, 1691

namespace System.Windows.Interop
{
    /// <summary>
    ///     The HwndHost class hosts an HWND inside of an Avalon tree.
    /// </summary>
    ///<remarks> Subclassing requires unmanaged code permission</remarks>
    ///<SecurityNote>
    ///
    ///     Inheritance demand put in place - as to host activeX controls you should have Unmanaged code permission.
    ///
    ///</SecurityNote>

    [SecurityPermission(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
    public abstract class HwndHost : FrameworkElement, IDisposable, IWin32Window, IKeyboardInputSink
    {
        static HwndHost()
        {
            FocusableProperty.OverrideMetadata(typeof(HwndHost), new FrameworkPropertyMetadata(true));
            HwndHost.DpiChangedEvent = Window.DpiChangedEvent.AddOwner(typeof(HwndHost));
        }

        /// <summary>
        ///     Constructs an instance of the HwndHost class.
        /// </summary>
        ///<remarks> Not available in Internet zone</remarks>
        ///<SecurityNote>
        ///     Critical - calls critical code, sets critical _fTrusted flag.
        ///     PublicOk - trusted flag is set to false.
        ///</SecurityNote>
        [ SecurityCritical ]
        protected HwndHost()
        {
            Initialize( false ) ;
        }

        ///<SecurityNote>
        ///     Critical sets fTrustedBit.
        ///</SecurityNote>
        [SecurityCritical]
        internal HwndHost(bool fTrusted )
        {
            Initialize( fTrusted ) ;
        }

        /// <summary>
        ///    Because we own an HWND, we implement a finalizer to make sure that we destroy it.
        /// </summary>
        ~HwndHost()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     The Win32 handle of the hosted window.
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This code accesses IsWindow, returns hwndHandle.
        ///     PublicOk: There is a demand
        /// </SecurityNote>
        public IntPtr Handle
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();

                return CriticalHandle;
            }
        }

        /// <summary>
        ///     An event that is notified of all unhandled messages received
        ///     by the hosted window.
        /// </summary>
        /// <SecurityNote>
        ///     Critical : Allows callers to sublc---- win32 HWnds which is considered a privilaged operation
        ///     Safe     : Demands unmanaged code permission.
        ///</SecurityNote>
        public event HwndSourceHook MessageHook
        {
            [SecuritySafeCritical]
            add
            {
                SecurityHelper.DemandUnmanagedCode();

                if(_hooks == null)
                {
                    _hooks = new ArrayList(8);
                }

                _hooks.Add(value);
            }

            [SecuritySafeCritical]
            remove
            {
                SecurityHelper.DemandUnmanagedCode();

                if(_hooks != null)
                {
                    _hooks.Remove(value);

                    if(_hooks.Count == 0)
                    {
                        _hooks = null;
                    }
                }
            }
        }

        /// <summary>
        ///     This event is raised after the DPI of the screen on which the HwndHost is displayed, changes.
        /// </summary>
        public event DpiChangedEventHandler DpiChanged
        {
            add { AddHandler(HwndHost.DpiChangedEvent, value); }
            remove { RemoveHandler(HwndHost.DpiChangedEvent, value); }
        }

        /// <summary>
        /// RoutedEvent for when DPI of the screen the HwndHost is on, changes.
        /// </summary>
        public static readonly RoutedEvent DpiChangedEvent;

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        ///<SecurityNote>
        /// Critical - Calls ComponentDispatcher.UnsecureCurrentKeyboardMessage.
        /// TreatAsSafe - Only calls for trusted controls
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        protected override void OnKeyUp(KeyEventArgs e)
        {
            MSG msg;
            if (_fTrusted.Value)
            {
                msg = ComponentDispatcher.UnsecureCurrentKeyboardMessage;
            }
            else
            {
                msg = ComponentDispatcher.CurrentKeyboardMessage;
            }

            ModifierKeys modifiers = HwndKeyboardInputProvider.GetSystemModifierKeys();

            bool handled = ((IKeyboardInputSink)this).TranslateAccelerator(ref msg, modifiers);

            if(handled)
                e.Handled = handled;

            base.OnKeyUp(e);
        }

        /// <summary>
        /// OnDpiChanged is called when the DPI at which this HwndHost is rendered, changes.
        /// </summary>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            RaiseEvent(new DpiChangedEventArgs(oldDpi, newDpi, HwndHost.DpiChangedEvent, this));
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        ///<SecurityNote>
        /// Critical - Calls ComponentDispatcher.UnsecureCurrentKeyboardMessage.
        /// TreatAsSafe - Only calls for trusted controls
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        protected override void OnKeyDown(KeyEventArgs e)
        {
            MSG msg;
            if (_fTrusted.Value)
            {
                msg = ComponentDispatcher.UnsecureCurrentKeyboardMessage;
            }
            else
            {
                msg = ComponentDispatcher.CurrentKeyboardMessage;
            }


            ModifierKeys modifiers = HwndKeyboardInputProvider.GetSystemModifierKeys();

            bool handled = ((IKeyboardInputSink)this).TranslateAccelerator(ref msg, modifiers);

            if(handled)
                e.Handled = handled;

            base.OnKeyDown(e);
        }


#region IKeyboardInputSink

        // General security note on the implementation pattern of this interface. In Dev10 it was chosen
        // to expose the interface implementation for overriding to customers. We did so by keeping the
        // explicit interface implementations (that do have the property of being hidden from the public
        // contract, which limits IntelliSense on derived types like WebBrowser) while sticking protected
        // virtuals next to them. Those virtuals contain our base implementation, while the explicit
        // interface implementation methods do call trivially into the virtuals.
        //
        // This comment outlines the security rationale applied to those methods.
        //
        // <SecurityNote Name="IKeyboardInputSink_Implementation">
        //     The security attributes on the virtual methods within this region mirror the corresponding
        //     IKeyboardInputSink methods; customers can override those methods, so we insert a LinkDemand
        //     to encourage them to have a LinkDemand too (via FxCop).
        // </SecurityNote>

        /// <summary>
        ///     Registers a IKeyboardInputSink with the HwndSource in order
        ///     to retreive a unique IKeyboardInputSite for it.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This API can be used for input spoofing.
        ///     PublicOK: This method has a demand on it.
        ///
        ///     The security attributes on this method mirror the corresponding IKeyboardInputSink method;
        ///     customers can override this method, so we insert a LinkDemand.
        /// </SecurityNote>
        [SecurityCritical, UIPermissionAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        protected virtual IKeyboardInputSite RegisterKeyboardInputSinkCore(IKeyboardInputSink sink)
        {
            throw new InvalidOperationException(SR.Get(SRID.HwndHostDoesNotSupportChildKeyboardSinks));
        }

        /// <SecurityNote>
        ///     Critical: Calls a method with a LinkDemand on it.
        ///     PublicOK: The interface declaration for this method has a demand on it.
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        [SecurityCritical]
        IKeyboardInputSite IKeyboardInputSink.RegisterKeyboardInputSink(IKeyboardInputSink sink)
        {
            return RegisterKeyboardInputSinkCore(sink);
        }

        /// <summary>
        ///     Gives the component a chance to process keyboard input.
        ///     Return value is true if handled, false if not.  Components
        ///     will generally call a child component's TranslateAccelerator
        ///     if they can't handle the input themselves.  The message must
        ///     either be WM_KEYDOWN or WM_SYSKEYDOWN.  It is illegal to
        ///     modify the MSG structure, it's passed by reference only as
        ///     a performance optimization.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This API can be used for input spoofing.
        ///     PublicOK: This method has a demand on it.
        ///
        ///     The security attributes on this method mirror the corresponding IKeyboardInputSink method;
        ///     customers can override this method, so we insert a LinkDemand.
        /// </SecurityNote>
        [SecurityCritical, UIPermissionAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        protected virtual bool TranslateAcceleratorCore(ref MSG msg, ModifierKeys modifiers)
        {
            return false;
        }

        /// <SecurityNote>
        ///     Critical: Calls a method with a LinkDemand on it.
        ///     PublicOk: The interface declaration for this method has a demand on it.
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        [SecurityCritical]
        bool IKeyboardInputSink.TranslateAccelerator(ref MSG msg, ModifierKeys modifiers)
        {
            return TranslateAcceleratorCore(ref msg, modifiers);
        }

        /// <summary>
        ///     Set focus to the first or last tab stop (according to the
        ///     TraversalRequest).  If it can't, because it has no tab stops,
        ///     the return value is false.
        /// </summary>
        protected virtual bool TabIntoCore(TraversalRequest request)
        {
            return false;
        }

        bool IKeyboardInputSink.TabInto(TraversalRequest request)
        {
            return TabIntoCore(request);
        }

        /// <summary>
        ///     The property should start with a null value.  The component's
        ///     container will set this property to a non-null value before
        ///     any other methods are called.  It may be set multiple times,
        ///     and should be set to null before disposal.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This API can be used for input spoofing
        ///     PublicOK: The interface declaration for this method has a demand on it.
        /// </SecurityNote>
        IKeyboardInputSite IKeyboardInputSink.KeyboardInputSite { get; [SecurityCritical] set; }

        /// <summary>
        ///     This method is called whenever one of the component's
        ///     mnemonics is invoked.  The message must either be WM_KEYDOWN
        ///     or WM_SYSKEYDOWN.  It's illegal to modify the MSG structrure,
        ///     it's passed by reference only as a performance optimization.
        ///     If this component contains child components, the container
        ///     OnMnemonic will need to call the child's OnMnemonic method.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This API can be used for input spoofing.
        ///     PublicOK: This method has a demand on it.
        ///
        ///     The security attributes on this method mirror the corresponding IKeyboardInputSink method;
        ///     customers can override this method, so we insert a LinkDemand.
        /// </SecurityNote>
        [SecurityCritical, UIPermissionAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        protected virtual bool OnMnemonicCore(ref MSG msg, ModifierKeys modifiers)
        {
            return false;
        }

        /// <SecurityNote>
        ///     Critical: Calls a method with a LinkDemand on it.
        ///     PublicOK: The interface declaration for this method has a demand on it.
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        [SecurityCritical]
        bool IKeyboardInputSink.OnMnemonic(ref MSG msg, ModifierKeys modifiers)
        {
            return OnMnemonicCore(ref msg, modifiers);
        }

        /// <summary>
        ///     Gives the component a chance to process keyboard input messages
        ///     WM_CHAR, WM_SYSCHAR, WM_DEADCHAR or WM_SYSDEADCHAR before calling OnMnemonic.
        ///     Will return true if "handled" meaning don't pass it to OnMnemonic.
        ///     The message must be WM_CHAR, WM_SYSCHAR, WM_DEADCHAR or WM_SYSDEADCHAR.
        ///     It is illegal to modify the MSG structure, it's passed by reference
        ///     only as a performance optimization.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This API can be used for input spoofing.
        ///     PublicOK: This method has a demand on it.
        ///
        ///     The security attributes on this method mirror the corresponding IKeyboardInputSink method;
        ///     customers can override this method, so we insert a LinkDemand.
        /// </SecurityNote>
        [SecurityCritical, UIPermissionAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        protected virtual bool TranslateCharCore(ref MSG msg, ModifierKeys modifiers)
        {
            return false;
        }

        /// <SecurityNote>
        ///     Critical: Calls a method with a LinkDemand on it.
        ///     PublicOK: The interface declaration for this method has a demand on it.
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        [SecurityCritical]
        bool IKeyboardInputSink.TranslateChar(ref MSG msg, ModifierKeys modifiers)
        {
            return TranslateCharCore(ref msg, modifiers);
        }

        /// <summary>
        ///     This returns true if the sink, or a child of it, has focus. And false otherwise.
        /// </summary>
        ///<SecurityNote>
        ///     Critical: Calls a method with a SUC - GetFocus.
        ///     TreatAsSafe: No critical information exposed.
        ///                  It's ok to return whether hwndHost has focus within itself in PT.
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        protected virtual bool HasFocusWithinCore()
        {
            HandleRef hwndFocus = new HandleRef(this, UnsafeNativeMethods.GetFocus());
            if (_hwnd.Handle != IntPtr.Zero && (hwndFocus.Handle == _hwnd.Handle || UnsafeNativeMethods.IsChild(_hwnd, hwndFocus)))
            {
                return true;
            }
            return false;
        }

        /// <SecurityNote>
        ///     PublicOK: It's ok to return whether hwndHost has focus within itself in PT.
        /// </SecurityNote>
        bool IKeyboardInputSink.HasFocusWithin()
        {
            return HasFocusWithinCore();
        }

#endregion IKeyboardInputSink

        /// <summary>
        ///     Updates the child window to reflect the state of this element.
        /// </summary>
        /// <remarks>
        ///     This includes the size of the window, the position of the
        ///     window, and the visibility of the window.
        /// </remarks>
        ///<remarks> Not available in Internet zone</remarks>
        ///<SecurityNote>
        ///     Critical: This code accesses critical code and also calls into PresentationSource
        ///     PublicOk : Repositioning the activeX control is ok.
        ///                Net effect is to make window location consistent with what layout calculated.
        ///</SecurityNote>
        [SecurityCritical]
        public void UpdateWindowPos()
        {
            // Verify the thread has access to the context.
            // VerifyAccess();

            if (_isDisposed)
            {
                return;
            }

            // Position the child HWND where layout put it.  To do this we
            // have to get coordinates relative to the parent window.

            PresentationSource source = null;
            CompositionTarget vt = null;
            if (( CriticalHandle != IntPtr.Zero) && IsVisible)
            {
                source = PresentationSource.CriticalFromVisual(this, false /* enable2DTo3DTransition */);
                if(source != null)
                {
                    vt = source.CompositionTarget;
                }
            }

            if(vt != null && vt.RootVisual != null)
            {
                // Translate the layout information assigned to us from the co-ordinate
                // space of this element, through the root visual, to the Win32 client
                // co-ordinate space
                NativeMethods.RECT rcClientRTLAdjusted = CalculateAssignedRC(source);

                // Set the Win32 position for the child window.
                //
                // Note, we can't check the existing position because we use
                // SWP_ASYNCWINDOWPOS, which means we could have pending position
                // change requests that haven't been applied yet.  If we need
                // this functionality (to avoid the extra SetWindowPos calls),
                // we'll have to track the last RECT we sent Win32 ourselves.
                //
                Rect rectClientRTLAdjusted = PointUtil.ToRect(rcClientRTLAdjusted);
                OnWindowPositionChanged(rectClientRTLAdjusted);

                // Show the window
                // Based on Dwayne, the reason we also show/hide window in UpdateWindowPos is for the 
                // following kind of scenario: When applying RenderTransform to HwndHost, the hwnd
                // will be left behind. Developer can workaround by hide the hwnd first using pinvoke. 
                // After the RenderTransform is applied to the HwndHost, call UpdateWindowPos to sync up
                // the hwnd's location, size and visibility with WPF.
                UnsafeNativeMethods.ShowWindowAsync(_hwnd, NativeMethods.SW_SHOW);
            }
            else
            {
                // For some reason we shouldn't be displayed: either we don't
                // have a parent, or the parent no longer has a root visual,
                // or we are marked as not being visible.
                //
                // Just hide the window to get it out of the way.
                UnsafeNativeMethods.ShowWindowAsync(_hwnd, NativeMethods.SW_HIDE);
            }
        }

        // Translate the layout information assigned to us from the co-ordinate
        // space of this element, through the root visual, to the Win32 client
        // co-ordinate space
        ///<SecurityNote>
        ///     Critical: This code accesses critical code and also calls into PresentationSource
        ///     TAS : Calculate the new position of the activeX control is ok.
        ///                Net effect is to make window location consistent with what layout calculated.
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private NativeMethods.RECT CalculateAssignedRC(PresentationSource source)
        {
            Rect rectElement = new Rect(RenderSize);
            Rect rectRoot = PointUtil.ElementToRoot(rectElement, this, source);
            Rect rectClient = PointUtil.RootToClient(rectRoot, source);

            // Adjust for Right-To-Left oriented windows
            IntPtr hwndParent = UnsafeNativeMethods.GetParent(_hwnd);
            NativeMethods.RECT rcClient = PointUtil.FromRect(rectClient);
            NativeMethods.RECT rcClientRTLAdjusted = PointUtil.AdjustForRightToLeft(rcClient, new HandleRef(null, hwndParent));

            return rcClientRTLAdjusted;
        }

        /// <summary>
        ///     Disposes this object.
        /// </summary>
        /// <param name="disposing">
        ///     true if called from explisit Dispose; and we free all objects managed and un-managed.
        ///     false if called from the finalizer; and we free only un-managed objects.
        /// </param>
        /// <remarks>
        ///     Derived classes should override this if they have additional
        ///     cleanup to do.  The base class implementation should be called.
        ///     Note that the calling thread must be the dispatcher thread.
        ///     If a window is being hosted, that window is destroyed.
        /// </remarks>
        ///<SecurityNote>
        ///     Critical - call to RemoveSourceChangeHandler invokes critical delegate.
        ///     SecurityTreatAsSafe : - disposing the HwndHost is considered ok.
        ///</SecurityNote>
        [ SecurityCritical, SecurityTreatAsSafe ]
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed == true)
            {
                return;
            }


            if(disposing)
            {
                // Verify the thread has access to the context.
#pragma warning suppress 6519
                 VerifyAccess();


                // Remove our subclass.  Even if this fails, it will be forcably removed
                // when the window is destroyed.
                if (_hwndSubclass != null)
                {
                    // Check if it is trusted (WebOC and AddInHost), call CriticalDetach to avoid the Demand.
                    if (_fTrusted.Value == true)
                    {
                        _hwndSubclass.CriticalDetach(false);
                    }
                    else
                    {
                        _hwndSubclass.RequestDetach(false);
                    }

                    _hwndSubclass = null;
                }

                // Drop the hooks so that they can be garbage-collected.
                _hooks = null;

                // We no longer need to know about the source changing.
                PresentationSource.RemoveSourceChangedHandler(this, new SourceChangedEventHandler(OnSourceChanged));
            }

            if (_weakEventDispatcherShutdown != null) // Can be null if the static ctor failed ... see WebBrowser.
            {
                _weakEventDispatcherShutdown.Dispose();
                _weakEventDispatcherShutdown = null;
            }

            DestroyWindow();

            _isDisposed = true;
        }

        private void OnDispatcherShutdown(object sender, EventArgs e)
        {
            Dispose();
        }

        /// <summary>
        ///     Derived classes override this method to actually build the
        ///     window being hosted.
        /// </summary>
        /// <param name="hwndParent">
        ///     The parent HWND for the child window.
        /// </param>
        /// <returns>
        ///     The HWND handle to the child window that was created.
        /// </returns>
        /// <remarks>
        ///     The window that is returned must be a child window of the
        ///     specified parent window.
        ///     <para/>
        ///     In addition, the child window will only be subclassed if
        ///     the window is owned by the calling thread.
        /// </remarks>
        protected abstract HandleRef BuildWindowCore(HandleRef hwndParent);

        /// <summary>
        ///     Derived classes override this method to destroy the
        ///     window being hosted.
        /// </summary>
        protected abstract void DestroyWindowCore(HandleRef hwnd);

        /// <summary>
        ///     A protected override for accessing the window proc of the
        ///     hosted child window.
        /// </summary>
        ///<remarks> Not available in Internet zone</remarks>
        ///<SecurityNote>
        ///     Critical - accesses _hwnd Critical member.
        ///     PublicOk - Inheritance Demand for unmanaged codepermission to buslclass.
        ///                any caller of the protected must have Unmanaged code permission.
        ///     DemandIfUntrusted is there as a defense in depth measure.
        /// </SecurityNote>
        [ SecurityCritical ]
        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            DemandIfUntrusted();

            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_NCDESTROY:
                    _hwnd = new HandleRef(null, IntPtr.Zero);
                    break;

                // When layout happens, we first calculate the right size/location then call SetWindowPos.
                // We only allow the changes that are coming from Avalon layout. The hwnd is not allowed to change by itself.
                // So the size of the hwnd should always be RenderSize and the position be where layout puts it.
                case WindowMessage.WM_WINDOWPOSCHANGING:
                    PresentationSource source = PresentationSource.CriticalFromVisual(this, false /* enable2DTo3DTransition */);

                    if (source != null)
                    {
                        // Get the rect assigned by layout to us.
                        NativeMethods.RECT assignedRC = CalculateAssignedRC(source);

                        // The lParam is a pointer to a WINDOWPOS structure
                        // that contains information about the size and
                        // position that the window is changing to.  Note that
                        // modifying this structure during WM_WINDOWPOSCHANGING
                        // will change what happens to the window.
                        unsafe
                        {
                            NativeMethods.WINDOWPOS * windowPos = (NativeMethods.WINDOWPOS *)lParam;

                            // Always force the size of the window to be the
                            // size of our assigned rectangle.  Note that we
                            // have to always clear the SWP_NOSIZE flag.
                            windowPos->cx = assignedRC.right - assignedRC.left;
                            windowPos->cy = assignedRC.bottom - assignedRC.top;
                            windowPos->flags &= ~NativeMethods.SWP_NOSIZE;

                            // Always force the position of the window to be
                            // the upper-left corner of our assigned rectangle.
                            // Note that we have to always clear the
                            // SWP_NOMOVE flag.
                            windowPos->x = assignedRC.left;
                            windowPos->y = assignedRC.top;
                            windowPos->flags &= ~NativeMethods.SWP_NOMOVE;

                            // Windows has an optimization to copy pixels
                            // around to reduce the amount of repainting
                            // needed when moving or resizing a window.
                            // Unfortunately, this is not compatible with WPF
                            // in many cases due to our use of DirectX for
                            // rendering from our rendering thread.
                            // To be safe, we disable this optimization and
                            // pay the cost of repainting.
                            windowPos->flags |= NativeMethods.SWP_NOCOPYBITS;
                        }
                    }

                    break;


                case WindowMessage.WM_GETOBJECT:
                    handled = true;
                    return OnWmGetObject(wParam, lParam);
            }

            return IntPtr.Zero ;
        }

        #region Automation

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new HwndHostAutomationPeer(this);
        }

        /// <SecurityNote>
        ///     Critical    - Calls critical HwndHost.CriticalHandle.
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr OnWmGetObject(IntPtr wparam, IntPtr lparam)
        {
            IntPtr result = IntPtr.Zero;

            AutomationPeer containerPeer = UIElementAutomationPeer.CreatePeerForElement(this);
            if (containerPeer != null)
            {
                // get the element proxy
                IRawElementProviderSimple el = containerPeer.GetInteropChild();
                result = AutomationInteropProvider.ReturnRawElementProvider(CriticalHandle, wparam, lparam, el);
            }
            return result;
        }

        #endregion Automation

        //











        [ SecurityCritical ]
        protected virtual void OnWindowPositionChanged(Rect rcBoundingBox)
        {
            if (_isDisposed)
            {
                return;
            }

            UnsafeNativeMethods.SetWindowPos(_hwnd,
                                           new HandleRef(null, IntPtr.Zero),
                                           (int)rcBoundingBox.X,
                                           (int)rcBoundingBox.Y,
                                           (int)rcBoundingBox.Width,
                                           (int)rcBoundingBox.Height,
                                           NativeMethods.SWP_ASYNCWINDOWPOS
                                           | NativeMethods.SWP_NOZORDER
                                           | NativeMethods.SWP_NOCOPYBITS
                                           | NativeMethods.SWP_NOACTIVATE);
        }

        /// <summary>
        ///     Return the desired size of the HWND.
        /// </summary>
        /// <remarks>
        ///     HWNDs usually expect a very simplisitic layout model where
        ///     a window gets to be whatever size it wants to be.  To respect
        ///     this we request the initial size that the window was created
        ///     at.  A window created with a 0 dimension will adopt whatever
        ///     size the containing layout wants it to be.  Layouts are free
        ///     to actually size the window to whatever they want, and the
        ///     child window will always be sized accordingly.
        ///     <para/>
        ///     Derived classes should only override this method if they
        ///     have special knowlege about the size the window wants to be.
        ///     Examples of such may be special HWND types like combo boxes.
        ///     In such cases, the base class must still be called, but the
        ///     return value can be changed appropriately.
        /// </remarks>
        ///<remarks> Not available in Internet zone</remarks>
        ///<SecurityNote>
        ///     Critical - calls CriticalHandle
        ///     TreatAsSafe - CriticalHandle used for a null check, not leaked out.
        ///                   Ok to Override MeasureOverride and return a size in PT.
        /// Demand put in place as a defense in depth measure.
        ///
        ///</SecurityNote>
        [ SecurityCritical, SecurityTreatAsSafe ]
        protected override Size MeasureOverride(Size constraint)
        {
            DemandIfUntrusted();

            Size desiredSize = new Size(0,0);

            // Measure to our desired size.  If we have a 0-length dimension,
            // the system will assume we don't care about that dimension.
            if(CriticalHandle != IntPtr.Zero)
            {
                desiredSize.Width = Math.Min(_desiredSize.Width, constraint.Width);
                desiredSize.Height = Math.Min(_desiredSize.Height, constraint.Height);
            }

            return desiredSize;
        }

        /// <summary>
        ///     GetDrawing - Returns the drawing content of this Visual.
        /// </summary>
        /// <remarks>
        ///     This returns a bitmap obtained by calling the PrintWindow Win32 API.
        /// </remarks>
        internal override DrawingGroup GetDrawing()
        {
            return GetDrawingHelper();
        }

        /// <summary>
        /// Returns the bounding box of the content.
        /// </summary>
        internal override Rect GetContentBounds()
        {
            return new Rect(RenderSize);
        }

        ///<SecurityNote>
        ///     Critical - calls many native methods, accesses critical data
        ///     TreatAsSafe - Demands UIWindow permission before giving out a bitmap of this window.
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private DrawingGroup GetDrawingHelper()
        {
            // Printing an HWND requires UIPermissionWindow.AllWindows to give out its pixels.
            SecurityHelper.DemandUIWindowPermission();

            DrawingGroup drawingGroup = null;

            if(_hwnd.Handle != IntPtr.Zero && UnsafeNativeMethods.IsWindow(_hwnd))
            {
                NativeMethods.RECT rc = new NativeMethods.RECT();
                SafeNativeMethods.GetWindowRect(_hwnd, ref rc);
                int width = rc.right - rc.left;
                int height = rc.bottom - rc.top;

                HandleRef hdcScreen = new HandleRef(this, UnsafeNativeMethods.GetDC(new HandleRef(this, IntPtr.Zero)));
                if(hdcScreen.Handle != IntPtr.Zero)
                {
                    HandleRef hdcBitmap = new HandleRef(this, IntPtr.Zero);
                    HandleRef hBitmap = new HandleRef(this, IntPtr.Zero);

                    try
                    {
                        hdcBitmap = new HandleRef(this, UnsafeNativeMethods.CriticalCreateCompatibleDC(hdcScreen));
                        if(hdcBitmap.Handle != IntPtr.Zero)
                        {
                            hBitmap = new HandleRef(this, UnsafeNativeMethods.CriticalCreateCompatibleBitmap(hdcScreen, width, height));

                            if(hBitmap.Handle != IntPtr.Zero)
                            {
                                // Select the bitmap into the DC so that we draw to it.
                                IntPtr hOldBitmap = UnsafeNativeMethods.CriticalSelectObject(hdcBitmap, hBitmap.Handle);
                                try
                                {
                                    // Clear the bitmap to white (so we don't waste toner printing a black bitmap something fails).
                                    NativeMethods.RECT rcPaint = new NativeMethods.RECT(0,0,width, height);
                                    IntPtr hbrWhite = UnsafeNativeMethods.CriticalGetStockObject(NativeMethods.WHITE_BRUSH);
                                    UnsafeNativeMethods.CriticalFillRect(hdcBitmap.Handle, ref rcPaint, hbrWhite);

                                    // First try to use the PrintWindow API.
                                    bool result = UnsafeNativeMethods.CriticalPrintWindow(_hwnd, hdcBitmap, 0);
                                    if(result == false)
                                    {
                                        // Fall back to sending a WM_PRINT message to the window.
                                        //
                                        // Note: there are known cases where WM_PRINT is not implemented
                                        // to provide visual parity with WM_PAINT.  However, since the
                                        // GetDrawing method is virtual, the derived class can override
                                        // this default implementation and provide a better implementation.
                                        UnsafeNativeMethods.SendMessage(_hwnd.Handle, WindowMessage.WM_PRINT, hdcBitmap.Handle, (IntPtr) (NativeMethods.PRF_CHILDREN | NativeMethods.PRF_CLIENT | NativeMethods.PRF_ERASEBKGND | NativeMethods.PRF_NONCLIENT));
                                    }
                                    else
                                    {
                                        // There is a know issue where calling PrintWindow on a window will
                                        // clear all dirty regions (but since it is redirected, the screen
                                        // won't be updated).  As a result we can leave unpainted pixels on
                                        // the screen if PrintWindow is called when the window was dirty.
                                        //
                                        // To fix this, we just force the child window to repaint.
                                        //
                                        UnsafeNativeMethods.CriticalRedrawWindow(_hwnd, IntPtr.Zero, IntPtr.Zero, NativeMethods.RDW_INVALIDATE | NativeMethods.RDW_ALLCHILDREN);
                                    }

                                    // Create a DrawingGroup that only contains an ImageDrawing that wraps the bitmap.
                                    drawingGroup = new DrawingGroup();
                                    System.Windows.Media.Imaging.BitmapSource bitmapSource = Imaging.CriticalCreateBitmapSourceFromHBitmap(hBitmap.Handle, IntPtr.Zero, Int32Rect.Empty, null, WICBitmapAlphaChannelOption.WICBitmapIgnoreAlpha);
                                    Rect rectElement    = new Rect(RenderSize);
                                    drawingGroup.Children.Add(new ImageDrawing(bitmapSource, rectElement));
                                    drawingGroup.Freeze();
                                }
                                finally
                                {
                                    // Put the old bitmap back into the DC.
                                    UnsafeNativeMethods.CriticalSelectObject(hdcBitmap, hOldBitmap);
                                }
                            }
                        }
                    }
                    finally
                    {
                        UnsafeNativeMethods.ReleaseDC(new HandleRef(this, IntPtr.Zero), hdcScreen);
                        hdcScreen = new HandleRef(null, IntPtr.Zero);

                        if(hBitmap.Handle != IntPtr.Zero)
                        {
                            UnsafeNativeMethods.DeleteObject(hBitmap);
                            hBitmap = new HandleRef(this, IntPtr.Zero);
                        }

                        if(hdcBitmap.Handle != IntPtr.Zero)
                        {
                            UnsafeNativeMethods.CriticalDeleteDC(hdcBitmap);
                            hdcBitmap = new HandleRef(this, IntPtr.Zero);
                        }
                    }
                }
            }

            return drawingGroup;
        }

        ///<SecurityNote>
        ///     Critical - calls a method that linkdemands.
        ///                creates critical for set data.
        ///</SecurityNote>
        [ SecurityCritical ]
        private void Initialize( bool fTrusted )
        {
            _fTrusted = new SecurityCriticalDataForSet<bool> ( fTrusted ) ;

            _hwndSubclassHook = new HwndWrapperHook(SubclassWndProc);
            _handlerLayoutUpdated = new EventHandler(OnLayoutUpdated);
            _handlerEnabledChanged = new DependencyPropertyChangedEventHandler(OnEnabledChanged);
            _handlerVisibleChanged = new DependencyPropertyChangedEventHandler(OnVisibleChanged);
            PresentationSource.AddSourceChangedHandler(this, new SourceChangedEventHandler(OnSourceChanged));

            _weakEventDispatcherShutdown = new WeakEventDispatcherShutdown(this, this.Dispatcher);
        }

        ///<summary>
        ///     Use this method as a defense-in-depth measure only.
        ///</summary>
        ///<SecurityNote>
        ///     Critical : Demands which is critical
        ///</SecurityNote>
        [SecurityCritical]
        private void DemandIfUntrusted()
        {
            if ( ! _fTrusted.Value )
            {
                SecurityHelper.DemandUnmanagedCode();
            }
        }

        ///<SecurityNote>
        /// Critical - calls CriticalFromVisual.
        /// TreatAsSafe - does not expose the HwndSource obtained.
        ///</SecurityNote>
        [SecurityCritical , SecurityTreatAsSafe ]
        private void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            // Remove ourselves as an IKeyboardInputSinks child of our previous
            // containing window.
            IKeyboardInputSite keyboardInputSite = ((IKeyboardInputSink)this).KeyboardInputSite;
            if (keyboardInputSite != null)
            {
                if (_fTrusted.Value == true)
                {
                    new UIPermission(PermissionState.Unrestricted).Assert(); //BlessedAssert:
                }

                try
                {
                    // Derived classes that implement IKeyboardInputSink should support setting it to null.
                    ((IKeyboardInputSink)this).KeyboardInputSite = null;

                    keyboardInputSite.Unregister();
                }
                finally
                {
                    if (_fTrusted.Value == true)
                    {
                        CodeAccessPermission.RevertAssert();
                    }
                }
            }

            // Add ourselves as an IKeyboardInputSinks child of our containing window.
            IKeyboardInputSink source = PresentationSource.CriticalFromVisual(this, false /* enable2DTo3DTransition */) as IKeyboardInputSink;
            if(source != null)
            {
                if (_fTrusted.Value == true)
                {
                    new UIPermission(PermissionState.Unrestricted).Assert(); //BlessedAssert:
                }

                try
                {
                    ((IKeyboardInputSink)this).KeyboardInputSite = source.RegisterKeyboardInputSink(this);
                }
                finally
                {
                    if (_fTrusted.Value == true)
                    {
                        CodeAccessPermission.RevertAssert();
                    }
                }
            }

            BuildOrReparentWindow();
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            UpdateWindowPos();
        }

        /// <SecurityNote>
        ///     Critical: This code calls into critical method EnableWindow
        ///     TreatAsSafe: Changing window to being enabled is safe.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void OnEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            bool boolNewValue = (bool)e.NewValue;
            UnsafeNativeMethods.EnableWindow(_hwnd, boolNewValue);
        }

        /// <SecurityNote>
        ///     Critical: This code calls into critical method show window
        ///     TreatAsSafe: Changing window visibility is safe.
        ///                  Window is always a child window.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            bool vis = (bool)e.NewValue;

            // 





            if(vis)
                UnsafeNativeMethods.ShowWindowAsync(_hwnd, NativeMethods.SW_SHOWNA);
            else
                UnsafeNativeMethods.ShowWindowAsync(_hwnd, NativeMethods.SW_HIDE);
        }

        // This routine handles the following cases:
        // 1) a parent window is present, build the child window
        // 2) a parent is present, reparent the child window to it
        // 3) a parent window is not present, hide the child window by parenting it to SystemResources.Hwnd window.
        /// <SecurityNote>
        /// Critical - calls Critical GetParent
        /// TreatAsSafe - it doesn't disclose GetParent returned information. also
        ///               as a defense in depth measure - we demand if not trusted. Setting trusted is critical
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void BuildOrReparentWindow()
        {
            DemandIfUntrusted();

            // Verify the thread has access to the context.
            // VerifyAccess();

            // Prevent reentry while building a child window,
            // also prevent the reconstruction of Disposed objects.
            if(_isBuildingWindow || _isDisposed)
            {
                return;
            }

            _isBuildingWindow = true;

            // Find the source window, this must be the parent window of
            // the child window.
            IntPtr hwndParent = IntPtr.Zero;
            PresentationSource source = PresentationSource.CriticalFromVisual(this, false /* enable2DTo3DTransition */);
            if(source != null)
            {
                HwndSource hwndSource = source as HwndSource ;
                if(hwndSource != null)
                {
                    hwndParent = hwndSource.CriticalHandle;
                }
            }
            else
            {
                // attempt to also walk through 3D - if we get a non-null result then we know we are inside of
                // a 3D scene which is not supported
                PresentationSource goingThrough3DSource = PresentationSource.CriticalFromVisual(this, true /* enable2DTo3DTransition */);
                if (goingThrough3DSource != null)
                {
                    if (TraceHwndHost.IsEnabled)
                    {
                        TraceHwndHost.Trace(TraceEventType.Warning, TraceHwndHost.HwndHostIn3D);
                    }
                }
            }

            try
            {
                if(hwndParent != IntPtr.Zero)
                {
                    if(_hwnd.Handle == IntPtr.Zero)
                    {
                        // We now have a parent window, so we can create the child
                        // window.
                        BuildWindow(new HandleRef(null, hwndParent));
                        this.LayoutUpdated += _handlerLayoutUpdated;
                        this.IsEnabledChanged += _handlerEnabledChanged;
                        this.IsVisibleChanged += _handlerVisibleChanged;
                    }
                    else if(hwndParent != UnsafeNativeMethods.GetParent(_hwnd))
                    {
                        // We have a different parent window.  Just reparent the
                        // child window under the new parent window.
                        UnsafeNativeMethods.SetParent(_hwnd, new HandleRef(null,hwndParent));
                    }
                }
                else
                {
                    // Reparent the window to the SystemResources.Hwnd
                    // window.  This keeps the child window around, but it is not
                    // visible.  We can reparent the window later when a new
                    // parent is available
                    UnsafeNativeMethods.SetParent(_hwnd, new HandleRef(null, SystemResources.Hwnd.Handle));
                    // ...But we have a potential problem: If the SystemResources listener window gets 
                    // destroyed ahead of the call to HwndHost.OnDispatcherShutdown(), the HwndHost's window
                    // will be destroyed too, before the "logical" Dispose has had a chance to do proper
                    // shutdown. This turns out to be very significant for WebBrowser/ActiveXHost, which shuts
                    // down the hosted control through the COM interfaces, and the control destroys its
                    // window internally. Evidently, the WebOC fails to do full, proper cleanup if its
                    // window is destroyed unexpectedly.
                    // To avoid this situation, we make sure SystemResources responds to the Dispatcher 
                    // shutdown event after this HwndHost.
                    SystemResources.DelayHwndShutdown();
                }
            }
            finally
            {
                // Be careful to clear our guard bit.
                _isBuildingWindow = false;
            }
        }


        /// <SecurityNote>
        /// Critical: Calls critical methods - eg. GetWindowLong, IsParent, GetParent etc.
        /// TreatAsSafe: We demand if the trusted bit is not set.
        ///                      Setting the trusted bit is criical.
        /// </SecurityNote>
        [ SecurityCritical, SecurityTreatAsSafe ]
        private void BuildWindow(HandleRef hwndParent)
        {
            // Demand unmanaged code to the caller. IT'S RISKY TO REMOVE THIS
            DemandIfUntrusted();

            // Allow the derived class to build our HWND.
            _hwnd = BuildWindowCore(hwndParent);

            if(_hwnd.Handle == IntPtr.Zero || !UnsafeNativeMethods.IsWindow(_hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.ChildWindowNotCreated));
            }

            // Make sure that the window that was created is indeed a child window.
            int windowStyle = UnsafeNativeMethods.GetWindowLong(new HandleRef(this,_hwnd.Handle), NativeMethods.GWL_STYLE);
            if((windowStyle & NativeMethods.WS_CHILD) == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.HostedWindowMustBeAChildWindow));
            }

            // Make sure the child window is the child of the expected parent window.
            if(hwndParent.Handle != UnsafeNativeMethods.GetParent(_hwnd))
            {
                throw new InvalidOperationException(SR.Get(SRID.ChildWindowMustHaveCorrectParent));
            }

            // Only subclass the child HWND if it is owned by our thread.
            int idWindowProcess;
            int idWindowThread = UnsafeNativeMethods.GetWindowThreadProcessId(_hwnd, out idWindowProcess);

#if WCP_SERVER2003_OR_LATER_ENABLED
            IntPtr hCurrentThread = UnsafeNativeMethods.GetCurrentThread();
            if ((idWindowThread == SafeNativeMethods.GetThreadId(hCurrentThread)) &&
                (idWindowProcess == UnsafeNativeMethods.GetProcessIdOfThread(hCurrentThread)))
#else
            if ((idWindowThread == SafeNativeMethods.GetCurrentThreadId()) &&
                (idWindowProcess == SafeNativeMethods.GetCurrentProcessId()))
#endif
            {
                _hwndSubclass = new HwndSubclass(_hwndSubclassHook);
                _hwndSubclass.CriticalAttach(_hwnd.Handle);
            }

            // Initially make sure the window is hidden.  We will show it later during rendering.
            UnsafeNativeMethods.ShowWindowAsync(_hwnd, NativeMethods.SW_HIDE);

            // Assume the desired size is the initial size.  If the window was
            // created with a 0-length dimension, we assume this means we
            // should fill all available space.
            NativeMethods.RECT rc = new NativeMethods.RECT();
            SafeNativeMethods.GetWindowRect(_hwnd, ref rc);

            // Convert from pixels to measure units.
            // PresentationSource can't be null if we get here.
            PresentationSource source = PresentationSource.CriticalFromVisual(this, false /* enable2DTo3DTransition */);
            Point ptUpperLeft = new Point(rc.left, rc.top);
            Point ptLowerRight = new Point(rc.right, rc.bottom);
            ptUpperLeft = source.CompositionTarget.TransformFromDevice.Transform(ptUpperLeft);
            ptLowerRight = source.CompositionTarget.TransformFromDevice.Transform(ptLowerRight);
            _desiredSize = new Size(ptLowerRight.X - ptUpperLeft.X, ptLowerRight.Y - ptUpperLeft.Y);

            // We have a new desired size, so invalidate measure.
            InvalidateMeasure();
        }

        /// <SecurityNote>
        ///     Critical - calls CriticalHandle.
        ///     TreatAsSafe - destroying a window previously created considered safe.
        /// </SecurityNote>
        [ SecurityCritical, SecurityTreatAsSafe  ]
        private void DestroyWindow()
        {
            // Destroy the window if we are hosting one.
            if( CriticalHandle == IntPtr.Zero)
                return;

            if(!CheckAccess())
            {
                // I understand we can get in here on the finalizer thread.  And
                // touching other GC'ed objects in the finalizer is typically bad.
                // But a Context object can be accessed after finalization.
                // We need to touch the Context to switch to the right thread.
                // If the Context has been finalized then we won't get switched
                // and that is OK.
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(AsyncDestroyWindow), null);
                return;
            }

            HandleRef hwnd = _hwnd;
            _hwnd = new HandleRef(null, IntPtr.Zero);

            DestroyWindowCore(hwnd);
        }

        private object AsyncDestroyWindow(object arg)
        {
            DestroyWindow();
            return null;
        }

        ///<SecurityNote>
        ///     Critical - returns Handle
        ///</SecurityNote>
        internal IntPtr CriticalHandle
        {
            [SecurityCritical]
            get
            {
                if(_hwnd.Handle != IntPtr.Zero)
                {
                    if(!UnsafeNativeMethods.IsWindow(_hwnd))
                    {
                        _hwnd = new HandleRef(null, IntPtr.Zero);
                    }
                }

                return _hwnd.Handle;
            }
        }

        ///<SecurityNote>
        ///     Critical - can be used to spoof messages.
        ///</SecurityNote>
        [ SecurityCritical ]
        private IntPtr SubclassWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;

            // Call the virtual first.
            result = WndProc(hwnd, msg, wParam, lParam, ref handled);

            // Call the handlers for the MessageHook event.
            if(!handled && _hooks != null)
            {
                for(int i = 0, nCount = _hooks.Count; i < nCount; i++)
                {
                    result = ((HwndSourceHook)_hooks[i])(hwnd, msg, wParam, lParam, ref handled);
                    if(handled)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private DependencyPropertyChangedEventHandler _handlerEnabledChanged;
        private DependencyPropertyChangedEventHandler _handlerVisibleChanged;
        private EventHandler _handlerLayoutUpdated;

        ///<SecurityNote>
        ///     Critical - ctor was critical. Manipulating/Handing this out to PT would be unsafe.
        ///</SecurityNote>
        [ SecurityCritical ]
        private HwndSubclass _hwndSubclass;

        ///<SecurityNote>
        ///     Critical - ctor was critical. Manipulating/Handing this out to PT would be unsafe.
        ///</SecurityNote>
        [ SecurityCritical ]
        private HwndWrapperHook _hwndSubclassHook;

        ///<SecurityNote>
        ///     Critical - ctor was critical. Manipulating/Handing this out to PT would be unsafe.
        ///</SecurityNote>
        [ SecurityCritical ]
        private HandleRef _hwnd;

        private ArrayList _hooks;
        private Size _desiredSize;

        private SecurityCriticalDataForSet<bool> _fTrusted ;

        private bool _isBuildingWindow = false;

        private bool _isDisposed = false;

        private class WeakEventDispatcherShutdown: WeakReference
        {
            public WeakEventDispatcherShutdown(HwndHost hwndHost, Dispatcher that): base(hwndHost)
            {
                _that = that;
                _that.ShutdownFinished += new EventHandler(this.OnShutdownFinished);
            }

            public void OnShutdownFinished(object sender, EventArgs e)
            {
                HwndHost hwndHost = this.Target as HwndHost;
                if(null != hwndHost)
                {
                    hwndHost.OnDispatcherShutdown(sender, e);
                }
                else
                {
                    Dispose();
                }
            }

            public void Dispose()
            {
                if(null != _that)
                {
                    _that.ShutdownFinished-= new EventHandler(this.OnShutdownFinished);
                }
            }

            private Dispatcher _that;
        }
        WeakEventDispatcherShutdown _weakEventDispatcherShutdown;
    }
}
