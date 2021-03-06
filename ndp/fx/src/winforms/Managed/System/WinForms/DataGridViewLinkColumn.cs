//------------------------------------------------------------------------------
// <copyright file="DataGridViewLinkColumn.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Windows.Forms
{
    using System;
    using System.Text;
    using System.Drawing;
    using System.ComponentModel;
    using System.Windows.Forms;
    using System.Globalization;

    /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn"]/*' />
    [ToolboxBitmapAttribute(typeof(DataGridViewLinkColumn), "DataGridViewLinkColumn.bmp")]
    public class DataGridViewLinkColumn : DataGridViewColumn
    {
        private static Type columnType = typeof(DataGridViewLinkColumn);

        private string text;

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.DataGridViewLinkColumn"]/*' />
        public DataGridViewLinkColumn() : base(new DataGridViewLinkCell())
        {
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.ActiveLinkColor"]/*' />
        [
            SRCategory(SR.CatAppearance),
            SRDescription(SR.DataGridView_LinkColumnActiveLinkColorDescr)
        ]
        public Color ActiveLinkColor
        {
            get
            {
                if (this.CellTemplate == null)
                {
                    throw new InvalidOperationException(SR.GetString(SR.DataGridViewColumn_CellTemplateRequired));
                }
                return ((DataGridViewLinkCell)this.CellTemplate).ActiveLinkColor;
            }
            set
            {
                if (!this.ActiveLinkColor.Equals(value))
                {
                    ((DataGridViewLinkCell)this.CellTemplate).ActiveLinkColorInternal = value;
                    if (this.DataGridView != null)
                    {
                        DataGridViewRowCollection dataGridViewRows = this.DataGridView.Rows;
                        int rowCount = dataGridViewRows.Count;
                        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                        {
                            DataGridViewRow dataGridViewRow = dataGridViewRows.SharedRow(rowIndex);
                            DataGridViewLinkCell dataGridViewCell = dataGridViewRow.Cells[this.Index] as DataGridViewLinkCell;
                            if (dataGridViewCell != null)
                            {
                                dataGridViewCell.ActiveLinkColorInternal = value;
                            }
                        }
                        this.DataGridView.InvalidateColumn(this.Index);
                    }
                }
            }
        }

        private bool ShouldSerializeActiveLinkColor()
        {
            if (SystemInformation.HighContrast && AccessibilityImprovements.Level2)
            {
                return !this.ActiveLinkColor.Equals(SystemColors.HotTrack);
            }

            return !this.ActiveLinkColor.Equals(LinkUtilities.IEActiveLinkColor);
        }
        
        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.CellTemplate"]/*' />
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)
        ]
        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                if (value != null && !(value is System.Windows.Forms.DataGridViewLinkCell))
                {
                    throw new InvalidCastException(SR.GetString(SR.DataGridViewTypeColumn_WrongCellTemplateType, "System.Windows.Forms.DataGridViewLinkCell"));
                }
                base.CellTemplate = value;
            }
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.LinkBehavior"]/*' />
        [
            DefaultValue(LinkBehavior.SystemDefault),
            SRCategory(SR.CatBehavior),
            SRDescription(SR.DataGridView_LinkColumnLinkBehaviorDescr)
        ]
        public LinkBehavior LinkBehavior
        {
            get
            {
                if (this.CellTemplate == null)
                {
                    throw new InvalidOperationException(SR.GetString(SR.DataGridViewColumn_CellTemplateRequired));
                }
                return ((DataGridViewLinkCell)this.CellTemplate).LinkBehavior;
            }
            set
            {
                if (!this.LinkBehavior.Equals(value))
                {
                    ((DataGridViewLinkCell)this.CellTemplate).LinkBehavior = value;
                    if (this.DataGridView != null)
                    {
                        DataGridViewRowCollection dataGridViewRows = this.DataGridView.Rows;
                        int rowCount = dataGridViewRows.Count;
                        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                        {
                            DataGridViewRow dataGridViewRow = dataGridViewRows.SharedRow(rowIndex);
                            DataGridViewLinkCell dataGridViewCell = dataGridViewRow.Cells[this.Index] as DataGridViewLinkCell;
                            if (dataGridViewCell != null)
                            {
                                dataGridViewCell.LinkBehaviorInternal = value;
                            }
                        }
                        this.DataGridView.InvalidateColumn(this.Index);
                    }
                }
            }
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.LinkColor"]/*' />
        [
            SRCategory(SR.CatAppearance),
            SRDescription(SR.DataGridView_LinkColumnLinkColorDescr)
        ]
        public Color LinkColor
        {
            get
            {
                if (this.CellTemplate == null)
                {
                    throw new InvalidOperationException(SR.GetString(SR.DataGridViewColumn_CellTemplateRequired));
                }
                return ((DataGridViewLinkCell)this.CellTemplate).LinkColor;
            }
            set
            {
                if (!this.LinkColor.Equals(value))
                {
                    ((DataGridViewLinkCell)this.CellTemplate).LinkColorInternal = value;
                    if (this.DataGridView != null)
                    {
                        DataGridViewRowCollection dataGridViewRows = this.DataGridView.Rows;
                        int rowCount = dataGridViewRows.Count;
                        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                        {
                            DataGridViewRow dataGridViewRow = dataGridViewRows.SharedRow(rowIndex);
                            DataGridViewLinkCell dataGridViewCell = dataGridViewRow.Cells[this.Index] as DataGridViewLinkCell;
                            if (dataGridViewCell != null)
                            {
                                dataGridViewCell.LinkColorInternal = value;
                            }
                        }
                        this.DataGridView.InvalidateColumn(this.Index);
                    }
                }
            }
        }

        private bool ShouldSerializeLinkColor()
        {
            if (SystemInformation.HighContrast && AccessibilityImprovements.Level2)
            {
                return !this.LinkColor.Equals(SystemColors.HotTrack);
            }

            return !this.LinkColor.Equals(LinkUtilities.IELinkColor);
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.Text"]/*' />
        [
            DefaultValue(null),
            SRCategory(SR.CatAppearance),
            SRDescription(SR.DataGridView_LinkColumnTextDescr)
        ]
        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                if (!string.Equals(value, this.text, StringComparison.Ordinal))
                {
                    this.text = value;
                    if (this.DataGridView != null)
                    {
                        if (this.UseColumnTextForLinkValue)
                        {
                            this.DataGridView.OnColumnCommonChange(this.Index);
                        }
                        else
                        {
                            DataGridViewRowCollection dataGridViewRows = this.DataGridView.Rows;
                            int rowCount = dataGridViewRows.Count;
                            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                            {
                                DataGridViewRow dataGridViewRow = dataGridViewRows.SharedRow(rowIndex);
                                DataGridViewLinkCell dataGridViewCell = dataGridViewRow.Cells[this.Index] as DataGridViewLinkCell;
                                if (dataGridViewCell != null && dataGridViewCell.UseColumnTextForLinkValue)
                                {
                                    this.DataGridView.OnColumnCommonChange(this.Index);
                                    return;
                                }
                            }
                            this.DataGridView.InvalidateColumn(this.Index);
                        }
                    }
                }
            }
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.TrackVisitedState"]/*' />
        [
            DefaultValue(true),
            SRCategory(SR.CatBehavior),
            SRDescription(SR.DataGridView_LinkColumnTrackVisitedStateDescr)
        ]
        public bool TrackVisitedState
        {
            get
            {
                if (this.CellTemplate == null)
                {
                    throw new InvalidOperationException(SR.GetString(SR.DataGridViewColumn_CellTemplateRequired));
                }
                return ((DataGridViewLinkCell)this.CellTemplate).TrackVisitedState;
            }
            set
            {
                if (this.TrackVisitedState != value)
                {
                    ((DataGridViewLinkCell)this.CellTemplate).TrackVisitedStateInternal = value;
                    if (this.DataGridView != null)
                    {
                        DataGridViewRowCollection dataGridViewRows = this.DataGridView.Rows;
                        int rowCount = dataGridViewRows.Count;
                        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                        {
                            DataGridViewRow dataGridViewRow = dataGridViewRows.SharedRow(rowIndex);
                            DataGridViewLinkCell dataGridViewCell = dataGridViewRow.Cells[this.Index] as DataGridViewLinkCell;
                            if (dataGridViewCell != null)
                            {
                                dataGridViewCell.TrackVisitedStateInternal = value;
                            }
                        }
                        this.DataGridView.InvalidateColumn(this.Index);
                    }
                }
            }
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.UseColumnTextForLinkValue"]/*' />
        [
            DefaultValue(false),
            SRCategory(SR.CatAppearance),
            SRDescription(SR.DataGridView_LinkColumnUseColumnTextForLinkValueDescr)
        ]
        public bool UseColumnTextForLinkValue
        {
            get
            {
                if (this.CellTemplate == null)
                {
                    throw new InvalidOperationException(SR.GetString(SR.DataGridViewColumn_CellTemplateRequired));
                }
                return ((DataGridViewLinkCell)this.CellTemplate).UseColumnTextForLinkValue;
            }
            set
            {
                if (this.UseColumnTextForLinkValue != value)
                {
                    ((DataGridViewLinkCell)this.CellTemplate).UseColumnTextForLinkValueInternal = value;
                    if (this.DataGridView != null)
                    {
                        DataGridViewRowCollection dataGridViewRows = this.DataGridView.Rows;
                        int rowCount = dataGridViewRows.Count;
                        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                        {
                            DataGridViewRow dataGridViewRow = dataGridViewRows.SharedRow(rowIndex);
                            DataGridViewLinkCell dataGridViewCell = dataGridViewRow.Cells[this.Index] as DataGridViewLinkCell;
                            if (dataGridViewCell != null)
                            {
                                dataGridViewCell.UseColumnTextForLinkValueInternal = value;
                            }
                        }
                        this.DataGridView.OnColumnCommonChange(this.Index);
                    }
                }
            }
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.VisitedLinkColor"]/*' />
        [
            SRCategory(SR.CatAppearance),
            SRDescription(SR.DataGridView_LinkColumnVisitedLinkColorDescr)
        ]
        public Color VisitedLinkColor
        {
            get
            {
                if (this.CellTemplate == null)
                {
                    throw new InvalidOperationException(SR.GetString(SR.DataGridViewColumn_CellTemplateRequired));
                }
                return ((DataGridViewLinkCell)this.CellTemplate).VisitedLinkColor;
            }
            set
            {
                if (!this.VisitedLinkColor.Equals(value))
                {
                    ((DataGridViewLinkCell)this.CellTemplate).VisitedLinkColorInternal = value;
                    if (this.DataGridView != null)
                    {
                        DataGridViewRowCollection dataGridViewRows = this.DataGridView.Rows;
                        int rowCount = dataGridViewRows.Count;
                        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                        {
                            DataGridViewRow dataGridViewRow = dataGridViewRows.SharedRow(rowIndex);
                            DataGridViewLinkCell dataGridViewCell = dataGridViewRow.Cells[this.Index] as DataGridViewLinkCell;
                            if (dataGridViewCell != null)
                            {
                                dataGridViewCell.VisitedLinkColorInternal = value;
                            }
                        }
                        this.DataGridView.InvalidateColumn(this.Index);
                    }
                }
            }
        }

        private bool ShouldSerializeVisitedLinkColor()
        {
            if (SystemInformation.HighContrast && AccessibilityImprovements.Level2)
            {
                return !this.VisitedLinkColor.Equals(SystemColors.HotTrack);
            }

            return !this.VisitedLinkColor.Equals(LinkUtilities.IEVisitedLinkColor);
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.Clone"]/*' />
        public override object Clone()
        {
            DataGridViewLinkColumn dataGridViewColumn;
            Type thisType = this.GetType();

            if (thisType == columnType) //performance improvement
            {
                dataGridViewColumn = new DataGridViewLinkColumn();
            }
            else
            {
                // SECREVIEW : Late-binding does not represent a security threat, see bug#411899 for more info..
                //
                dataGridViewColumn = (DataGridViewLinkColumn)System.Activator.CreateInstance(thisType);
            }
            if (dataGridViewColumn != null)
            {
                base.CloneInternal(dataGridViewColumn);
                dataGridViewColumn.Text = this.text;
            }
            return dataGridViewColumn;
        }

        /// <include file='doc\DataGridViewLinkColumn.uex' path='docs/doc[@for="DataGridViewLinkColumn.ToString"]/*' />
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(64);
            sb.Append("DataGridViewLinkColumn { Name=");
            sb.Append(this.Name);
            sb.Append(", Index=");
            sb.Append(this.Index.ToString(CultureInfo.CurrentCulture));
            sb.Append(" }");
            return sb.ToString();
        }
    }
}
