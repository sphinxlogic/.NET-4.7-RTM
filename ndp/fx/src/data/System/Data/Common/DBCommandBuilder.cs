//------------------------------------------------------------------------------
// <copyright file="CommandBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data.Common {

    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    public abstract class DbCommandBuilder : Component { // V1.2.3300
        private class ParameterNames {
            private const string DefaultOriginalPrefix = "Original_";
            private const string DefaultIsNullPrefix = "IsNull_";

            // we use alternative prefix if the default prefix fails parametername validation
            private const string AlternativeOriginalPrefix = "original";
            private const string AlternativeIsNullPrefix = "isnull";
            private const string AlternativeOriginalPrefix2 = "ORIGINAL";
            private const string AlternativeIsNullPrefix2 = "ISNULL";

            private string _originalPrefix;
            private string _isNullPrefix;

            private Regex _parameterNameParser;
            private DbCommandBuilder _dbCommandBuilder;
            private string[] _baseParameterNames;
            private string[] _originalParameterNames;
            private string[] _nullParameterNames;
            private bool[] _isMutatedName;
            private int _count;
            private int _genericParameterCount;
            private int _adjustedParameterNameMaxLength;

            internal ParameterNames(DbCommandBuilder dbCommandBuilder, DbSchemaRow[] schemaRows) {
                _dbCommandBuilder = dbCommandBuilder;
                _baseParameterNames = new string[schemaRows.Length];
                _originalParameterNames = new string[schemaRows.Length];
                _nullParameterNames = new string[schemaRows.Length];
                _isMutatedName = new bool[schemaRows.Length];
                _count = schemaRows.Length;
                _parameterNameParser = new Regex(_dbCommandBuilder.ParameterNamePattern, RegexOptions.ExplicitCapture | RegexOptions.Singleline);

                SetAndValidateNamePrefixes();
                _adjustedParameterNameMaxLength = GetAdjustedParameterNameMaxLength();

                // Generate the baseparameter names and remove conflicting names
                // No names will be generated for any name that is rejected due to invalid prefix, regex violation or
                // name conflict after mutation.
                // All null values will be replaced with generic parameter names
                //
                for (int i = 0; i < schemaRows.Length; i++) {
                    if (null == schemaRows[i]) {
                        continue;
                    }
                    bool isMutatedName = false;
                    string columnName = schemaRows[i].ColumnName;

                    // all names that start with original- or isNullPrefix are invalid
                    if (null != _originalPrefix) {
                        if (columnName.StartsWith(_originalPrefix, StringComparison.OrdinalIgnoreCase)) {
                            continue;
                        }
                    }
                    if (null != _isNullPrefix) {
                        if (columnName.StartsWith(_isNullPrefix, StringComparison.OrdinalIgnoreCase)) {
                            continue;
                        }
                    }

                    // Mutate name if it contains space(s)
                    if (columnName.IndexOf(' ') >= 0) {
                        columnName = columnName.Replace(' ', '_');
                        isMutatedName = true;
                    }

                    // Validate name against regular expression
                    if (!_parameterNameParser.IsMatch(columnName)) {
                        continue;
                    }

                    // Validate name against adjusted max parametername length
                    if (columnName.Length > _adjustedParameterNameMaxLength) {
                        continue;
                    }

                    _baseParameterNames[i] = columnName;
                    _isMutatedName[i] = isMutatedName;
                }

                EliminateConflictingNames();

                // Generate names for original- and isNullparameters
                // no names will be generated if the prefix failed parametername validation
                for (int i = 0; i < schemaRows.Length; i++) {
                    if (null != _baseParameterNames[i]) {
                        if (null != _originalPrefix) {
                            _originalParameterNames[i] = _originalPrefix + _baseParameterNames[i];
                        }
                        if (null != _isNullPrefix) {
                            // don't bother generating an 'IsNull' name if it's not used
                            if (schemaRows[i].AllowDBNull) {
                                _nullParameterNames[i] = _isNullPrefix + _baseParameterNames[i];
                            }
                        }
                    }
                }
                ApplyProviderSpecificFormat();
                GenerateMissingNames(schemaRows);
            }

            private void SetAndValidateNamePrefixes() {
                if (_parameterNameParser.IsMatch(DefaultIsNullPrefix)) {
                    _isNullPrefix = DefaultIsNullPrefix;
                }
                else if (_parameterNameParser.IsMatch(AlternativeIsNullPrefix)) {
                    _isNullPrefix = AlternativeIsNullPrefix;
                }
                else if (_parameterNameParser.IsMatch(AlternativeIsNullPrefix2)) {
                    _isNullPrefix = AlternativeIsNullPrefix2;
                }
                else {
                    _isNullPrefix = null;
                }
                if (_parameterNameParser.IsMatch(DefaultOriginalPrefix)) {
                    _originalPrefix = DefaultOriginalPrefix;
                }
                else if (_parameterNameParser.IsMatch(AlternativeOriginalPrefix)) {
                    _originalPrefix = AlternativeOriginalPrefix;
                }
                else if (_parameterNameParser.IsMatch(AlternativeOriginalPrefix2)) {
                    _originalPrefix = AlternativeOriginalPrefix2;
                }
                else {
                    _originalPrefix = null;
                }
            }

            private void ApplyProviderSpecificFormat() {
                for (int i = 0; i < _baseParameterNames.Length; i++) {
                    if (null != _baseParameterNames[i]) {
                        _baseParameterNames[i] = _dbCommandBuilder.GetParameterName(_baseParameterNames[i]);
                    }
                    if (null != _originalParameterNames[i]) {
                        _originalParameterNames[i] = _dbCommandBuilder.GetParameterName(_originalParameterNames[i]);
                    }
                    if (null != _nullParameterNames[i]) {
                        _nullParameterNames[i] = _dbCommandBuilder.GetParameterName(_nullParameterNames[i]);
                    }
                }
            }

            private void EliminateConflictingNames() {
                // 



                for (int i = 0; i < _count - 1; i++) {
                    string name = _baseParameterNames[i];
                    if (null != name) {
                        for (int j = i + 1; j < _count; j++) {
                            if (ADP.CompareInsensitiveInvariant(name, _baseParameterNames[j])) {
                                // found duplicate name
                                // the name unchanged name wins
                                int iMutatedName = _isMutatedName[j] ? j : i;
                                Debug.Assert(_isMutatedName[iMutatedName], String.Format(CultureInfo.InvariantCulture, "{0} expected to be a mutated name", _baseParameterNames[iMutatedName]));
                                _baseParameterNames[iMutatedName] = null;   // null out the culprit
                            }
                        }
                    }
                }
            }

            // Generates parameternames that couldn't be generated from columnname
            internal void GenerateMissingNames(DbSchemaRow[] schemaRows) {
                // foreach name in base names
                // if base name is null
                //  for base, original and nullnames (null names only if nullable)
                //   do
                //    generate name based on current index
                //    increment index
                //    search name in base names
                //   loop while name occures in base names
                //  end for
                // end foreach
                string name;
                for (int i = 0; i < _baseParameterNames.Length; i++) {
                    name = _baseParameterNames[i];
                    if (null == name) {
                        _baseParameterNames[i] = GetNextGenericParameterName();
                        _originalParameterNames[i] = GetNextGenericParameterName();
                        // don't bother generating an 'IsNull' name if it's not used
                        if ((null != schemaRows[i]) && schemaRows[i].AllowDBNull) {
                            _nullParameterNames[i] = GetNextGenericParameterName();
                        }
                    }
                }
            }

            private int GetAdjustedParameterNameMaxLength() {
                int maxPrefixLength = Math.Max(
                    (null != _isNullPrefix ? _isNullPrefix.Length : 0),
                    (null != _originalPrefix ? _originalPrefix.Length : 0)
                    ) + _dbCommandBuilder.GetParameterName("").Length;
                return _dbCommandBuilder.ParameterNameMaxLength - maxPrefixLength;
            }

            private string GetNextGenericParameterName() {
                string name;
                bool nameExist;
                do {
                    nameExist = false;
                    _genericParameterCount++;
                    name = _dbCommandBuilder.GetParameterName(_genericParameterCount);
                    for (int i = 0; i < _baseParameterNames.Length; i++) {
                        if (ADP.CompareInsensitiveInvariant(_baseParameterNames[i], name)) {
                            nameExist = true;
                            break;
                        }
                    }
                } while (nameExist);
                return name;
            }

            internal string GetBaseParameterName(int index) {
                return (_baseParameterNames[index]);
            }
            internal string GetOriginalParameterName(int index) {
                return (_originalParameterNames[index]);
            }
            internal string GetNullParameterName(int index) {
                return (_nullParameterNames[index]);
            }
        }

        private const string DeleteFrom          = "DELETE FROM ";

        private const string InsertInto          = "INSERT INTO ";
        private const string DefaultValues       = " DEFAULT VALUES";
        private const string Values              = " VALUES ";

        private const string Update              = "UPDATE ";

        private const string Set                 = " SET ";
        private const string Where               = " WHERE ";
        private const string SpaceLeftParenthesis = " (";

        private const string Comma               = ", ";
        private const string Equal               = " = ";
        private const string LeftParenthesis     = "(";
        private const string RightParenthesis    = ")";
        private const string NameSeparator       = ".";

        private const string IsNull              = " IS NULL";
        private const string EqualOne            = " = 1";
        private const string And                 = " AND ";
        private const string Or                  = " OR ";

        private DbDataAdapter _dataAdapter;

        private DbCommand _insertCommand;
        private DbCommand _updateCommand;
        private DbCommand _deleteCommand;

        private MissingMappingAction _missingMappingAction;

        private ConflictOption _conflictDetection = ConflictOption.CompareAllSearchableValues;
        private bool _setAllValues = false;
        private bool _hasPartialPrimaryKey = false;

        private DataTable _dbSchemaTable;
        private DbSchemaRow[] _dbSchemaRows;
        private string[] _sourceColumnNames;
        private ParameterNames _parameterNames = null;

        private string _quotedBaseTableName;

        // quote strings to use around SQL object names
        private CatalogLocation _catalogLocation = CatalogLocation.Start;
        private string _catalogSeparator = NameSeparator;
        private string _schemaSeparator = NameSeparator;
        private string _quotePrefix = "";
        private string _quoteSuffix = "";
        private string _parameterNamePattern = null;
        private string _parameterMarkerFormat = null;
        private int    _parameterNameMaxLength = 0;

        protected DbCommandBuilder() : base() { // V1.2.3300
        }

        [
        DefaultValueAttribute(ConflictOption.CompareAllSearchableValues),
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbCommandBuilder_ConflictOption),
        ]
        virtual public ConflictOption ConflictOption { // V1.2.3300
            get {
                return _conflictDetection;
            }
            set {
                switch(value) {
                case ConflictOption.CompareAllSearchableValues:
                case ConflictOption.CompareRowVersion:
                case ConflictOption.OverwriteChanges:
                    _conflictDetection = value;
                    break;
                default:
                    throw ADP.InvalidConflictOptions(value);
                }
            }
        }

        [
        DefaultValueAttribute(CatalogLocation.Start),
        ResCategoryAttribute(Res.DataCategory_Schema),
        ResDescriptionAttribute(Res.DbCommandBuilder_CatalogLocation),
        ]
        virtual public CatalogLocation CatalogLocation { // V1.2.3300, MDAC 79449
            get {
                return _catalogLocation;
            }
            set {
                if (null != _dbSchemaTable) {
                    throw ADP.NoQuoteChange();
                }
                switch(value) {
                case CatalogLocation.Start:
                case CatalogLocation.End:
                    _catalogLocation = value;
                    break;
                default:
                    throw ADP.InvalidCatalogLocation(value);
                }
            }
        }

        [
        DefaultValueAttribute(DbCommandBuilder.NameSeparator),
        ResCategoryAttribute(Res.DataCategory_Schema),
        ResDescriptionAttribute(Res.DbCommandBuilder_CatalogSeparator),
        ]
        virtual public string CatalogSeparator { // V1.2.3300,  MDAC 79449
            get {
                string catalogSeparator = _catalogSeparator;
                return (((null != catalogSeparator) && (0 < catalogSeparator.Length)) ? catalogSeparator : NameSeparator);
            }
            set {
                if (null != _dbSchemaTable) {
                    throw ADP.NoQuoteChange();
                }
                _catalogSeparator = value;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.DbCommandBuilder_DataAdapter),
        ]
        public DbDataAdapter DataAdapter { // V1.2.3300
            get {
                return _dataAdapter;
            }
            set {
                if (_dataAdapter != value) {
                    RefreshSchema();

                    if (null != _dataAdapter) {
                        // derived should remove event handler from old adapter
                        SetRowUpdatingHandler(_dataAdapter);
                        _dataAdapter = null;
                    }
                    if (null != value) {
                        // derived should add event handler to new adapter
                        SetRowUpdatingHandler(value);
                        _dataAdapter = value;
                    }
                }
            }
        }

        internal int ParameterNameMaxLength {
            get {
                return _parameterNameMaxLength;
            }
        }

        internal string ParameterNamePattern {
            get {
                return _parameterNamePattern;
            }
        }

        private string QuotedBaseTableName {
            get {
                return _quotedBaseTableName;
            }
        }

        [
        DefaultValueAttribute(""),
        ResCategoryAttribute(Res.DataCategory_Schema),
        ResDescriptionAttribute(Res.DbCommandBuilder_QuotePrefix),
        ]
        virtual public string QuotePrefix { // V1.2.3300, XXXCommandBuilder V1.0.3300
            get {
                string quotePrefix = _quotePrefix;
                return ((null != quotePrefix) ? quotePrefix : ADP.StrEmpty);
            }
            set {
                if (null != _dbSchemaTable) {
                    throw ADP.NoQuoteChange();
                }
                _quotePrefix = value;
            }
        }

        [
        DefaultValueAttribute(""),
        ResCategoryAttribute(Res.DataCategory_Schema),
        ResDescriptionAttribute(Res.DbCommandBuilder_QuoteSuffix),
        ]
        virtual public string QuoteSuffix { // V1.2.3300, XXXCommandBuilder V1.0.3300
            get {
                string quoteSuffix = _quoteSuffix;
                return ((null != quoteSuffix) ? quoteSuffix : ADP.StrEmpty);
            }
            set {
                if (null != _dbSchemaTable) {
                    throw ADP.NoQuoteChange();
                }
                _quoteSuffix = value;
            }
        }


        [
        DefaultValueAttribute(DbCommandBuilder.NameSeparator),
        ResCategoryAttribute(Res.DataCategory_Schema),
        ResDescriptionAttribute(Res.DbCommandBuilder_SchemaSeparator),
        ]
        virtual public string SchemaSeparator { // V1.2.3300, MDAC 79449
            get {
                string schemaSeparator = _schemaSeparator;
                return (((null != schemaSeparator) && (0 < schemaSeparator.Length)) ? schemaSeparator : NameSeparator);
            }
            set {
                if (null != _dbSchemaTable) {
                    throw ADP.NoQuoteChange();
                }
                _schemaSeparator = value;
            }
        }

        [
        DefaultValueAttribute(false),
        ResCategoryAttribute(Res.DataCategory_Schema),
        ResDescriptionAttribute(Res.DbCommandBuilder_SetAllValues),
        ]
        public bool SetAllValues {
            get {
                return _setAllValues;
            }
            set {
                _setAllValues = value;
            }
        }

        private DbCommand InsertCommand {
            get {
                return _insertCommand;
            }
            set {
                _insertCommand = value;
            }
        }

        private DbCommand UpdateCommand {
            get {
                return _updateCommand;
            }
            set {
                _updateCommand = value;
            }
        }

        private DbCommand DeleteCommand {
            get {
                return _deleteCommand;
            }
            set {
                _deleteCommand = value;
            }
        }

        private void BuildCache(bool closeConnection, DataRow dataRow, bool useColumnsForParameterNames) { // V1.2.3300
            // Don't bother building the cache if it's done already; wait for
            // the user to call RefreshSchema first.
            if ((null != _dbSchemaTable) && (!useColumnsForParameterNames || (null != _parameterNames))) {
                return;
            }
            DataTable schemaTable = null;

            DbCommand srcCommand = GetSelectCommand();
            DbConnection connection = srcCommand.Connection;
            if (null == connection) {
                throw ADP.MissingSourceCommandConnection();
            }

            try {
                if (0 == (ConnectionState.Open & connection.State)) {
                    connection.Open();
                }
                else {
                    closeConnection = false;
                }

                if (useColumnsForParameterNames) {
                    DataTable dataTable = connection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
                    if (dataTable.Rows.Count == 1) {
                        _parameterNamePattern = dataTable.Rows[0][DbMetaDataColumnNames.ParameterNamePattern] as string;
                        _parameterMarkerFormat = dataTable.Rows[0][DbMetaDataColumnNames.ParameterMarkerFormat] as string;

                        object oParameterNameMaxLength = dataTable.Rows[0][DbMetaDataColumnNames.ParameterNameMaxLength];
                        _parameterNameMaxLength = (oParameterNameMaxLength is int) ? (int)oParameterNameMaxLength : 0;

                        // note that we protect against errors in the xml file!
                        if (0 == _parameterNameMaxLength || null == _parameterNamePattern || null == _parameterMarkerFormat) {
                            useColumnsForParameterNames = false;
                        }
                    }
                    else {
                        Debug.Assert(false, "Rowcount expected to be 1");
                        useColumnsForParameterNames = false;
                    }
                }
                schemaTable = GetSchemaTable(srcCommand);
            }
            finally {
                if (closeConnection) {
                    connection.Close();
                }
            }

            if (null == schemaTable) {
                throw ADP.DynamicSQLNoTableInfo();
            }
#if DEBUG
            //if (AdapterSwitches.DbCommandBuilder.TraceVerbose) {
            //    ADP.TraceDataTable("DbCommandBuilder", schemaTable);
            //}
#endif
            BuildInformation(schemaTable);

            _dbSchemaTable = schemaTable;

            DbSchemaRow[] schemaRows = _dbSchemaRows;
            string[] srcColumnNames = new string[schemaRows.Length];
            for (int i = 0; i < schemaRows.Length; ++i) {
                if (null != schemaRows[i]) {
                    srcColumnNames[i] = schemaRows[i].ColumnName;
                }
            }
            _sourceColumnNames = srcColumnNames;
            if (useColumnsForParameterNames) {
                _parameterNames = new ParameterNames(this, schemaRows);
            }
            ADP.BuildSchemaTableInfoTableNames(srcColumnNames);
        }

        virtual protected DataTable GetSchemaTable (DbCommand sourceCommand) {
            using (IDataReader dataReader = sourceCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo)){
                return dataReader.GetSchemaTable();
            }
        }

        private void BuildInformation(DataTable schemaTable) {
            DbSchemaRow[] rows = DbSchemaRow.GetSortedSchemaRows(schemaTable, false); // MDAC 60609
            if ((null == rows) || (0 == rows.Length)) {
                throw ADP.DynamicSQLNoTableInfo();
            }

            string baseServerName = ""; // MDAC 72721, 73599
            string baseCatalogName = "";
            string baseSchemaName = "";
            string baseTableName = null;

            for (int i = 0; i < rows.Length; ++i) {
                DbSchemaRow row = rows[i];
                string tableName = row.BaseTableName;
                if ((null == tableName) || (0 == tableName.Length)) {
                    rows[i] = null;
                    continue;
                }

                string serverName = row.BaseServerName;
                string catalogName = row.BaseCatalogName;
                string schemaName = row.BaseSchemaName;
                if (null == serverName) {
                    serverName = "";
                }
                if (null == catalogName) {
                    catalogName = "";
                }
                if (null == schemaName) {
                    schemaName = "";
                }
                if (null == baseTableName) {
                    baseServerName = serverName;
                    baseCatalogName = catalogName;
                    baseSchemaName = schemaName;
                    baseTableName = tableName;
                }
                else if (  (0 != ADP.SrcCompare(baseTableName, tableName))
                    || (0 != ADP.SrcCompare(baseSchemaName, schemaName))
                    || (0 != ADP.SrcCompare(baseCatalogName, catalogName))
                    || (0 != ADP.SrcCompare(baseServerName, serverName))) {
                    throw ADP.DynamicSQLJoinUnsupported();
                }
            }
            if (0 == baseServerName.Length) {
                baseServerName = null;
            }
            if (0 == baseCatalogName.Length) {
                baseServerName = null;
                baseCatalogName = null;
            }
            if (0 == baseSchemaName.Length) {
                baseServerName = null;
                baseCatalogName = null;
                baseSchemaName = null;
            }
            if ((null == baseTableName) || (0 == baseTableName.Length)) {
                throw ADP.DynamicSQLNoTableInfo();
            }

            CatalogLocation location = CatalogLocation;
            string catalogSeparator = CatalogSeparator;
            string schemaSeparator = SchemaSeparator;

            string quotePrefix = QuotePrefix;
            string quoteSuffix = QuoteSuffix;

            if (!ADP.IsEmpty(quotePrefix) && (-1 != baseTableName.IndexOf(quotePrefix, StringComparison.Ordinal))) {
                throw ADP.DynamicSQLNestedQuote(baseTableName, quotePrefix);
            }
            if (!ADP.IsEmpty(quoteSuffix) && (-1 != baseTableName.IndexOf(quoteSuffix, StringComparison.Ordinal))) {
                throw ADP.DynamicSQLNestedQuote(baseTableName, quoteSuffix);
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (CatalogLocation.Start == location) {
                // MDAC 79449
                if (null != baseServerName) {
                    builder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, baseServerName));
                    builder.Append(catalogSeparator);
                }
                if (null != baseCatalogName) {
                    builder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, baseCatalogName));
                    builder.Append(catalogSeparator);
                }
                // 
            }
            if (null != baseSchemaName) {
                builder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, baseSchemaName));
                builder.Append(schemaSeparator);
            }
            // 
            builder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, baseTableName));

            if (CatalogLocation.End == location) {
                // MDAC 79449
                if (null != baseServerName) {
                    builder.Append(catalogSeparator);
                    builder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, baseServerName));
                }
                if (null != baseCatalogName) {
                    builder.Append(catalogSeparator);
                    builder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, baseCatalogName));
                }
            }
            _quotedBaseTableName = builder.ToString();

            _hasPartialPrimaryKey = false;
            foreach(DbSchemaRow row in rows) {
                if ((null != row) && (row.IsKey || row.IsUnique) && !row.IsLong && !row.IsRowVersion && row.IsHidden) {
                    _hasPartialPrimaryKey = true;
                    break;
                }
            }
            _dbSchemaRows = rows;
        }

        private DbCommand BuildDeleteCommand(DataTableMapping mappings, DataRow dataRow) {
            DbCommand command = InitializeCommand(DeleteCommand);
            StringBuilder builder = new StringBuilder();
            int parameterCount  = 0;

            Debug.Assert (!ADP.IsEmpty(_quotedBaseTableName), "no table name");

            builder.Append(DeleteFrom);
            builder.Append(QuotedBaseTableName);

            parameterCount = BuildWhereClause(mappings, dataRow, builder, command, parameterCount, false);

            command.CommandText = builder.ToString();

            RemoveExtraParameters(command, parameterCount);
            DeleteCommand = command;
            return command;
        }

        private DbCommand BuildInsertCommand(DataTableMapping mappings, DataRow dataRow) {
            DbCommand command = InitializeCommand(InsertCommand);
            StringBuilder builder = new StringBuilder();
            int             parameterCount  = 0;
            string          nextSeparator   = SpaceLeftParenthesis;

            Debug.Assert (!ADP.IsEmpty(_quotedBaseTableName), "no table name");

            builder.Append(InsertInto);
            builder.Append(QuotedBaseTableName);

            // search for the columns in that base table, to be the column clause
            DbSchemaRow[] schemaRows = _dbSchemaRows;

            string[] parameterName = new string[schemaRows.Length];
            for (int i = 0; i < schemaRows.Length; ++i) {
                DbSchemaRow row = schemaRows[i];

                if ( (null == row) || (0 == row.BaseColumnName.Length) || !IncludeInInsertValues(row) )
                    continue;

                object currentValue = null;
                string sourceColumn = _sourceColumnNames[i];

                // If we're building a statement for a specific row, then check the
                // values to see whether the column should be included in the insert
                // statement or not
                if ((null != mappings) && (null != dataRow)) {
                    DataColumn dataColumn = GetDataColumn(sourceColumn, mappings, dataRow);

                    if (null == dataColumn)
                        continue;

                    // Don't bother inserting if the column is readonly in both the data
                    // set and the back end.
                    if (row.IsReadOnly && dataColumn.ReadOnly)
                        continue;

                    currentValue = GetColumnValue(dataRow, dataColumn, DataRowVersion.Current);

                    // If the value is null, and the column doesn't support nulls, then
                    // the user is requesting the server-specified default value, so don't
                    // include it in the set-list.
                    if ( !row.AllowDBNull && (null == currentValue || Convert.IsDBNull(currentValue)) )
                        continue;
                }

                builder.Append(nextSeparator);
                nextSeparator = Comma;
                builder.Append(QuotedColumn(row.BaseColumnName));

                parameterName[parameterCount] = CreateParameterForValue(
                    command,
                    GetBaseParameterName(i),
                    sourceColumn,
                    DataRowVersion.Current,
                    parameterCount,
                    currentValue,
                    row, StatementType.Insert, false
                    );
                parameterCount++;
            }

            if (0 == parameterCount)
                builder.Append(DefaultValues);
            else {
                builder.Append(RightParenthesis);
                builder.Append(Values);
                builder.Append(LeftParenthesis);

                builder.Append(parameterName[0]);
                for (int i = 1; i < parameterCount; ++i) {
                    builder.Append(Comma);
                    builder.Append(parameterName[i]);
                }

                builder.Append(RightParenthesis);
            }

            command.CommandText = builder.ToString();

            RemoveExtraParameters(command, parameterCount);
            InsertCommand = command;
            return command;
        }

        private DbCommand BuildUpdateCommand(DataTableMapping mappings, DataRow dataRow) {
            DbCommand command = InitializeCommand(UpdateCommand);
            StringBuilder builder = new StringBuilder();
            string nextSeparator = Set;
            int parameterCount  = 0;

            Debug.Assert (!ADP.IsEmpty(_quotedBaseTableName), "no table name");

            builder.Append(Update);
            builder.Append(QuotedBaseTableName);

            // search for the columns in that base table, to build the set clause
            DbSchemaRow[] schemaRows = _dbSchemaRows;
            for (int i = 0; i < schemaRows.Length; ++i) {
                DbSchemaRow row = schemaRows[i];

                if ((null == row) || (0 == row.BaseColumnName.Length) || !IncludeInUpdateSet(row))
                    continue;

                object currentValue = null;
                string sourceColumn = _sourceColumnNames[i];

                // If we're building a statement for a specific row, then check the
                // values to see whether the column should be included in the update
                // statement or not
                if ((null != mappings) && (null != dataRow)) {
                    DataColumn  dataColumn = GetDataColumn(sourceColumn, mappings, dataRow);

                    if (null == dataColumn)
                        continue;

                    // Don't bother updating if the column is readonly in both the data
                    // set and the back end.
                    if (row.IsReadOnly && dataColumn.ReadOnly)
                        continue;

                    // Unless specifically directed to do so, we will not automatically update
                    // a column with it's original value, which means that we must determine
                    // whether the value has changed locally, before we send it up.
                    currentValue = GetColumnValue(dataRow, dataColumn, DataRowVersion.Current);

                    if (!SetAllValues) {
                        object originalValue = GetColumnValue(dataRow, dataColumn, DataRowVersion.Original);

                        if ((originalValue == currentValue)
                            || ((null != originalValue) && originalValue.Equals(currentValue))) {
                            continue;
                        }
                    }
                }

                builder.Append(nextSeparator);
                nextSeparator = Comma;

                builder.Append(QuotedColumn(row.BaseColumnName));
                builder.Append(Equal);
                builder.Append(
                    CreateParameterForValue(
                        command,
                        GetBaseParameterName(i),
                        sourceColumn,
                        DataRowVersion.Current,
                        parameterCount,
                        currentValue,
                        row, StatementType.Update, false
                    )
                );
                parameterCount++;
            }

            // It is an error to attempt an update when there's nothing to update;
            bool skipRow = (0 == parameterCount);

            parameterCount = BuildWhereClause(mappings, dataRow, builder, command, parameterCount, true);

            command.CommandText = builder.ToString();

            RemoveExtraParameters(command, parameterCount);
            UpdateCommand = command;
            return (skipRow) ? null : command;
        }

        private int BuildWhereClause(
            DataTableMapping mappings,
            DataRow          dataRow,
            StringBuilder    builder,
            DbCommand        command,
            int              parameterCount,
            bool             isUpdate
            ) {
            string  beginNewCondition = string.Empty;
            int     whereCount = 0;

            builder.Append(Where);
            builder.Append(LeftParenthesis);

            DbSchemaRow[] schemaRows = _dbSchemaRows;
            for (int i = 0; i < schemaRows.Length; ++i) {
                DbSchemaRow row = schemaRows[i];

                if ((null == row) || (0 == row.BaseColumnName.Length) || !IncludeInWhereClause(row, isUpdate)) {
                    continue;
                }
                builder.Append(beginNewCondition);
                beginNewCondition = And;

                object value = null;
                string sourceColumn = _sourceColumnNames[i];
                string baseColumnName = QuotedColumn(row.BaseColumnName);

                if ((null != mappings) && (null != dataRow))
                    value = GetColumnValue(dataRow, sourceColumn, mappings, DataRowVersion.Original);

                if (!row.AllowDBNull) {
                    //  (<baseColumnName> = ?)
                    builder.Append(LeftParenthesis);
                    builder.Append(baseColumnName);
                    builder.Append(Equal);
                    builder.Append(
                        CreateParameterForValue(
                            command,
                            GetOriginalParameterName(i),
                            sourceColumn,
                            DataRowVersion.Original,
                            parameterCount,
                            value,
                            row, (isUpdate ? StatementType.Update : StatementType.Delete), true
                        )
                    );
                    parameterCount++;
                    builder.Append(RightParenthesis);
                }
                else {
                    //  ((? = 1 AND <baseColumnName> IS NULL) OR (<baseColumnName> = ?))
                    builder.Append(LeftParenthesis);

                    builder.Append(LeftParenthesis);
                    builder.Append(
                        CreateParameterForNullTest(
                            command,
                            GetNullParameterName(i),
                            sourceColumn,
                            DataRowVersion.Original,
                            parameterCount,
                            value,
                            row, (isUpdate ? StatementType.Update : StatementType.Delete), true
                        )
                    );
                    parameterCount++;
                    builder.Append(EqualOne);
                    builder.Append(And);
                    builder.Append(baseColumnName);
                    builder.Append(IsNull);
                    builder.Append(RightParenthesis);

                    builder.Append(Or);

                    builder.Append(LeftParenthesis);
                    builder.Append(baseColumnName);
                    builder.Append(Equal);
                    builder.Append(
                        CreateParameterForValue(
                            command,
                            GetOriginalParameterName(i),
                            sourceColumn,
                            DataRowVersion.Original,
                            parameterCount,
                            value,
                            row, (isUpdate ? StatementType.Update : StatementType.Delete), true
                        )
                    );
                    parameterCount++;
                    builder.Append(RightParenthesis);

                    builder.Append(RightParenthesis);
                }

                if (IncrementWhereCount(row)) {
                    whereCount++;
                }
            }

            builder.Append(RightParenthesis);

            if (0 == whereCount) {
                if (isUpdate) {
                    if (ConflictOption.CompareRowVersion == ConflictOption) {
                        throw ADP.DynamicSQLNoKeyInfoRowVersionUpdate();
                    }
                    throw ADP.DynamicSQLNoKeyInfoUpdate();
                }
                else {
                    if (ConflictOption.CompareRowVersion == ConflictOption) {
                        throw ADP.DynamicSQLNoKeyInfoRowVersionDelete();
                    }
                    throw ADP.DynamicSQLNoKeyInfoDelete();
                }
            }
            return parameterCount;
        }

        private string CreateParameterForNullTest(
            DbCommand       command,
            string          parameterName,
            string          sourceColumn,
            DataRowVersion  version,
            int             parameterCount,
            object          value,
            DbSchemaRow     row,
            StatementType   statementType,
            bool            whereClause
            ) {
            DbParameter p = GetNextParameter(command, parameterCount);

            Debug.Assert(!ADP.IsEmpty(sourceColumn), "empty source column");
            if (null == parameterName) {
                p.ParameterName = GetParameterName(1 + parameterCount);
            }
            else {
                p.ParameterName = parameterName;
            }
            p.Direction     = ParameterDirection.Input;
            p.SourceColumn  = sourceColumn;
            p.SourceVersion = version;
            p.SourceColumnNullMapping = true;
            p.Value         = value;
            p.Size          = 0; // don't specify parameter.Size so that we don't silently truncate to the metadata size

            ApplyParameterInfo(p, row.DataRow, statementType, whereClause);

            p.DbType        = DbType.Int32;
            p.Value         = ADP.IsNull(value) ? DbDataAdapter.ParameterValueNullValue : DbDataAdapter.ParameterValueNonNullValue;

            if (!command.Parameters.Contains(p)) {
                command.Parameters.Add(p);
            }

            if (null == parameterName) {
                return GetParameterPlaceholder(1 + parameterCount);
            }
            else {
                Debug.Assert(null != _parameterNames, "How can we have a parameterName without a _parameterNames collection?");
                Debug.Assert(null != _parameterMarkerFormat, "How can we have a _parameterNames collection but no _parameterMarkerFormat?");

                return String.Format(CultureInfo.InvariantCulture, _parameterMarkerFormat, parameterName);
            }
        }

        private string CreateParameterForValue(
            DbCommand       command,
            string          parameterName,
            string          sourceColumn,
            DataRowVersion  version,
            int             parameterCount,
            object          value,
            DbSchemaRow     row,
            StatementType   statementType,
            bool            whereClause
            ) {
            DbParameter p = GetNextParameter(command, parameterCount);

            if (null == parameterName) {
                p.ParameterName = GetParameterName(1 + parameterCount);
            }
            else {
                p.ParameterName = parameterName;
            }
            p.Direction     = ParameterDirection.Input;
            p.SourceColumn  = sourceColumn;
            p.SourceVersion = version;
            p.SourceColumnNullMapping = false;
            p.Value         = value;
            p.Size          = 0; // don't specify parameter.Size so that we don't silently truncate to the metadata size

            ApplyParameterInfo(p, row.DataRow, statementType, whereClause);

            if (!command.Parameters.Contains(p)) {
                command.Parameters.Add(p);
            }

            if (null == parameterName) {
                return GetParameterPlaceholder(1 + parameterCount);
            }
            else {
                Debug.Assert(null != _parameterNames, "How can we have a parameterName without a _parameterNames collection?");
                Debug.Assert(null != _parameterMarkerFormat, "How can we have a _parameterNames collection but no _parameterMarkerFormat?");

                return String.Format(CultureInfo.InvariantCulture, _parameterMarkerFormat, parameterName);
            }
        }

        override protected void Dispose(bool disposing) { // V1.2.3300, XXXCommandBuilder V1.0.3300
            // MDAC 65459
            if (disposing) {
                // release mananged objects
                DataAdapter = null;
            }
            //release unmanaged objects

            base.Dispose(disposing); // notify base classes
        }

        private DataTableMapping GetTableMapping(DataRow dataRow ) {
            DataTableMapping tableMapping = null;
            if (null != dataRow) {
                DataTable dataTable = dataRow.Table;
                if (null != dataTable) {
                    DbDataAdapter adapter = DataAdapter;
                    if (null != adapter) {
                        tableMapping = adapter.GetTableMapping(dataTable);
                    }
                    else {
                        string tableName = dataTable.TableName;
                        tableMapping = new DataTableMapping(tableName, tableName);
                    }
                }
            }
            return tableMapping;
        }

        private string GetBaseParameterName(int index) {
            if (null != _parameterNames) {
                return (_parameterNames.GetBaseParameterName(index));
            }
            else {
                return null;
            }
        }
        private string GetOriginalParameterName(int index) {
            if (null != _parameterNames) {
                return (_parameterNames.GetOriginalParameterName(index));
            }
            else {
                return null;
            }
        }
        private string GetNullParameterName(int index) {
            if (null != _parameterNames) {
                return (_parameterNames.GetNullParameterName(index));
            }
            else {
                return null;
            }
        }

        private DbCommand GetSelectCommand() { // V1.2.3300
            DbCommand select = null;
            DbDataAdapter adapter = DataAdapter;
            if (null != adapter) {
                if (0 == _missingMappingAction) {
                    _missingMappingAction = adapter.MissingMappingAction;
                }
                select = (DbCommand)adapter.SelectCommand;
            }
            if (null == select) {
                throw ADP.MissingSourceCommand();
            }
            return select;
        }

        // open connection is required by OleDb/OdbcCommandBuilder.QuoteIdentifier and UnquoteIdentifier 
        // to get literals quotes from the driver
        internal DbConnection GetConnection() {
            DbDataAdapter adapter = DataAdapter;
            if (adapter != null) {
                DbCommand select = (DbCommand)adapter.SelectCommand;
                if (select != null) {
                    return select.Connection;
                }
            }

            return null;
        }

        public DbCommand GetInsertCommand() { // V1.2.3300, XXXCommandBuilder V1.0.3300
            return GetInsertCommand((DataRow)null, false);
        }

        public DbCommand GetInsertCommand(bool useColumnsForParameterNames) {
            return GetInsertCommand((DataRow)null, useColumnsForParameterNames);
        }
        internal DbCommand GetInsertCommand(DataRow dataRow, bool useColumnsForParameterNames) {
            BuildCache(true, dataRow, useColumnsForParameterNames);
            BuildInsertCommand(GetTableMapping(dataRow), dataRow);
            return InsertCommand;
        }

        public DbCommand GetUpdateCommand() { // V1.2.3300, XXXCommandBuilder V1.0.3300
            return GetUpdateCommand((DataRow)null, false);
        }
        public DbCommand GetUpdateCommand(bool useColumnsForParameterNames) {
            return GetUpdateCommand((DataRow)null, useColumnsForParameterNames);
        }
        internal DbCommand GetUpdateCommand(DataRow dataRow, bool useColumnsForParameterNames) {
            BuildCache(true, dataRow, useColumnsForParameterNames);
            BuildUpdateCommand(GetTableMapping(dataRow), dataRow);
            return UpdateCommand;
        }

        public DbCommand GetDeleteCommand() { // V1.2.3300, XXXCommandBuilder V1.0.3300
            return GetDeleteCommand((DataRow)null, false);
        }
        public DbCommand GetDeleteCommand(bool useColumnsForParameterNames) {
            return GetDeleteCommand((DataRow)null, useColumnsForParameterNames);
        }
        internal DbCommand GetDeleteCommand(DataRow dataRow, bool useColumnsForParameterNames) {
            BuildCache(true, dataRow, useColumnsForParameterNames);
            BuildDeleteCommand(GetTableMapping(dataRow), dataRow);
            return DeleteCommand;
        }

        private object GetColumnValue(DataRow row, String columnName, DataTableMapping mappings, DataRowVersion version) {
           return GetColumnValue(row, GetDataColumn(columnName, mappings, row), version);
        }

        private object GetColumnValue(DataRow row, DataColumn column, DataRowVersion  version) {
            object value = null;
            if (null != column) {
                value = row[column, version];
            }
            return value;
        }

        private DataColumn GetDataColumn(string columnName, DataTableMapping tablemapping, DataRow row) {
            DataColumn column = null;
            if (!ADP.IsEmpty(columnName)) {
                column = tablemapping.GetDataColumn(columnName, null, row.Table, _missingMappingAction, MissingSchemaAction.Error);
            }
            return column;
        }

        static private DbParameter GetNextParameter(DbCommand command, int pcount) {
            DbParameter p;
            if (pcount < command.Parameters.Count) {
                p = command.Parameters[pcount];
            }
            else {
                p = command.CreateParameter();
                /*if (null == p) {
                    // 
*/
            }
            Debug.Assert(null != p, "null CreateParameter");
            return p;
        }

        private bool IncludeInInsertValues(DbSchemaRow row) {
            // 

            return (!row.IsAutoIncrement && !row.IsHidden && !row.IsExpression && !row.IsRowVersion && !row.IsReadOnly);
        }

        private bool IncludeInUpdateSet(DbSchemaRow row) {
            // 

            return (!row.IsAutoIncrement && !row.IsRowVersion && !row.IsHidden && !row.IsReadOnly);
        }

        private bool IncludeInWhereClause(DbSchemaRow row, bool isUpdate) {
            bool flag = IncrementWhereCount(row);
            if (flag && row.IsHidden) { // MDAC 52564
                if (ConflictOption.CompareRowVersion == ConflictOption) {
                    throw ADP.DynamicSQLNoKeyInfoRowVersionUpdate();
                }
                throw ADP.DynamicSQLNoKeyInfoUpdate();
            }
            if (!flag && (ConflictOption.CompareAllSearchableValues == ConflictOption)) {
                // include other searchable values
                flag = !row.IsLong && !row.IsRowVersion && !row.IsHidden;
            }
            return flag;
        }

        private bool IncrementWhereCount(DbSchemaRow row) {
            ConflictOption value = ConflictOption;
            switch(value) {
            case ConflictOption.CompareAllSearchableValues:
            case ConflictOption.OverwriteChanges:
                // find the primary key
                return (row.IsKey || row.IsUnique) && !row.IsLong && !row.IsRowVersion;
            case ConflictOption.CompareRowVersion:
                // or the row version
                return (((row.IsKey || row.IsUnique) && !_hasPartialPrimaryKey) || row.IsRowVersion) && !row.IsLong;
            default:
                throw ADP.InvalidConflictOptions(value);                    
            }
        }

        virtual protected DbCommand InitializeCommand(DbCommand command) { // V1.2.3300
            if (null == command) {
                DbCommand select = GetSelectCommand();
                command = select.Connection.CreateCommand();
                /*if (null == command) {
                    // 
*/

                // the following properties are only initialized when the object is created
                // all other properites are reinitialized on every row
                /*command.Connection = select.Connection;*/ // initialized by CreateCommand
                command.CommandTimeout = select.CommandTimeout;
                command.Transaction = select.Transaction;
            }
            command.CommandType      = CommandType.Text;
            command.UpdatedRowSource = UpdateRowSource.None; // no select or output parameters expected
            return command;
        }

        private string QuotedColumn(string column) {
            return ADP.BuildQuotedString(QuotePrefix, QuoteSuffix, column);
        }

        public virtual string QuoteIdentifier(string unquotedIdentifier ) {

        throw ADP.NotSupported();
        }

        virtual public void RefreshSchema() { // V1.2.3300, XXXCommandBuilder V1.0.3300
            _dbSchemaTable = null;
            _dbSchemaRows = null;
            _sourceColumnNames = null;
            _quotedBaseTableName = null;

            DbDataAdapter adapter = DataAdapter;
            if (null != adapter) { // MDAC 66016
                if (InsertCommand == adapter.InsertCommand) {
                    adapter.InsertCommand = null;
                }
                if (UpdateCommand == adapter.UpdateCommand) {
                    adapter.UpdateCommand = null;
                }
                if (DeleteCommand == adapter.DeleteCommand) {
                    adapter.DeleteCommand = null;
                }
            }
            DbCommand command;
            if (null != (command = InsertCommand)) {
                command.Dispose();
            }
            if (null != (command = UpdateCommand)) {
                command.Dispose();
            }
            if (null != (command = DeleteCommand)) {
                command.Dispose();
            }
            InsertCommand = null;
            UpdateCommand = null;
            DeleteCommand = null;
        }

        static private void RemoveExtraParameters(DbCommand command, int usedParameterCount) {
            for (int i = command.Parameters.Count-1; i >= usedParameterCount; --i) {
                command.Parameters.RemoveAt(i);
            }
        }

        protected void RowUpdatingHandler(RowUpdatingEventArgs rowUpdatingEvent) {
            if (null == rowUpdatingEvent) {
                throw ADP.ArgumentNull("rowUpdatingEvent");
            }
            try {
                if (UpdateStatus.Continue == rowUpdatingEvent.Status) {
                    StatementType stmtType = rowUpdatingEvent.StatementType;
                    DbCommand command = (DbCommand)rowUpdatingEvent.Command;

                    if (null != command) {
                        switch(stmtType) {
                        case StatementType.Select:
                            Debug.Assert(false, "how did we get here?");
                            return; // don't mess with it
                        case StatementType.Insert:
                            command = InsertCommand;
                            break;
                        case StatementType.Update:
                            command = UpdateCommand;
                            break;
                        case StatementType.Delete:
                            command = DeleteCommand;
                            break;
                        default:
                            throw ADP.InvalidStatementType(stmtType);
                        }

                        if (command != rowUpdatingEvent.Command) {
                            command = (DbCommand)rowUpdatingEvent.Command;
                            if ((null != command) && (null == command.Connection)) { // MDAC 87649
                                DbDataAdapter adapter = DataAdapter;
                                DbCommand select = ((null != adapter) ? ((DbCommand)adapter.SelectCommand) : null);
                                if (null != select) {
                                    command.Connection = (DbConnection)select.Connection;

                                }
                            }
                            // user command, not a command builder command
                        }
                        else command = null;
                    }
                    if (null == command) {
                        RowUpdatingHandlerBuilder(rowUpdatingEvent);
                    }
                 }
            }
            catch(Exception e) {
                // 
                if (!ADP.IsCatchableExceptionType(e)) {
                    throw;
                }

                ADP.TraceExceptionForCapture(e);

                rowUpdatingEvent.Status = UpdateStatus.ErrorsOccurred;
                rowUpdatingEvent.Errors = e;
            }
        }

        private void RowUpdatingHandlerBuilder(RowUpdatingEventArgs rowUpdatingEvent) {
            // MDAC 58710 - unable to tell Update method that Event opened connection and Update needs to close when done
            // HackFix - the Update method will close the connection if command was null and returned command.Connection is same as SelectCommand.Connection
            DataRow datarow = rowUpdatingEvent.Row;
            BuildCache(false, datarow, false);

            DbCommand command;
            switch(rowUpdatingEvent.StatementType) {
            case StatementType.Insert:
                command = BuildInsertCommand(rowUpdatingEvent.TableMapping, datarow);
                break;
            case StatementType.Update:
                command = BuildUpdateCommand(rowUpdatingEvent.TableMapping, datarow);
                break;
            case StatementType.Delete:
                command = BuildDeleteCommand(rowUpdatingEvent.TableMapping, datarow);
                break;
#if DEBUG
            case StatementType.Select:
                Debug.Assert(false, "how did we get here?");
                goto default;
#endif
            default:
                throw ADP.InvalidStatementType(rowUpdatingEvent.StatementType);
            }
            if (null == command) {
                if (null != datarow) {
                    datarow.AcceptChanges();
                }
                rowUpdatingEvent.Status = UpdateStatus.SkipCurrentRow;
            }
            rowUpdatingEvent.Command = command;
        }

         public virtual string UnquoteIdentifier(string quotedIdentifier ) {
            throw ADP.NotSupported();
        }

        abstract protected void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause); // V1.2.3300
        abstract protected string GetParameterName(int parameterOrdinal); // V1.2.3300
        abstract protected string GetParameterName(string parameterName);
        abstract protected string GetParameterPlaceholder(int parameterOrdinal); // V1.2.3300
        abstract protected void SetRowUpdatingHandler(DbDataAdapter adapter); // V1.2.3300


        // 



        static internal string[] ParseProcedureName(string name, string quotePrefix, string quoteSuffix) {
            // Procedure may consist of up to four parts:
            // 0) Server
            // 1) Catalog
            // 2) Schema
            // 3) ProcedureName
            //
            // Parse the string into four parts, allowing the last part to contain '.'s.
            // If less than four period delimited parts, use the parts from procedure backwards.
            //
            const string Separator = ".";

            string[] qualifiers = new string[4];
            if (!ADP.IsEmpty(name)) {
                bool useQuotes = !ADP.IsEmpty(quotePrefix) && !ADP.IsEmpty(quoteSuffix);

                int currentPos = 0, parts;
                for(parts = 0; (parts < qualifiers.Length) && (currentPos < name.Length); ++parts) {
                    int startPos = currentPos;

                    // does the part begin with a quotePrefix?
                    if (useQuotes && (name.IndexOf(quotePrefix, currentPos, quotePrefix.Length, StringComparison.Ordinal) == currentPos)) {
                        currentPos += quotePrefix.Length; // move past the quotePrefix

                        // search for the quoteSuffix (or end of string)
                        while (currentPos < name.Length) {
                            currentPos = name.IndexOf(quoteSuffix, currentPos, StringComparison.Ordinal);
                            if (currentPos < 0) {
                                // error condition, no quoteSuffix
                                currentPos = name.Length;
                                break;
                            }
                            else {
                                currentPos += quoteSuffix.Length; // move past the quoteSuffix

                                // is this a double quoteSuffix?
                                if ((currentPos < name.Length) && (name.IndexOf(quoteSuffix, currentPos, quoteSuffix.Length, StringComparison.Ordinal) == currentPos)) {
                                    // a second quoteSuffix, continue search for terminating quoteSuffix
                                    currentPos += quoteSuffix.Length; // move past the second quoteSuffix
                                }
                                else {
                                    // found the terminating quoteSuffix
                                    break;
                                }
                            }
                        }
                    }

                    // search for separator (either no quotePrefix or already past quoteSuffix)
                    if (currentPos < name.Length) {
                        currentPos = name.IndexOf(Separator, currentPos, StringComparison.Ordinal);
                        if ((currentPos < 0) || (parts == qualifiers.Length-1)) {
                            // last part that can be found
                            currentPos = name.Length;
                        }
                    }

                    qualifiers[parts] = name.Substring(startPos, currentPos-startPos);
                    currentPos += Separator.Length;
                }

                // allign the qualifiers if we had less than MaxQualifiers
                for(int j = qualifiers.Length-1; 0 <= j; --j) {
                    qualifiers[j] = ((0 < parts) ? qualifiers[--parts] : null);
                }
            }
            return qualifiers;
        }
    }
}

