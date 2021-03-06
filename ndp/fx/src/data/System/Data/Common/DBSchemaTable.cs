//------------------------------------------------------------------------------
// <copyright file="DBSchemaTable.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data.Common {

    using System;
    using System.Data;
    using System.Data.ProviderBase;
    using System.Diagnostics;
    
    sealed internal class DbSchemaTable {

        private enum ColumnEnum {
            ColumnName,
            ColumnOrdinal,
            ColumnSize,
            BaseServerName,
            BaseCatalogName,
            BaseColumnName,
            BaseSchemaName,
            BaseTableName,
            IsAutoIncrement,
            IsUnique,
            IsKey,
            IsRowVersion,
            DataType,
            ProviderSpecificDataType,
            AllowDBNull,
            ProviderType,
            IsExpression,
            IsHidden,
            IsLong,
            IsReadOnly,
            SchemaMappingUnsortedIndex,
        }

        static readonly private string[] DBCOLUMN_NAME = new string[] {
            SchemaTableColumn.ColumnName,
            SchemaTableColumn.ColumnOrdinal,
            SchemaTableColumn.ColumnSize,
            SchemaTableOptionalColumn.BaseServerName,
            SchemaTableOptionalColumn.BaseCatalogName,
            SchemaTableColumn.BaseColumnName,
            SchemaTableColumn.BaseSchemaName,
            SchemaTableColumn.BaseTableName,
            SchemaTableOptionalColumn.IsAutoIncrement,
            SchemaTableColumn.IsUnique,
            SchemaTableColumn.IsKey,
            SchemaTableOptionalColumn.IsRowVersion,
            SchemaTableColumn.DataType,
            SchemaTableOptionalColumn.ProviderSpecificDataType,
            SchemaTableColumn.AllowDBNull,
            SchemaTableColumn.ProviderType,
            SchemaTableColumn.IsExpression,
            SchemaTableOptionalColumn.IsHidden,
            SchemaTableColumn.IsLong,
            SchemaTableOptionalColumn.IsReadOnly,
            DbSchemaRow.SchemaMappingUnsortedIndex,
        };

        internal DataTable dataTable;
        private DataColumnCollection columns;
        private DataColumn[] columnCache = new DataColumn[DBCOLUMN_NAME.Length];
        private bool _returnProviderSpecificTypes;

        internal DbSchemaTable(DataTable dataTable, bool returnProviderSpecificTypes) {
            this.dataTable = dataTable;
            this.columns = dataTable.Columns;
            _returnProviderSpecificTypes = returnProviderSpecificTypes;
        }

        internal DataColumn ColumnName      { get { return CachedDataColumn(ColumnEnum.ColumnName);}}
        internal DataColumn Size            { get { return CachedDataColumn(ColumnEnum.ColumnSize);}}
        internal DataColumn BaseServerName  { get { return CachedDataColumn(ColumnEnum.BaseServerName);}}
        internal DataColumn BaseColumnName  { get { return CachedDataColumn(ColumnEnum.BaseColumnName);}}
        internal DataColumn BaseTableName   { get { return CachedDataColumn(ColumnEnum.BaseTableName);}}
        internal DataColumn BaseCatalogName { get { return CachedDataColumn(ColumnEnum.BaseCatalogName);}}
        internal DataColumn BaseSchemaName  { get { return CachedDataColumn(ColumnEnum.BaseSchemaName);}}
        internal DataColumn IsAutoIncrement { get { return CachedDataColumn(ColumnEnum.IsAutoIncrement);}}
        internal DataColumn IsUnique        { get { return CachedDataColumn(ColumnEnum.IsUnique);}}
        internal DataColumn IsKey           { get { return CachedDataColumn(ColumnEnum.IsKey);}}
        internal DataColumn IsRowVersion    { get { return CachedDataColumn(ColumnEnum.IsRowVersion);}}

        internal DataColumn AllowDBNull              { get { return CachedDataColumn(ColumnEnum.AllowDBNull);}}
        internal DataColumn IsExpression             { get { return CachedDataColumn(ColumnEnum.IsExpression);}}
        internal DataColumn IsHidden                 { get { return CachedDataColumn(ColumnEnum.IsHidden);}}
        internal DataColumn IsLong                   { get { return CachedDataColumn(ColumnEnum.IsLong);}}
        internal DataColumn IsReadOnly               { get { return CachedDataColumn(ColumnEnum.IsReadOnly);}}

        internal DataColumn UnsortedIndex   { get { return CachedDataColumn(ColumnEnum.SchemaMappingUnsortedIndex);}}
        
        internal DataColumn DataType {
            get {
                if (_returnProviderSpecificTypes) {
                    return CachedDataColumn(ColumnEnum.ProviderSpecificDataType, ColumnEnum.DataType);
                }
                return CachedDataColumn(ColumnEnum.DataType);
            }
        }

        private DataColumn CachedDataColumn(ColumnEnum column) {
            return CachedDataColumn(column, column);
        }
        
        private DataColumn CachedDataColumn(ColumnEnum column, ColumnEnum column2) {
            DataColumn dataColumn = columnCache[(int) column];
            if (null == dataColumn) {
                int index = columns.IndexOf(DBCOLUMN_NAME[(int) column]);
                if ((-1 == index) && (column != column2)) {
                    index = columns.IndexOf(DBCOLUMN_NAME[(int) column2]);
                }
                if (-1 != index) {
                    dataColumn = columns[index];
                    columnCache[(int) column] = dataColumn;
                }
            }
            return dataColumn;
        }
    }
}
