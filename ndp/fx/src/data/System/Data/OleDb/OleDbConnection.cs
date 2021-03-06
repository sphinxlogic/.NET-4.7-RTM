//------------------------------------------------------------------------------
// <copyright file="OleDbConnection.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data.OleDb {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Data.ProviderBase;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using SysTx = System.Transactions;

    // wraps the OLEDB IDBInitialize interface which represents a connection
    // Notes about connection pooling
    // 1. Connection pooling isn't supported on Win95
    // 2. Only happens if we use the IDataInitialize or IDBPromptInitialize interfaces
    //    it won't happen if you directly create the provider and set its properties
    // 3. First call on IDBInitialize must be Initialize, can't QI for any other interfaces before that
    [DefaultEvent("InfoMessage")]
    public sealed partial class OleDbConnection : DbConnection, ICloneable, IDbConnection { 

        static private readonly object EventInfoMessage = new object();

        public OleDbConnection(string connectionString) : this() {
            ConnectionString = connectionString;
        }

        private OleDbConnection(OleDbConnection connection) : this() { // Clone
            CopyFrom(connection);
        }

        [
        DefaultValue(""),
#pragma warning disable 618 // ignore obsolete warning about RecommendedAsConfigurable to use SettingsBindableAttribute
        RecommendedAsConfigurable(true),
#pragma warning restore 618
        SettingsBindableAttribute(true),
        RefreshProperties(RefreshProperties.All),
        ResCategoryAttribute(Res.DataCategory_Data),
        Editor("Microsoft.VSDesigner.Data.ADO.Design.OleDbConnectionStringEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
        ResDescriptionAttribute(Res.OleDbConnection_ConnectionString),
        ]
        override public string ConnectionString {
            get {
                return ConnectionString_Get();
            }
            set {
                ConnectionString_Set(value);
            }
        }

        private OleDbConnectionString OleDbConnectionStringValue {
            get { return (OleDbConnectionString)ConnectionOptions; }
        }

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.OleDbConnection_ConnectionTimeout),
        ]
        override public int ConnectionTimeout {
            get {
                IntPtr hscp;
                Bid.ScopeEnter(out hscp, "<oledb.OleDbConnection.get_ConnectionTimeout|API> %d#\n", ObjectID);
                try {
                    object value = null;
                    if (IsOpen) {
                        value = GetDataSourceValue(OleDbPropertySetGuid.DBInit, ODB.DBPROP_INIT_TIMEOUT);
                    }
                    else {
                        OleDbConnectionString constr = this.OleDbConnectionStringValue;
                        value = (null != constr) ? constr.ConnectTimeout : ADP.DefaultConnectionTimeout;
                    }
                    if (null != value) {
                        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    }
                    else {
                        return ADP.DefaultConnectionTimeout;
                    }
                }
                finally {
                    Bid.ScopeLeave(ref hscp);
                }
            }
        }

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.OleDbConnection_Database),
        ]
        override public string Database {
            get {
                IntPtr hscp;
                Bid.ScopeEnter(out hscp, "<oledb.OleDbConnection.get_Database|API> %d#\n", ObjectID);
                try {
                    OleDbConnectionString constr = (OleDbConnectionString)UserConnectionOptions;
                    object value = (null != constr) ? constr.InitialCatalog : ADP.StrEmpty;
                    if ((null != value) && !((string)value).StartsWith(DbConnectionOptions.DataDirectory, StringComparison.OrdinalIgnoreCase)) {
                        OleDbConnectionInternal connection = GetOpenConnection();
                        if (null != connection) {
                            if (connection.HasSession) {
                                value = GetDataSourceValue(OleDbPropertySetGuid.DataSource, ODB.DBPROP_CURRENTCATALOG);
                            }
                            else {
                                value = GetDataSourceValue(OleDbPropertySetGuid.DBInit, ODB.DBPROP_INIT_CATALOG);
                            }
                        }
                        else {
                            constr = this.OleDbConnectionStringValue;
                            value = (null != constr) ? constr.InitialCatalog : ADP.StrEmpty;
                        }
                    }
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                }
                finally {
                    Bid.ScopeLeave(ref hscp);
                }
            }
        }

        [
        Browsable(true),
        ResDescriptionAttribute(Res.OleDbConnection_DataSource),
        ]
        override public string DataSource {
            get {
                IntPtr hscp;
                Bid.ScopeEnter(out hscp, "<oledb.OleDbConnection.get_DataSource|API> %d#\n", ObjectID);
                try {
                    OleDbConnectionString constr = (OleDbConnectionString)UserConnectionOptions;
                    object value = (null != constr) ? constr.DataSource : ADP.StrEmpty;
                    if ((null != value) && !((string)value).StartsWith(DbConnectionOptions.DataDirectory, StringComparison.OrdinalIgnoreCase)) {
                        if (IsOpen) {
                            value = GetDataSourceValue(OleDbPropertySetGuid.DBInit, ODB.DBPROP_INIT_DATASOURCE);
                            if ((null == value) || ((value is string) && (0 == (value as string).Length))) {
                                value = GetDataSourceValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_DATASOURCENAME); // MDAC 76248
                            }
                        }
                        else {
                            constr = this.OleDbConnectionStringValue;
                            value = (null != constr) ? constr.DataSource : ADP.StrEmpty;
                        }
                    }
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                }
                finally {
                    Bid.ScopeLeave(ref hscp);
                }
            }
        }

        internal bool IsOpen {
            get { return (null != GetOpenConnection()); }
        }

        internal OleDbTransaction LocalTransaction {
            set {
                OleDbConnectionInternal openConnection = GetOpenConnection();

                if (null != openConnection) {
                    openConnection.LocalTransaction = value;
                }
            }
        }

        [
        Browsable(true),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
       ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.OleDbConnection_Provider),
        ]
        public String Provider {
            get {
                Bid.Trace("<oledb.OleDbConnection.get_Provider|API> %d#\n", ObjectID);
                OleDbConnectionString constr = this.OleDbConnectionStringValue;
                string value = ((null != constr) ? constr.ConvertValueToString(ODB.Provider, null) : null);
                return ((null != value) ? value : ADP.StrEmpty);
            }
        }
        
        internal OleDbConnectionPoolGroupProviderInfo ProviderInfo {
            get {
                return (OleDbConnectionPoolGroupProviderInfo)PoolGroup.ProviderInfo;
            }
        }

        [
        ResDescriptionAttribute(Res.OleDbConnection_ServerVersion),
        ]
        override public string ServerVersion { // MDAC 55481
            get {
                return InnerConnection.ServerVersion;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.DbConnection_State),
        ]
        override public ConnectionState State {
            get {
                return InnerConnection.State;
            }
        }

        [
        EditorBrowsableAttribute(EditorBrowsableState.Advanced),
        ]
        public void ResetState() { // MDAC 58606
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<oledb.OleDbCommand.ResetState|API> %d#\n", ObjectID);
            try {
                if (IsOpen) {
                    object value = GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_CONNECTIONSTATUS);
                    if (value is Int32) {
                        int connectionStatus = (int) value;
                        switch (connectionStatus) {
                        case ODB.DBPROPVAL_CS_UNINITIALIZED: // provider closed on us
                        case ODB.DBPROPVAL_CS_COMMUNICATIONFAILURE: // broken connection
                            GetOpenConnection().DoomThisConnection();
                            NotifyWeakReference(OleDbReferenceCollection.Canceling); // MDAC 71435
                            Close();
                            break;

                        case ODB.DBPROPVAL_CS_INITIALIZED: // everything is okay
                            break;

                        default: // have to assume everything is okay
                            Debug.Assert(false, "Unknown 'Connection Status' value " + connectionStatus.ToString("G", CultureInfo.InvariantCulture));
                            break;
                        }
                    }
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        }

        [
        ResCategoryAttribute(Res.DataCategory_InfoMessage),
        ResDescriptionAttribute(Res.DbConnection_InfoMessage),
        ]
        public event OleDbInfoMessageEventHandler InfoMessage {
            add {
                Events.AddHandler(EventInfoMessage, value);
            }
            remove {
                Events.RemoveHandler(EventInfoMessage, value);
            }
        }

        internal UnsafeNativeMethods.ICommandText ICommandText() {
            Debug.Assert(null != GetOpenConnection(), "ICommandText closed");
            return GetOpenConnection().ICommandText();
        }

        private IDBPropertiesWrapper IDBProperties() {
            Debug.Assert(null != GetOpenConnection(), "IDBProperties closed");
            return GetOpenConnection().IDBProperties();
        }

        internal IOpenRowsetWrapper IOpenRowset() {
            Debug.Assert(null != GetOpenConnection(), "IOpenRowset closed");
            return GetOpenConnection().IOpenRowset();
        }

        internal int SqlSupport() {
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString SqlSupport");
            return this.OleDbConnectionStringValue.GetSqlSupport(this);
        }

        internal bool SupportMultipleResults() {
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString SupportMultipleResults");
            return this.OleDbConnectionStringValue.GetSupportMultipleResults(this);
        }

        internal bool SupportIRow(OleDbCommand cmd) { // MDAC 72902
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString SupportIRow");
            return this.OleDbConnectionStringValue.GetSupportIRow(this, cmd);
        }

        internal int QuotedIdentifierCase() { // MDAC 67385
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString QuotedIdentifierCase");
            
            int quotedIdentifierCase;
            object value = GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_QUOTEDIDENTIFIERCASE);
            if (value is Int32) {// not OleDbPropertyStatus
                quotedIdentifierCase =  (int) value;
            }
            else {
                quotedIdentifierCase = -1;
            }
            return quotedIdentifierCase;
        }

        new public OleDbTransaction BeginTransaction() {
            return BeginTransaction(IsolationLevel.Unspecified);
        }

        new public OleDbTransaction BeginTransaction(IsolationLevel isolationLevel) {
            return (OleDbTransaction)InnerConnection.BeginTransaction(isolationLevel);
        }

        override public void ChangeDatabase(string value) {
            OleDbConnection.ExecutePermission.Demand();

            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<oledb.OleDbConnection.ChangeDatabase|API> %d#, value='%ls'\n", ObjectID, value);
            try {
                CheckStateOpen(ADP.ChangeDatabase);
                if ((null == value) || (0 == value.Trim().Length)) { // MDAC 62679
                    throw ADP.EmptyDatabaseName();
                }
                SetDataSourcePropertyValue(OleDbPropertySetGuid.DataSource, ODB.DBPROP_CURRENTCATALOG, ODB.Current_Catalog, true, value);
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        }

        internal void CheckStateOpen(string method) {
            ConnectionState state = State;
            if (ConnectionState.Open != state) {
                throw ADP.OpenConnectionRequired(method, state);
            }
        }

        object ICloneable.Clone() {
            OleDbConnection clone = new OleDbConnection(this);
            Bid.Trace("<oledb.OleDbConnection.Clone|API> %d#, clone=%d#\n", ObjectID, clone.ObjectID);
            return clone;
        }

        override public void Close() {
            InnerConnection.CloseConnection(this, ConnectionFactory);
            // does not require GC.KeepAlive(this) because of OnStateChange
        }

        new public OleDbCommand CreateCommand() {
            return new OleDbCommand("", this);
        }

        private void DisposeMe(bool disposing) { // MDAC 65459
            if (disposing) { // release mananged objects
                if (DesignMode) {
                    // release the object pool in design-mode so that
                    // native MDAC can be properly released during shutdown
                    OleDbConnection.ReleaseObjectPool();
                }
            }
        }


        // suppress this message - we cannot use SafeHandle here. Also, see notes in the code (VSTFDEVDIV# 560355)
        [SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive")]
        override protected DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) {
            IntPtr hscp;

            Bid.ScopeEnter(out hscp, "<prov.OleDbConnection.BeginDbTransaction|API> %d#, isolationLevel=%d{ds.IsolationLevel}", ObjectID, (int)isolationLevel);
            try {

                DbTransaction transaction = InnerConnection.BeginTransaction(isolationLevel);

                // VSTFDEVDIV# 560355 - InnerConnection doesn't maintain a ref on the outer connection (this) and 
                //   subsequently leaves open the possibility that the outer connection could be GC'ed before the DbTransaction
                //   is fully hooked up (leaving a DbTransaction with a null connection property). Ensure that this is reachable
                //   until the completion of BeginTransaction with KeepAlive
                GC.KeepAlive(this);

                return transaction;
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        }

        public void EnlistDistributedTransaction(System.EnterpriseServices.ITransaction transaction) {
            EnlistDistributedTransactionHelper(transaction);
        }

        internal object GetDataSourcePropertyValue(Guid propertySet, int propertyID) {
            OleDbConnectionInternal connection = GetOpenConnection();
            return connection.GetDataSourcePropertyValue(propertySet, propertyID);
        }

        internal object GetDataSourceValue(Guid propertySet, int propertyID) {
            object value = GetDataSourcePropertyValue(propertySet, propertyID);
            if ((value is OleDbPropertyStatus) || Convert.IsDBNull(value)) {
                value = null;
            }
            return value;
        }

        private OleDbConnectionInternal GetOpenConnection() {
            DbConnectionInternal innerConnection = InnerConnection;
            return (innerConnection as OleDbConnectionInternal);
        }

        internal void GetLiteralQuotes(string method, out string quotePrefix, out string quoteSuffix) {
            CheckStateOpen(method);
            OleDbConnectionPoolGroupProviderInfo info = ProviderInfo;
            if (info.HasQuoteFix) {
                quotePrefix = info.QuotePrefix;
                quoteSuffix = info.QuoteSuffix;
            }
            else {
                OleDbConnectionInternal connection = GetOpenConnection();
                quotePrefix = connection.GetLiteralInfo(ODB.DBLITERAL_QUOTE_PREFIX);
                quoteSuffix = connection.GetLiteralInfo(ODB.DBLITERAL_QUOTE_SUFFIX);
                if (null == quotePrefix) {
                    quotePrefix = "";
                }
                if (null == quoteSuffix) {
                    quoteSuffix = quotePrefix;
                }
                info.SetQuoteFix(quotePrefix, quoteSuffix);
            }
        }

        public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions) { // MDAC 61846
            OleDbConnection.ExecutePermission.Demand();
            
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<oledb.OleDbConnection.GetOleDbSchemaTable|API> %d#, schema=%ls, restrictions\n", ObjectID, schema);
            try {
                CheckStateOpen(ADP.GetOleDbSchemaTable);
                OleDbConnectionInternal connection = GetOpenConnection();

                if (OleDbSchemaGuid.DbInfoLiterals == schema) {
                    if ((null == restrictions) || (0 == restrictions.Length)) {
                        return connection.BuildInfoLiterals();
                    }
                    throw ODB.InvalidRestrictionsDbInfoLiteral("restrictions");
                }
                else if (OleDbSchemaGuid.SchemaGuids == schema) {
                    if ((null == restrictions) || (0 == restrictions.Length)) {
                        return connection.BuildSchemaGuids();
                    }
                    throw ODB.InvalidRestrictionsSchemaGuids("restrictions");
                }
                else if (OleDbSchemaGuid.DbInfoKeywords == schema) {
                    if ((null == restrictions) || (0 == restrictions.Length)) {
                        return connection.BuildInfoKeywords();
                    }
                    throw ODB.InvalidRestrictionsDbInfoKeywords("restrictions");
                }

                if (connection.SupportSchemaRowset(schema)) {
                    return connection.GetSchemaRowset(schema, restrictions);
                }
                else {
                    using(IDBSchemaRowsetWrapper wrapper = connection.IDBSchemaRowset()) {
                        if (null == wrapper.Value) {
                            throw ODB.SchemaRowsetsNotSupported(Provider); // MDAC 72689
                        }
                    }
                    throw ODB.NotSupportedSchemaTable(schema, this); // MDAC 63279
                }
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        }

        internal DataTable GetSchemaRowset(Guid schema, object[] restrictions) {
            Debug.Assert(null != GetOpenConnection(), "GetSchemaRowset closed");
            return GetOpenConnection().GetSchemaRowset(schema, restrictions);
        }

        internal bool HasLiveReader(OleDbCommand cmd)  {
            bool result = false;
            OleDbConnectionInternal openConnection = GetOpenConnection();

            if (null != openConnection) {
                result = openConnection.HasLiveReader(cmd);
            }
            return result;
        }

        internal void OnInfoMessage(UnsafeNativeMethods.IErrorInfo errorInfo, OleDbHResult errorCode) {
            OleDbInfoMessageEventHandler handler = (OleDbInfoMessageEventHandler) Events[EventInfoMessage];
            if (null != handler) {
                try {
                    OleDbException exception = OleDbException.CreateException(errorInfo, errorCode, null);
                    OleDbInfoMessageEventArgs e = new OleDbInfoMessageEventArgs(exception);
                    if (Bid.TraceOn) {
                        Bid.Trace("<oledb.OledbConnection.OnInfoMessage|API|INFO> %d#, Message='%ls'\n", ObjectID, e.Message);
                    }
                    handler(this, e);
                }
                catch (Exception e) { // eat the exception
                    // 
                    if (!ADP.IsCatchableOrSecurityExceptionType(e)) {
                        throw;
                    }

                    ADP.TraceExceptionWithoutRethrow(e);
                }
            }
#if DEBUG
            else {
                OleDbException exception = OleDbException.CreateException(errorInfo, errorCode, null);
                Bid.Trace("<oledb.OledbConnection.OnInfoMessage|API|INFO> %d#, Message='%ls'\n", ObjectID, exception.Message);
            }
#endif
        }
        
        override public void Open() {
            InnerConnection.OpenConnection(this, ConnectionFactory);
            
            // SQLBUDT #276132 - need to manually enlist in some cases, because
            // native OLE DB doesn't know about SysTx transactions.
            if ((0!=(ODB.DBPROPVAL_OS_TXNENLISTMENT & ((OleDbConnectionString)(this.ConnectionOptions)).OleDbServices)) 
                        && ADP.NeedManualEnlistment()) {
                GetOpenConnection().EnlistTransactionInternal(SysTx.Transaction.Current);
            }
        }

        internal void SetDataSourcePropertyValue(Guid propertySet, int propertyID, string description, bool required, object value) {
            CheckStateOpen(ADP.SetProperties);
            OleDbHResult hr;
            using(IDBPropertiesWrapper idbProperties = IDBProperties()) {
                using(DBPropSet propSet = DBPropSet.CreateProperty(propertySet, propertyID, required, value)) {
                    
                    Bid.Trace("<oledb.IDBProperties.SetProperties|API|OLEDB> %d#\n", ObjectID);
                    hr = idbProperties.Value.SetProperties(propSet.PropertySetCount, propSet);
                    Bid.Trace("<oledb.IDBProperties.SetProperties|API|OLEDB|RET> %08X{HRESULT}\n", hr);
                    
                    if (hr < 0) {
                        Exception e = OleDbConnection.ProcessResults(hr, null, this);
                        if (OleDbHResult.DB_E_ERRORSOCCURRED == hr) {

                            StringBuilder builder = new StringBuilder();
                            Debug.Assert(1 == propSet.PropertySetCount, "too many PropertySets");

                            tagDBPROP[] dbprops = propSet.GetPropertySet(0, out propertySet);
                            Debug.Assert(1 == dbprops.Length, "too many Properties");

                            ODB.PropsetSetFailure(builder, description, dbprops[0].dwStatus);

                            e = ODB.PropsetSetFailure(builder.ToString(), e);
                        }
                        if (null != e) {
                            throw e;
                        }
                    }
                    else {
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                    }
                }
            }
        }

        internal bool SupportSchemaRowset(Guid schema) {
            return GetOpenConnection().SupportSchemaRowset(schema);
        }

        internal OleDbTransaction ValidateTransaction(OleDbTransaction transaction, string method) {
            return GetOpenConnection().ValidateTransaction(transaction, method);
        }

        static internal Exception ProcessResults(OleDbHResult hresult, OleDbConnection connection, object src) {
            if ((0 <= (int)hresult) && ((null == connection) || (null == connection.Events[EventInfoMessage]))) {
                SafeNativeMethods.Wrapper.ClearErrorInfo();
                return null;
            }

            // ErrorInfo object is to be checked regardless the hresult returned by the function called
            Exception e = null;
            UnsafeNativeMethods.IErrorInfo errorInfo = null;
            OleDbHResult hr = UnsafeNativeMethods.GetErrorInfo(0, out errorInfo);  // 0 - IErrorInfo exists, 1 - no IErrorInfo
            if ((OleDbHResult.S_OK == hr) && (null != errorInfo)) {
                if (hresult < 0) {
                    // 





                    e = OleDbException.CreateException(errorInfo, hresult, null);
                    //}

                    if (OleDbHResult.DB_E_OBJECTOPEN == hresult) {
                        e = ADP.OpenReaderExists(e);
                    }

                    ResetState(connection);
                }
                else if (null != connection) {
                    connection.OnInfoMessage(errorInfo, hresult);
                }
                else {
                    Bid.Trace("<oledb.OledbConnection|WARN|INFO> ErrorInfo available, but not connection %08X{HRESULT}\n", hresult);
                }
                Marshal.ReleaseComObject(errorInfo);
            }
            else if (0 < hresult) {
                // @devnote: OnInfoMessage with no ErrorInfo
                Bid.Trace("<oledb.OledbConnection|ERR|INFO> ErrorInfo not available %08X{HRESULT}\n", hresult);
            }
            else if ((int)hresult < 0) {
                e = ODB.NoErrorInformation((null != connection) ? connection.Provider : null, hresult, null); // OleDbException

                ResetState(connection);
            }
            if (null != e) {
                ADP.TraceExceptionAsReturnValue(e);
            }
            return e;
        }

        // @devnote: should be multithread safe
        static public void ReleaseObjectPool() {
            (new OleDbPermission(PermissionState.Unrestricted)).Demand();

            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<oledb.OleDbConnection.ReleaseObjectPool|API>\n");
            try {
                OleDbConnectionString.ReleaseObjectPool();
                OleDbConnectionInternal.ReleaseObjectPool();
                OleDbConnectionFactory.SingletonInstance.ClearAllPools();
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        }

        static private void ResetState(OleDbConnection connection) {
            if (null != connection) {
                connection.ResetState();
            }
        }
    }
}
