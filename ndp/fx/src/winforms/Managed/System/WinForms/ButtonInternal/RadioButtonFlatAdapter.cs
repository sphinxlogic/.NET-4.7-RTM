//------------------------------------------------------------------------------
// <copyright file="ButtonBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Windows.Forms.ButtonInternal {
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.Windows.Forms;

    internal class RadioButtonFlatAdapter : RadioButtonBaseAdapter {
        
        protected const int flatCheckSize = 12;
        
        internal RadioButtonFlatAdapter(ButtonBase control) : base(control) {}

        internal override void PaintDown(PaintEventArgs e, CheckState state) {
            if (Control.Appearance == Appearance.Button) {
                ButtonFlatAdapter adapter = new ButtonFlatAdapter(Control);
                adapter.PaintDown(e, Control.Checked ? CheckState.Checked : CheckState.Unchecked);
                return;
            }

            ColorData colors = PaintFlatRender(e.Graphics).Calculate();
            if (Control.Enabled) {
                PaintFlatWorker(e, colors.windowText, colors.highlight, colors.windowFrame, colors);
            }
            else {
                PaintFlatWorker(e, colors.buttonShadow, colors.buttonFace, colors.buttonShadow, colors);
            }
        } 

        internal override void PaintOver(PaintEventArgs e, CheckState state) {
            if (Control.Appearance == Appearance.Button) {
                ButtonFlatAdapter adapter = new ButtonFlatAdapter(Control);
                adapter.PaintOver(e, Control.Checked ? CheckState.Checked : CheckState.Unchecked);
                return;
            }

            ColorData colors = PaintFlatRender(e.Graphics).Calculate();
            if (Control.Enabled) {
                PaintFlatWorker(e, colors.windowText, colors.lowHighlight, colors.windowFrame, colors);
            }
            else {
                PaintFlatWorker(e, colors.buttonShadow, colors.buttonFace, colors.buttonShadow, colors);
            }
        }

        internal override void PaintUp(PaintEventArgs e, CheckState state) {
            if (Control.Appearance == Appearance.Button) {
                ButtonFlatAdapter adapter = new ButtonFlatAdapter(Control);
                adapter.PaintUp(e, Control.Checked ? CheckState.Checked : CheckState.Unchecked);
                return;
            }

            ColorData colors = PaintFlatRender(e.Graphics).Calculate();
            if (Control.Enabled) {
                PaintFlatWorker(e, colors.windowText, colors.highlight, colors.windowFrame, colors);
            }
            else {
                PaintFlatWorker(e, colors.buttonShadow, colors.buttonFace, colors.buttonShadow, colors);
            }
        }

        void PaintFlatWorker(PaintEventArgs e, Color checkColor, Color checkBackground, Color checkBorder, ColorData colors) {
            System.Drawing.Graphics g = e.Graphics;
            LayoutData layout = Layout(e).Layout();
            PaintButtonBackground(e, Control.ClientRectangle, null);

            PaintImage(e, layout);
            DrawCheckFlat(e, layout, checkColor, colors.options.highContrast ? colors.buttonFace : checkBackground, checkBorder);
            AdjustFocusRectangle(layout);
            PaintField(e, layout, colors, checkColor, true);
        }

        #region Layout

        protected override ButtonBaseAdapter CreateButtonAdapter() {
            return new ButtonFlatAdapter(Control);
        }

        // RadioButtonPopupLayout also uses this layout for down and over
        protected override LayoutOptions Layout(PaintEventArgs e) {
            LayoutOptions layout = CommonLayout();
            layout.checkSize         = (int)(flatCheckSize * GetDpiScaleRatio(e.Graphics));
            layout.shadowedText      = false;

            return layout;
        }

        #endregion

    }
}
