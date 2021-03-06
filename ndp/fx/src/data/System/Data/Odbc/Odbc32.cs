//------------------------------------------------------------------------------
// <copyright file="Odbc32.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------


namespace System.Data.Odbc {

    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Text;

    internal static class ODBC {

        static internal Exception ConnectionClosed() {
            return ADP.InvalidOperation(Res.GetString(Res.Odbc_ConnectionClosed));
        }

        static internal Exception OpenConnectionNoOwner() {
            return ADP.InvalidOperation(Res.GetString(Res.Odbc_OpenConnectionNoOwner));
        }

        static internal Exception UnknownSQLType(ODBC32.SQL_TYPE sqltype) {
            return ADP.Argument(Res.GetString(Res.Odbc_UnknownSQLType, sqltype.ToString()));
        }
        static internal Exception ConnectionStringTooLong() {
            return ADP.Argument(Res.GetString(Res.OdbcConnection_ConnectionStringTooLong,  ODBC32.MAX_CONNECTION_STRING_LENGTH));
        }
        static internal ArgumentException GetSchemaRestrictionRequired() {
            return ADP.Argument(Res.GetString(Res.ODBC_GetSchemaRestrictionRequired));
        }
        static internal ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, int value) {
            return ADP.ArgumentOutOfRange(Res.GetString(Res.ODBC_NotSupportedEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        }
        static internal ArgumentOutOfRangeException NotSupportedCommandType(CommandType value) {
#if DEBUG
            switch(value) {
            case CommandType.Text:
            case CommandType.StoredProcedure:
                Debug.Assert(false, "valid CommandType " + value.ToString());
                break;
            case CommandType.TableDirect:
                break;
            default:
                Debug.Assert(false, "invalid CommandType " + value.ToString());
                break;
            }
#endif
            return ODBC.NotSupportedEnumerationValue(typeof(CommandType), (int)value);
        }
        static internal ArgumentOutOfRangeException NotSupportedIsolationLevel(IsolationLevel value) {
#if DEBUG
            switch(value) {
            case IsolationLevel.Unspecified:
            case IsolationLevel.ReadUncommitted:
            case IsolationLevel.ReadCommitted:
            case IsolationLevel.RepeatableRead:
            case IsolationLevel.Serializable:
            case IsolationLevel.Snapshot:
                Debug.Assert(false, "valid IsolationLevel " + value.ToString());
                break;
            case IsolationLevel.Chaos:
                break;
            default:
                Debug.Assert(false, "invalid IsolationLevel " + value.ToString());
                break;
            }
#endif
            return ODBC.NotSupportedEnumerationValue(typeof(IsolationLevel), (int)value);
        }

        static internal InvalidOperationException NoMappingForSqlTransactionLevel(int value) {
            return ADP.DataAdapter(Res.GetString(Res.Odbc_NoMappingForSqlTransactionLevel, value.ToString(CultureInfo.InvariantCulture)));
        }

        static internal Exception NegativeArgument() {
            return ADP.Argument(Res.GetString(Res.Odbc_NegativeArgument));
        }
        static internal Exception CantSetPropertyOnOpenConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.Odbc_CantSetPropertyOnOpenConnection));
        }
        static internal Exception CantEnableConnectionpooling(ODBC32.RetCode retcode) {
            return ADP.DataAdapter(Res.GetString(Res.Odbc_CantEnableConnectionpooling, ODBC32.RetcodeToString(retcode)));
        }
        static internal Exception CantAllocateEnvironmentHandle(ODBC32.RetCode retcode) {
            return ADP.DataAdapter(Res.GetString(Res.Odbc_CantAllocateEnvironmentHandle, ODBC32.RetcodeToString(retcode)));
        }
        static internal Exception FailedToGetDescriptorHandle(ODBC32.RetCode retcode) {
            return ADP.DataAdapter(Res.GetString(Res.Odbc_FailedToGetDescriptorHandle, ODBC32.RetcodeToString(retcode)));
        }
        static internal Exception NotInTransaction() {
            return ADP.InvalidOperation(Res.GetString(Res.Odbc_NotInTransaction));
        }
        static internal Exception UnknownOdbcType(OdbcType odbctype) {
            return ADP.InvalidEnumerationValue(typeof(OdbcType), (int) odbctype);
        }
        internal const string Pwd = "pwd";

        static internal void TraceODBC(int level, string method, ODBC32.RetCode retcode) {
            Bid.TraceSqlReturn("<odbc|API|ODBC|RET> %08X{SQLRETURN}, method=%ls\n", retcode, method);
        }

        internal static short ShortStringLength(string inputString) {
            return checked((short)ADP.StringLength(inputString));
        }
    }


    internal static class ODBC32 {

        internal enum SQL_HANDLE : short {
            ENV                 = 1,
            DBC                 = 2,
            STMT                = 3,
            DESC                = 4,
        }

        // from .\public\sdk\inc\sqlext.h: and .\public\sdk\inc\sql.h
        // must be public because it is serialized by OdbcException
        [Serializable]
        public enum RETCODE : int { // must be int instead of short for Everett OdbcException Serializablity.
            SUCCESS             = 0,
            SUCCESS_WITH_INFO   = 1,
            ERROR               = -1,
            INVALID_HANDLE      = -2,
            NO_DATA             = 100,
        }

        // must be public because it is serialized by OdbcException
        internal enum RetCode : short {
            SUCCESS             = 0,
            SUCCESS_WITH_INFO   = 1,
            ERROR               = -1,
            INVALID_HANDLE      = -2,
            NO_DATA             = 100,
        }

        internal static string RetcodeToString(RetCode retcode) {
            switch (retcode) {
                case RetCode.SUCCESS:           return "SUCCESS";
                case RetCode.SUCCESS_WITH_INFO: return "SUCCESS_WITH_INFO";
                case RetCode.ERROR:             return "ERROR";
                case RetCode.INVALID_HANDLE:    return "INVALID_HANDLE";
                case RetCode.NO_DATA:           return "NO_DATA";
                default:
                    Debug.Assert(false, "Unknown enumerator passed to RetcodeToString method");
                    goto case RetCode.ERROR;
            }
        }



        internal enum SQL_CONVERT : ushort {
            BIGINT              = 53,
            BINARY              = 54,
            BIT                 = 55,
            CHAR                = 56,
            DATE                = 57,
            DECIMAL             = 58,
            DOUBLE              = 59,
            FLOAT               = 60,
            INTEGER             = 61,
            LONGVARCHAR         = 62,
            NUMERIC             = 63,
            REAL                = 64,
            SMALLINT            = 65,
            TIME                = 66,
            TIMESTAMP           = 67,
            TINYINT             = 68,
            VARBINARY           = 69,
            VARCHAR             = 70,
            LONGVARBINARY       = 71,
        }

        [Flags]
        internal enum SQL_CVT {
            CHAR                = 0x00000001,
            NUMERIC             = 0x00000002,
            DECIMAL             = 0x00000004,
            INTEGER             = 0x00000008,
            SMALLINT            = 0x00000010,
            FLOAT               = 0x00000020,
            REAL                = 0x00000040,
            DOUBLE              = 0x00000080,
            VARCHAR             = 0x00000100,
            LONGVARCHAR         = 0x00000200,
            BINARY              = 0x00000400,
            VARBINARY           = 0x00000800,
            BIT                 = 0x00001000,
            TINYINT             = 0x00002000,
            BIGINT              = 0x00004000,
            DATE                = 0x00008000,
            TIME                = 0x00010000,
            TIMESTAMP           = 0x00020000,
            LONGVARBINARY       = 0x00040000,
            INTERVAL_YEAR_MONTH = 0x00080000,
            INTERVAL_DAY_TIME   = 0x00100000,
            WCHAR               = 0x00200000,
            WLONGVARCHAR        = 0x00400000,
            WVARCHAR            = 0x00800000,
            GUID                = 0x01000000,
        }

        internal enum STMT : short {
            CLOSE               =  0,
            DROP                =  1,
            UNBIND              =  2,
            RESET_PARAMS        =  3,
        }

        internal enum SQL_MAX{
            NUMERIC_LEN     =   16,
        }

        internal enum SQL_IS{
            POINTER         =   -4,
            INTEGER         =   -6,
            UINTEGER        =   -5,
            SMALLINT        =   -8,
        }


        //SQL Server specific defines
        //
        internal enum SQL_HC                          // from Odbcss.h
        {
            OFF                 = 0,                //  FOR BROWSE columns are hidden
            ON                  = 1,                //  FOR BROWSE columns are exposed
        }

        internal enum SQL_NB                          // from Odbcss.h
        {
            OFF                 = 0,                //  NO_BROWSETABLE is off
            ON                  = 1,                //  NO_BROWSETABLE is on
        }

        //  SQLColAttributes driver specific defines.
        //  SQLSet/GetDescField driver specific defines.
        //  Microsoft has 1200 thru 1249 reserved for Microsoft SQL Server driver usage.
        //
        internal enum SQL_CA_SS                       // from Odbcss.h
        {
            BASE                =   1200,           // SQL_CA_SS_BASE

            COLUMN_HIDDEN       =   BASE + 11,      //  Column is hidden (FOR BROWSE)
            COLUMN_KEY          =   BASE + 12,      //  Column is key column (FOR BROWSE)
            VARIANT_TYPE        =   BASE + 15,
            VARIANT_SQL_TYPE    =   BASE + 16,
            VARIANT_SERVER_TYPE =   BASE + 17,

        }
        internal enum SQL_SOPT_SS                     // from Odbcss.h
        {
            BASE                =   1225,           // SQL_SOPT_SS_BASE
            HIDDEN_COLUMNS      =   BASE + 2,       // Expose FOR BROWSE hidden columns
            NOBROWSETABLE       =   BASE + 3,       // Set NOBROWSETABLE option
        }

        internal const Int16 SQL_COMMIT              =   0;      //Commit
        internal const Int16 SQL_ROLLBACK            =   1;      //Abort

        static internal readonly IntPtr SQL_AUTOCOMMIT_OFF = ADP.PtrZero;
        static internal readonly IntPtr SQL_AUTOCOMMIT_ON = new IntPtr(1);

        internal enum SQL_TRANSACTION
        {
            READ_UNCOMMITTED    =   0x00000001,
            READ_COMMITTED      =   0x00000002,
            REPEATABLE_READ     =   0x00000004,
            SERIALIZABLE        =   0x00000008,
            SNAPSHOT            =   0x00000020, // VSDD 414121: SQL_TXN_SS_SNAPSHOT == 0x20 (sqlncli.h)
        }

        internal enum SQL_PARAM
        {
// unused   TYPE_UNKNOWN        =   0,          // SQL_PARAM_TYPE_UNKNOWN
            INPUT               =   1,          // SQL_PARAM_INPUT
            INPUT_OUTPUT        =   2,          // SQL_PARAM_INPUT_OUTPUT
// unused   RESULT_COL          =   3,          // SQL_RESULT_COL
            OUTPUT              =   4,          // SQL_PARAM_OUTPUT
            RETURN_VALUE        =   5,          // SQL_RETURN_VALUE
        }

        // SQL_API_* values
        // there are a gillion of these I am only defining the ones currently needed
        // others can be added as needed
        internal enum SQL_API : ushort
        {
            SQLCOLUMNS          = 40,
            SQLEXECDIRECT       = 11,
            SQLGETTYPEINFO      = 47,
            SQLPROCEDURECOLUMNS = 66,
            SQLPROCEDURES       = 67,
            SQLSTATISTICS       = 53,
            SQLTABLES           = 54,
        }


        internal enum SQL_DESC : short
        {
            // from sql.h (ODBCVER >= 3.0)
            //
            COUNT                  = 1001,
            TYPE                   = 1002,
            LENGTH                 = 1003,
            OCTET_LENGTH_PTR       = 1004,
            PRECISION              = 1005,
            SCALE                  = 1006,
            DATETIME_INTERVAL_CODE = 1007,
            NULLABLE               = 1008,
            INDICATOR_PTR          = 1009,
            DATA_PTR               = 1010,
            NAME                   = 1011,
            UNNAMED                = 1012,
            OCTET_LENGTH           = 1013,
            ALLOC_TYPE             = 1099,

            // from sqlext.h (ODBCVER >= 3.0)
            //
            CONCISE_TYPE           = SQL_COLUMN.TYPE,
            DISPLAY_SIZE           = SQL_COLUMN.DISPLAY_SIZE,
            UNSIGNED               = SQL_COLUMN.UNSIGNED,
            UPDATABLE              = SQL_COLUMN.UPDATABLE,
            AUTO_UNIQUE_VALUE      = SQL_COLUMN.AUTO_INCREMENT,

            TYPE_NAME              = SQL_COLUMN.TYPE_NAME,
            TABLE_NAME             = SQL_COLUMN.TABLE_NAME,
            SCHEMA_NAME            = SQL_COLUMN.OWNER_NAME,
            CATALOG_NAME           = SQL_COLUMN.QUALIFIER_NAME,

            BASE_COLUMN_NAME       = 22,
            BASE_TABLE_NAME        = 23,
        }

        // ODBC version 2.0 style attributes
        // All IdentifierValues are ODBC 1.0 unless marked differently
        //
        internal enum SQL_COLUMN
        {
            COUNT                  = 0,
            NAME                   = 1,
            TYPE                   = 2,
            LENGTH                 = 3,
            PRECISION              = 4,
            SCALE                  = 5,
            DISPLAY_SIZE           = 6,
            NULLABLE               = 7,
            UNSIGNED               = 8,
            MONEY                  = 9,
            UPDATABLE              = 10,
            AUTO_INCREMENT         = 11,
            CASE_SENSITIVE         = 12,
            SEARCHABLE             = 13,
            TYPE_NAME              = 14,
            TABLE_NAME             = 15,    // (ODBC 2.0)
            OWNER_NAME             = 16,    // (ODBC 2.0)
            QUALIFIER_NAME         = 17,    // (ODBC 2.0)
            LABEL                  = 18,
        }

        internal enum SQL_GROUP_BY
        {
            NOT_SUPPORTED               = 0,    // SQL_GB_NOT_SUPPORTED
            GROUP_BY_EQUALS_SELECT      = 1,    // SQL_GB_GROUP_BY_EQUALS_SELECT
            GROUP_BY_CONTAINS_SELECT    = 2,    // SQL_GB_GROUP_BY_CONTAINS_SELECT
            NO_RELATION                 = 3,    // SQL_GB_NO_RELATION
            COLLATE                     = 4,    // SQL_GB_COLLATE - added in ODBC 3.0
        }

        // values from sqlext.h
        internal enum SQL_SQL92_RELATIONAL_JOIN_OPERATORS
        {
            CORRESPONDING_CLAUSE   = 0x00000001,    // SQL_SRJO_CORRESPONDING_CLAUSE
            CROSS_JOIN             = 0x00000002,    // SQL_SRJO_CROSS_JOIN
            EXCEPT_JOIN            = 0x00000004,    // SQL_SRJO_EXCEPT_JOIN
            FULL_OUTER_JOIN        = 0x00000008,    // SQL_SRJO_FULL_OUTER_JOIN
            INNER_JOIN             = 0x00000010,    // SQL_SRJO_INNER_JOIN
            INTERSECT_JOIN         = 0x00000020,    // SQL_SRJO_INTERSECT_JOIN
            LEFT_OUTER_JOIN        = 0x00000040,    // SQL_SRJO_LEFT_OUTER_JOIN
            NATURAL_JOIN           = 0x00000080,    // SQL_SRJO_NATURAL_JOIN
            RIGHT_OUTER_JOIN       = 0x00000100,    // SQL_SRJO_RIGHT_OUTER_JOIN
            UNION_JOIN             = 0x00000200,    // SQL_SRJO_UNION_JOIN
        }

        // values from sql.h
        internal enum SQL_OJ_CAPABILITIES
        {
            LEFT   = 0x00000001,    // SQL_OJ_LEFT
            RIGHT = 0x00000002,    // SQL_OJ_RIGHT
            FULL  = 0x00000004,    // SQL_OJ_FULL
            NESTED  = 0x00000008,    // SQL_OJ_NESTED
            NOT_ORDERED  = 0x00000010,    // SQL_OJ_NOT_ORDERED
            INNER  = 0x00000020,    // SQL_OJ_INNER
            ALL_COMPARISON_OPS = 0x00000040,  //SQL_OJ_ALLCOMPARISION+OPS
        }

        internal enum SQL_UPDATABLE
        {
            READONLY                = 0,    // SQL_ATTR_READ_ONLY
            WRITE                   = 1,    // SQL_ATTR_WRITE
            READWRITE_UNKNOWN       = 2,    // SQL_ATTR_READWRITE_UNKNOWN
        }

        internal enum SQL_IDENTIFIER_CASE
        {
            UPPER       = 1,    // SQL_IC_UPPER
            LOWER       = 2,    // SQL_IC_LOWER
            SENSITIVE   = 3,    // SQL_IC_SENSITIVE
            MIXED       = 4,    // SQL_IC_MIXED
        }

        // Uniqueness parameter in the SQLStatistics function
        internal enum SQL_INDEX : short
        {
            UNIQUE      = 0,
            ALL          = 1,
        }

        // Reserved parameter in the SQLStatistics function
        internal enum SQL_STATISTICS_RESERVED : short
        {
            QUICK       = 0,                // SQL_QUICK
            ENSURE      = 1,                // SQL_ENSURE
        }

        // Identifier type parameter in the SQLSpecialColumns function
        internal enum SQL_SPECIALCOLS : ushort
        {
            BEST_ROWID      = 1,            // SQL_BEST_ROWID
            ROWVER          = 2,            // SQL_ROWVER
        }

        // Scope parameter in the SQLSpecialColumns function
        internal enum SQL_SCOPE : ushort
        {
            CURROW          = 0,            // SQL_SCOPE_CURROW
            TRANSACTION      = 1,           // SQL_SCOPE_TRANSACTION
            SESSION          = 2,           // SQL_SCOPE_SESSION
        }

        internal enum SQL_NULLABILITY : ushort
        {
            NO_NULLS    = 0,                // SQL_NO_NULLS
            NULLABLE    = 1,                // SQL_NULLABLE
            UNKNOWN     = 2,                // SQL_NULLABLE_UNKNOWN
        }

        internal enum SQL_SEARCHABLE
        {
            UNSEARCHABLE        = 0,        // SQL_UNSEARCHABLE
            LIKE_ONLY           = 1,        // SQL_LIKE_ONLY
            ALL_EXCEPT_LIKE     = 2,        // SQL_ALL_EXCEPT_LIKE
            SEARCHABLE          = 3,        // SQL_SEARCHABLE
        }

        internal enum SQL_UNNAMED
        {
            NAMED    = 0,                   // SQL_NAMED
            UNNAMED    = 1,                 // SQL_UNNAMED
        }
// todo:move
// internal constants
// not odbc specific
//
        internal enum HANDLER
        {
            IGNORE                  = 0x00000000,
            THROW                   = 0x00000001,
        }

        // values for SQLStatistics TYPE column
        internal enum SQL_STATISTICSTYPE
        {
            TABLE_STAT          = 0,                    // TABLE Statistics
            INDEX_CLUSTERED     = 1,                    // CLUSTERED index statistics
            INDEX_HASHED        = 2,                    // HASHED index statistics
            INDEX_OTHER         = 3,                    // OTHER index statistics
        }

        // values for SQLProcedures PROCEDURE_TYPE column
        internal enum SQL_PROCEDURETYPE
        {
            UNKNOWN         = 0,                    // procedure is of unknow type
            PROCEDURE       = 1,                    // procedure is a procedure
            FUNCTION        = 2,                    // procedure is a function
        }

// private constants
// to define data types (see below)
//
        private const Int32 SIGNED_OFFSET   =    -20;    // SQL_SIGNED_OFFSET
        private const Int32 UNSIGNED_OFFSET =    -22;    // SQL_UNSIGNED_OFFSET

        //C Data Types - used when getting data (SQLGetData)
        internal enum SQL_C : short
        {
            CHAR            =    1,                     //SQL_C_CHAR
            WCHAR           =   -8,                     //SQL_C_WCHAR
            SLONG           =    4 + SIGNED_OFFSET,     //SQL_C_LONG+SQL_SIGNED_OFFSET
//          ULONG           =    4 + UNSIGNED_OFFSET,   //SQL_C_LONG+SQL_UNSIGNED_OFFSET
            SSHORT          =    5 + SIGNED_OFFSET,     //SQL_C_SSHORT+SQL_SIGNED_OFFSET
//          USHORT          =    5 + UNSIGNED_OFFSET,   //SQL_C_USHORT+SQL_UNSIGNED_OFFSET
            REAL            =    7,                     //SQL_C_REAL
            DOUBLE          =    8,                     //SQL_C_DOUBLE
            BIT             =   -7,                     //SQL_C_BIT
//          STINYINT        =   -6 + SIGNED_OFFSET,     //SQL_C_STINYINT+SQL_SIGNED_OFFSET
            UTINYINT        =   -6 + UNSIGNED_OFFSET,   //SQL_C_UTINYINT+SQL_UNSIGNED_OFFSET
            SBIGINT         =   -5 + SIGNED_OFFSET,     //SQL_C_SBIGINT+SQL_SIGNED_OFFSET
            UBIGINT         =   -5 + UNSIGNED_OFFSET,   //SQL_C_UBIGINT+SQL_UNSIGNED_OFFSET
            BINARY          =   -2,                     //SQL_C_BINARY
            TIMESTAMP       =   11,                     //SQL_C_TIMESTAMP

            TYPE_DATE       =   91,                     //SQL_C_TYPE_DATE
            TYPE_TIME       =   92,                     //SQL_C_TYPE_TIME
            TYPE_TIMESTAMP  =   93,                     //SQL_C_TYPE_TIMESTAMP

            NUMERIC         =    2,                     //SQL_C_NUMERIC
            GUID            =   -11,                    //SQL_C_GUID
            DEFAULT         =   99,                     //SQL_C_DEFAULT
            ARD_TYPE        =   -99,                    //SQL_ARD_TYPE
        }

        //SQL Data Types - returned as column types (SQLColAttribute)
        internal enum SQL_TYPE : short
        {
            CHAR            =   SQL_C.CHAR,             //SQL_CHAR
            VARCHAR         =   12,                     //SQL_VARCHAR
            LONGVARCHAR     =   -1,                     //SQL_LONGVARCHAR
            WCHAR           =   SQL_C.WCHAR,            //SQL_WCHAR
            WVARCHAR        =   -9,                     //SQL_WVARCHAR
            WLONGVARCHAR    =   -10,                    //SQL_WLONGVARCHAR
            DECIMAL         =   3,                      //SQL_DECIMAL
            NUMERIC         =   SQL_C.NUMERIC,          //SQL_NUMERIC
            SMALLINT        =   5,                      //SQL_SMALLINT
            INTEGER         =   4,                      //SQL_INTEGER
            REAL            =   SQL_C.REAL,             //SQL_REAL
            FLOAT           =   6,                      //SQL_FLOAT
            DOUBLE          =   SQL_C.DOUBLE,           //SQL_DOUBLE
            BIT             =   SQL_C.BIT,              //SQL_BIT
            TINYINT         =   -6,                     //SQL_TINYINT
            BIGINT          =   -5,                     //SQL_BIGINT
            BINARY          =   SQL_C.BINARY,           //SQL_BINARY
            VARBINARY       =   -3,                     //SQL_VARBINARY
            LONGVARBINARY   =   -4,                     //SQL_LONGVARBINARY

//          DATE            =   9,                      //SQL_DATE
            TYPE_DATE       =   SQL_C.TYPE_DATE,        //SQL_TYPE_DATE
            TYPE_TIME       =   SQL_C.TYPE_TIME,        //SQL_TYPE_TIME
            TIMESTAMP       =   SQL_C.TIMESTAMP,        //SQL_TIMESTAMP
            TYPE_TIMESTAMP  =   SQL_C.TYPE_TIMESTAMP,   //SQL_TYPE_TIMESTAMP


            GUID            =   SQL_C.GUID,             //SQL_GUID

        //  from odbcss.h in mdac 9.0 sources!
        //  Driver specific SQL type defines.
        //  Microsoft has -150 thru -199 reserved for Microsoft SQL Server driver usage.
        //
            SS_VARIANT =                     -150,
            SS_UDT =                         -151,
            SS_XML =                         -152,
            SS_UTCDATETIME =                 -153,
            SS_TIME_EX =                     -154,
        }

        internal const Int16 SQL_ALL_TYPES = 0;
        static internal readonly IntPtr  SQL_HANDLE_NULL  = ADP.PtrZero;
        internal const Int32  SQL_NULL_DATA     = -1;   // sql.h
        internal const Int32  SQL_NO_TOTAL      = -4;   // sqlext.h

        internal const Int32  SQL_DEFAULT_PARAM= -5;
//      internal const Int32  SQL_IGNORE         = -6;

// column ordinals for SQLProcedureColumns result set
// this column ordinals are not defined in any c/c++ header but in the ODBC Programmer's Reference under SQLProcedureColumns
//
        internal const Int32  COLUMN_NAME = 4;
        internal const Int32  COLUMN_TYPE = 5;
        internal const Int32  DATA_TYPE = 6;
        internal const Int32  COLUMN_SIZE = 8;
        internal const Int32  DECIMAL_DIGITS = 10;
        internal const Int32  NUM_PREC_RADIX = 11;

        internal enum SQL_ATTR
        {
            APP_ROW_DESC        =   10010,              // (ODBC 3.0)
            APP_PARAM_DESC      =   10011,              // (ODBC 3.0)
            IMP_ROW_DESC        =   10012,              // (ODBC 3.0)
            IMP_PARAM_DESC      =   10013,              // (ODBC 3.0)
            METADATA_ID         =   10014,              // (ODBC 3.0)
            ODBC_VERSION        =   200,
            CONNECTION_POOLING  =   201,
            AUTOCOMMIT          =   102,
            TXN_ISOLATION       =   108,
            CURRENT_CATALOG     =   109,
            LOGIN_TIMEOUT       =   103,
            QUERY_TIMEOUT       =   0,                  // from sqlext.h
            CONNECTION_DEAD     =   1209,               // from sqlext.h

            // from sqlncli.h
            SQL_COPT_SS_BASE          = 1200,
            SQL_COPT_SS_ENLIST_IN_DTC = (SQL_COPT_SS_BASE + 7),
            SQL_COPT_SS_TXN_ISOLATION = (SQL_COPT_SS_BASE + 27), // Used to set/get any driver-specific or ODBC-defined TXN iso level
        }

        //SQLGetInfo
        internal enum SQL_INFO : ushort
        {
            DATA_SOURCE_NAME            = 2,    // SQL_DATA_SOURCE_NAME in sql.h
            SERVER_NAME                 = 13,   // SQL_SERVER_NAME in sql.h
            DRIVER_NAME                 = 6,    // SQL_DRIVER_NAME as defined in sqlext.h
            DRIVER_VER                  = 7,    // SQL_DRIVER_VER as defined in sqlext.h
            ODBC_VER                    = 10,   // SQL_ODBC_VER as defined in sqlext.h
            SEARCH_PATTERN_ESCAPE       = 14,   // SQL_SEARCH_PATTERN_ESCAPE from sql.h
            DBMS_VER                    = 18,
            DBMS_NAME                   = 17,   // SQL_DBMS_NAME as defined in sqlext.h
            IDENTIFIER_CASE             = 28,   // SQL_IDENTIFIER_CASE from sql.h
            IDENTIFIER_QUOTE_CHAR       = 29,   // SQL_IDENTIFIER_QUOTE_CHAR from sql.h
            CATALOG_NAME_SEPARATOR      = 41,   // SQL_CATALOG_NAME_SEPARATOR
            DRIVER_ODBC_VER             = 77,   // SQL_DRIVER_ODBC_VER as defined in sqlext.h
            GROUP_BY                    = 88,   // SQL_GROUP_BY as defined in  sqlext.h
            KEYWORDS                    = 89,   // SQL_KEYWORDS as defined in sqlext.h
            ORDER_BY_COLUMNS_IN_SELECT  = 90,   // SQL_ORDER_BY_COLUNS_IN_SELECT in sql.h
            QUOTED_IDENTIFIER_CASE      = 93,   // SQL_QUOTED_IDENTIFIER_CASE in sqlext.h
            SQL_OJ_CAPABILITIES_30 = 115, //SQL_OJ_CAPABILITIES from sql.h
            SQL_OJ_CAPABILITIES_20 = 65003, //SQL_OJ_CAPABILITIES from sqlext.h
            SQL_SQL92_RELATIONAL_JOIN_OPERATORS = 161, //SQL_SQL92_RELATIONAL_JOIN_OPERATORS from sqlext.h

        }

        static internal readonly IntPtr  SQL_OV_ODBC3      =  new IntPtr(3);
        internal const Int32  SQL_NTS                 = -3;       //flags for null-terminated string

        //Pooling
        static internal readonly IntPtr  SQL_CP_OFF                        =  new IntPtr(0);       //Connection Pooling disabled
        static internal readonly IntPtr  SQL_CP_ONE_PER_DRIVER  =  new IntPtr(1);       //One pool per driver
        static internal readonly IntPtr  SQL_CP_ONE_PER_HENV     =  new IntPtr(2);       //One pool per environment

        /* values for SQL_ATTR_CONNECTION_DEAD */
        internal const Int32  SQL_CD_TRUE             = 1;
        internal const Int32  SQL_CD_FALSE            = 0;

        internal const Int32 SQL_DTC_DONE = 0;
        internal const Int32 SQL_IS_POINTER = -4;
        internal const Int32 SQL_IS_PTR = 1;

        internal enum SQL_DRIVER
        {
            NOPROMPT            = 0,
            COMPLETE            = 1,
            PROMPT              = 2,
            COMPLETE_REQUIRED   = 3,
        }

// todo:move
// internal const. not odbc specific
//
        // Connection string max length
        internal const Int32 MAX_CONNECTION_STRING_LENGTH    = 1024;

        // Column set for SQLPrimaryKeys
        internal enum SQL_PRIMARYKEYS : short
        {
/*
            CATALOGNAME         = 1,                    // TABLE_CAT
            SCHEMANAME          = 2,                    // TABLE_SCHEM
            TABLENAME           = 3,                    // TABLE_NAME
*/
            COLUMNNAME          = 4,                    // COLUMN_NAME
/*
            KEY_SEQ             = 5,                    // KEY_SEQ
            PKNAME              = 6,                    // PK_NAME
*/
        }

        // Column set for SQLStatistics
        internal enum SQL_STATISTICS : short
        {
/*
            CATALOGNAME         = 1,                    // TABLE_CAT
            SCHEMANAME          = 2,                    // TABLE_SCHEM
            TABLENAME           = 3,                    // TABLE_NAME
            NONUNIQUE           = 4,                    // NON_UNIQUE
            INDEXQUALIFIER      = 5,                    // INDEX_QUALIFIER
*/
            INDEXNAME           = 6,                    // INDEX_NAME
/*
            TYPE                = 7,                    // TYPE
*/
            ORDINAL_POSITION    = 8,                    // ORDINAL_POSITION
            COLUMN_NAME         = 9,                    // COLUMN_NAME
/*
            ASC_OR_DESC         = 10,                   // ASC_OR_DESC
            CARDINALITY         = 11,                   // CARDINALITY
            PAGES               = 12,                   // PAGES
            FILTER_CONDITION    = 13,                   // FILTER_CONDITION
*/
        }

        // Column set for SQLSpecialColumns
        internal enum SQL_SPECIALCOLUMNSET : short
        {
/*
            SCOPE               = 1,                    // SCOPE
*/
            COLUMN_NAME         = 2,                    // COLUMN_NAME
/*
            DATA_TYPE           = 3,                    // DATA_TYPE
            TYPE_NAME           = 4,                    // TYPE_NAME
            COLUMN_SIZE         = 5,                    // COLUMN_SIZE
            BUFFER_LENGTH       = 6,                    // BUFFER_LENGTH
            DECIMAL_DIGITS      = 7,                    // DECIMAL_DIGITS
            PSEUDO_COLUMN       = 8,                    // PSEUDO_COLUMN
*/
        }

        internal const short SQL_DIAG_SQLSTATE = 4;
        internal const short SQL_RESULT_COL = 3;

        // Helpers
        static internal OdbcErrorCollection GetDiagErrors(string source, OdbcHandle hrHandle, RetCode retcode) {
            OdbcErrorCollection errors = new OdbcErrorCollection();
            GetDiagErrors(errors, source, hrHandle, retcode);
            return errors;
        }

        static internal void GetDiagErrors(OdbcErrorCollection errors, string source, OdbcHandle hrHandle, RetCode retcode) {
            Debug.Assert(retcode!=ODBC32.RetCode.INVALID_HANDLE, "retcode must never be ODBC32.RetCode.INVALID_HANDLE");
            if (RetCode.SUCCESS != retcode) {
                Int32       NativeError;
                Int16       iRec            = 0;
                Int16       cchActual       = 0;

                StringBuilder message = new StringBuilder(1024);
                string sqlState;
                bool moreerrors = true;
                while(moreerrors) {

                    ++iRec;

                    retcode = hrHandle.GetDiagnosticRecord(iRec, out sqlState, message, out NativeError, out cchActual);
                    if ((RetCode.SUCCESS_WITH_INFO == retcode) && (message.Capacity-1 < cchActual)) {
                        message.Capacity = cchActual+1;
                        retcode = hrHandle.GetDiagnosticRecord(iRec, out sqlState, message, out NativeError, out cchActual);
                    }

                    //Note: SUCCESS_WITH_INFO from SQLGetDiagRec would be because
                    //the buffer is not large enough for the error string.
                    moreerrors = (retcode == RetCode.SUCCESS || retcode == RetCode.SUCCESS_WITH_INFO);
                    if(moreerrors) {
                        //Sets up the InnerException as well...
                        errors.Add(new OdbcError(
                            source,
                            message.ToString(),
                            sqlState,
                            NativeError
                            )
                        );
                    }
                }
            }
        }
    }

   sealed internal class TypeMap { // MDAC 68988
//      private TypeMap                                           (OdbcType odbcType,         DbType dbType,                Type type,        ODBC32.SQL_TYPE sql_type,       ODBC32.SQL_C sql_c,          ODBC32.SQL_C param_sql_c,   int bsize, int csize, bool signType)
//      ---------------                                            ------------------         --------------                ----------        -------------------------       -------------------          -------------------------   -----------------------
        static private  readonly TypeMap _BigInt     = new TypeMap(OdbcType.BigInt,           DbType.Int64,                 typeof(Int64),    ODBC32.SQL_TYPE.BIGINT,         ODBC32.SQL_C.SBIGINT,        ODBC32.SQL_C.SBIGINT,         8, 20, true);
        static private  readonly TypeMap _Binary     = new TypeMap(OdbcType.Binary,           DbType.Binary,                typeof(byte[]),   ODBC32.SQL_TYPE.BINARY,         ODBC32.SQL_C.BINARY,         ODBC32.SQL_C.BINARY,         -1, -1, false);
        static private  readonly TypeMap _Bit        = new TypeMap(OdbcType.Bit,              DbType.Boolean,               typeof(Boolean),  ODBC32.SQL_TYPE.BIT,            ODBC32.SQL_C.BIT,            ODBC32.SQL_C.BIT,             1,  1, false);
        static internal readonly TypeMap _Char       = new TypeMap(OdbcType.Char,             DbType.AnsiStringFixedLength, typeof(String),   ODBC32.SQL_TYPE.CHAR,           ODBC32.SQL_C.WCHAR,          ODBC32.SQL_C.CHAR,           -1, -1, false);
        static private  readonly TypeMap _DateTime   = new TypeMap(OdbcType.DateTime,         DbType.DateTime,              typeof(DateTime), ODBC32.SQL_TYPE.TYPE_TIMESTAMP, ODBC32.SQL_C.TYPE_TIMESTAMP, ODBC32.SQL_C.TYPE_TIMESTAMP, 16, 23, false);
        static private  readonly TypeMap _Date       = new TypeMap(OdbcType.Date,             DbType.Date,                  typeof(DateTime), ODBC32.SQL_TYPE.TYPE_DATE,      ODBC32.SQL_C.TYPE_DATE,      ODBC32.SQL_C.TYPE_DATE,       6, 10, false);
        static private  readonly TypeMap _Time       = new TypeMap(OdbcType.Time,             DbType.Time,                  typeof(TimeSpan), ODBC32.SQL_TYPE.TYPE_TIME,      ODBC32.SQL_C.TYPE_TIME,      ODBC32.SQL_C.TYPE_TIME,       6, 12, false);
        static private  readonly TypeMap _Decimal    = new TypeMap(OdbcType.Decimal,          DbType.Decimal,               typeof(Decimal),  ODBC32.SQL_TYPE.DECIMAL,        ODBC32.SQL_C.NUMERIC,        ODBC32.SQL_C.NUMERIC,        19, ADP.DecimalMaxPrecision28, false);
//        static private  readonly TypeMap _Currency   = new TypeMap(OdbcType.Decimal,          DbType.Currency,              typeof(Decimal),  ODBC32.SQL_TYPE.DECIMAL,        ODBC32.SQL_C.NUMERIC,        ODBC32.SQL_C.NUMERIC,        19, ADP.DecimalMaxPrecision28, false);
        static private  readonly TypeMap _Double     = new TypeMap(OdbcType.Double,           DbType.Double,                typeof(Double),   ODBC32.SQL_TYPE.DOUBLE,         ODBC32.SQL_C.DOUBLE,         ODBC32.SQL_C.DOUBLE,          8, 15, false);
        static internal readonly TypeMap _Image      = new TypeMap(OdbcType.Image,            DbType.Binary,                typeof(Byte[]),   ODBC32.SQL_TYPE.LONGVARBINARY,  ODBC32.SQL_C.BINARY,         ODBC32.SQL_C.BINARY,         -1, -1, false);
        static private  readonly TypeMap _Int        = new TypeMap(OdbcType.Int,              DbType.Int32,                 typeof(Int32),    ODBC32.SQL_TYPE.INTEGER,        ODBC32.SQL_C.SLONG,          ODBC32.SQL_C.SLONG,           4, 10, true);
        static private  readonly TypeMap _NChar      = new TypeMap(OdbcType.NChar,            DbType.StringFixedLength,     typeof(String),   ODBC32.SQL_TYPE.WCHAR,          ODBC32.SQL_C.WCHAR,          ODBC32.SQL_C.WCHAR,          -1, -1, false);
        static internal readonly TypeMap _NText      = new TypeMap(OdbcType.NText,            DbType.String,                typeof(String),   ODBC32.SQL_TYPE.WLONGVARCHAR,   ODBC32.SQL_C.WCHAR,          ODBC32.SQL_C.WCHAR,          -1, -1, false);
        static private  readonly TypeMap _Numeric    = new TypeMap(OdbcType.Numeric,          DbType.Decimal,               typeof(Decimal),  ODBC32.SQL_TYPE.NUMERIC,        ODBC32.SQL_C.NUMERIC,        ODBC32.SQL_C.NUMERIC,        19, ADP.DecimalMaxPrecision28, false);
        static internal readonly TypeMap _NVarChar   = new TypeMap(OdbcType.NVarChar,         DbType.String,                typeof(String),   ODBC32.SQL_TYPE.WVARCHAR,       ODBC32.SQL_C.WCHAR,          ODBC32.SQL_C.WCHAR,          -1, -1, false);
        static private  readonly TypeMap _Real       = new TypeMap(OdbcType.Real,             DbType.Single,                typeof(Single),   ODBC32.SQL_TYPE.REAL,           ODBC32.SQL_C.REAL,           ODBC32.SQL_C.REAL,            4,  7, false);
        static private  readonly TypeMap _UniqueId   = new TypeMap(OdbcType.UniqueIdentifier, DbType.Guid,                  typeof(Guid),     ODBC32.SQL_TYPE.GUID,           ODBC32.SQL_C.GUID,           ODBC32.SQL_C.GUID,           16, 36, false);
        static private  readonly TypeMap _SmallDT    = new TypeMap(OdbcType.SmallDateTime,    DbType.DateTime,              typeof(DateTime), ODBC32.SQL_TYPE.TYPE_TIMESTAMP, ODBC32.SQL_C.TYPE_TIMESTAMP, ODBC32.SQL_C.TYPE_TIMESTAMP, 16, 23, false);
        static private  readonly TypeMap _SmallInt   = new TypeMap(OdbcType.SmallInt,         DbType.Int16,                 typeof(Int16),    ODBC32.SQL_TYPE.SMALLINT,       ODBC32.SQL_C.SSHORT,         ODBC32.SQL_C.SSHORT,          2,  5, true);
        static internal readonly TypeMap _Text       = new TypeMap(OdbcType.Text,             DbType.AnsiString,            typeof(String),   ODBC32.SQL_TYPE.LONGVARCHAR,    ODBC32.SQL_C.WCHAR,          ODBC32.SQL_C.CHAR,           -1, -1, false);
        static private  readonly TypeMap _Timestamp  = new TypeMap(OdbcType.Timestamp,        DbType.Binary,                typeof(Byte[]),   ODBC32.SQL_TYPE.BINARY,         ODBC32.SQL_C.BINARY,         ODBC32.SQL_C.BINARY,         -1, -1, false);
        static private  readonly TypeMap _TinyInt    = new TypeMap(OdbcType.TinyInt,          DbType.Byte,                  typeof(Byte),     ODBC32.SQL_TYPE.TINYINT,        ODBC32.SQL_C.UTINYINT,       ODBC32.SQL_C.UTINYINT,        1,  3, true);
        static private  readonly TypeMap _VarBinary  = new TypeMap(OdbcType.VarBinary,        DbType.Binary,                typeof(Byte[]),   ODBC32.SQL_TYPE.VARBINARY,      ODBC32.SQL_C.BINARY,         ODBC32.SQL_C.BINARY,         -1, -1, false);
        static internal readonly TypeMap _VarChar    = new TypeMap(OdbcType.VarChar,          DbType.AnsiString,            typeof(String),   ODBC32.SQL_TYPE.VARCHAR,        ODBC32.SQL_C.WCHAR,          ODBC32.SQL_C.CHAR,           -1, -1, false);
        static private  readonly TypeMap _Variant    = new TypeMap(OdbcType.Binary,           DbType.Binary,                typeof(object),   ODBC32.SQL_TYPE.SS_VARIANT,     ODBC32.SQL_C.BINARY,         ODBC32.SQL_C.BINARY,         -1, -1, false);
        static private  readonly TypeMap _UDT        = new TypeMap(OdbcType.Binary,           DbType.Binary,                typeof(object),   ODBC32.SQL_TYPE.SS_UDT,         ODBC32.SQL_C.BINARY,         ODBC32.SQL_C.BINARY,         -1, -1, false);
        static private  readonly TypeMap _XML        = new TypeMap(OdbcType.Text,             DbType.AnsiString,            typeof(String),   ODBC32.SQL_TYPE.LONGVARCHAR,    ODBC32.SQL_C.WCHAR,          ODBC32.SQL_C.CHAR,           -1, -1, false);

        internal readonly OdbcType _odbcType;
        internal readonly DbType   _dbType;
        internal readonly Type     _type;

        internal readonly ODBC32.SQL_TYPE _sql_type;
        internal readonly ODBC32.SQL_C    _sql_c;
        internal readonly ODBC32.SQL_C    _param_sql_c;


        internal readonly int _bufferSize;  // fixed length byte size to reserve for buffer
        internal readonly int _columnSize;  // column size passed to SQLBindParameter
        internal readonly bool _signType;   // this type may be has signature information

        private TypeMap(OdbcType odbcType, DbType dbType, Type type, ODBC32.SQL_TYPE sql_type, ODBC32.SQL_C sql_c, ODBC32.SQL_C param_sql_c, int bsize, int csize, bool signType) {
            _odbcType = odbcType;
            _dbType = dbType;
            _type = type;

            _sql_type = sql_type;
            _sql_c = sql_c;
            _param_sql_c = param_sql_c; // alternative sql_c type for parameters

            _bufferSize = bsize;
            _columnSize = csize;
            _signType = signType;
        }

        static internal TypeMap FromOdbcType(OdbcType odbcType) {
            switch(odbcType) {
            case OdbcType.BigInt: return _BigInt;
            case OdbcType.Binary: return _Binary;
            case OdbcType.Bit: return _Bit;
            case OdbcType.Char: return _Char;
            case OdbcType.DateTime: return _DateTime;
            case OdbcType.Date: return _Date;
            case OdbcType.Time: return _Time;
            case OdbcType.Double: return _Double;
            case OdbcType.Decimal: return _Decimal;
            case OdbcType.Image: return _Image;
            case OdbcType.Int: return _Int;
            case OdbcType.NChar: return _NChar;
            case OdbcType.NText: return _NText;
            case OdbcType.Numeric: return _Numeric;
            case OdbcType.NVarChar: return _NVarChar;
            case OdbcType.Real: return _Real;
            case OdbcType.UniqueIdentifier: return _UniqueId;
            case OdbcType.SmallDateTime: return _SmallDT;
            case OdbcType.SmallInt: return _SmallInt;
            case OdbcType.Text: return _Text;
            case OdbcType.Timestamp: return _Timestamp;
            case OdbcType.TinyInt: return _TinyInt;
            case OdbcType.VarBinary: return _VarBinary;
            case OdbcType.VarChar: return _VarChar;
            default: throw ODBC.UnknownOdbcType(odbcType);
            }
        }

        static internal TypeMap FromDbType(DbType dbType) {
            switch(dbType) {
            case DbType.AnsiString: return _VarChar;
            case DbType.AnsiStringFixedLength: return _Char;
            case DbType.Binary:     return _VarBinary;
            case DbType.Byte:       return _TinyInt;
            case DbType.Boolean:    return _Bit;
            case DbType.Currency:   return _Decimal;
//            case DbType.Currency:   return _Currency;
            case DbType.Date:       return _Date;
            case DbType.Time:       return _Time;
            case DbType.DateTime:   return _DateTime;
            case DbType.Decimal:    return _Decimal;
            case DbType.Double:     return _Double;
            case DbType.Guid:       return _UniqueId;
            case DbType.Int16:      return _SmallInt;
            case DbType.Int32:      return _Int;
            case DbType.Int64:      return _BigInt;
            case DbType.Single:     return _Real;
            case DbType.String:     return _NVarChar;
            case DbType.StringFixedLength: return _NChar;
            case DbType.Object:
            case DbType.SByte:
            case DbType.UInt16:
            case DbType.UInt32:
            case DbType.UInt64:
            case DbType.VarNumeric:
            default: throw ADP.DbTypeNotSupported(dbType, typeof(OdbcType));
            }
        }

        static internal TypeMap FromSystemType(Type dataType) {
            switch(Type.GetTypeCode(dataType)) {
            case TypeCode.Empty:     throw ADP.InvalidDataType(TypeCode.Empty);
            case TypeCode.Object:
                if (dataType == typeof(System.Byte[])) {
                    return _VarBinary;
                }
                else if (dataType == typeof(System.Guid)) {
                    return _UniqueId;
                }
                else if (dataType == typeof(System.TimeSpan)) {
                    return _Time;
                }
                else if (dataType == typeof(System.Char[])) {
                    return _NVarChar;
                }
                throw ADP.UnknownDataType(dataType);

            case TypeCode.DBNull:    throw ADP.InvalidDataType(TypeCode.DBNull);
            case TypeCode.Boolean:   return _Bit;

// devnote: Char is actually not supported. Our _Char type is actually a fixed length string, not a single character
//            case TypeCode.Char:      return _Char;
            case TypeCode.SByte:     return _SmallInt;
            case TypeCode.Byte:      return _TinyInt;
            case TypeCode.Int16:     return _SmallInt;
            case TypeCode.UInt16:    return _Int;
            case TypeCode.Int32:     return _Int;
            case TypeCode.UInt32:    return _BigInt;
            case TypeCode.Int64:     return _BigInt;
            case TypeCode.UInt64:    return _Numeric;
            case TypeCode.Single:    return _Real;
            case TypeCode.Double:    return _Double;
            case TypeCode.Decimal:   return _Numeric;
            case TypeCode.DateTime:  return _DateTime;

            case TypeCode.Char:
            case TypeCode.String:    return _NVarChar;

            default:                 throw ADP.UnknownDataTypeCode(dataType, Type.GetTypeCode(dataType));
            }
        }

        static internal TypeMap FromSqlType(ODBC32.SQL_TYPE sqltype) {
            switch(sqltype) {
            case ODBC32.SQL_TYPE.CHAR: return _Char;
            case ODBC32.SQL_TYPE.VARCHAR: return _VarChar;
            case ODBC32.SQL_TYPE.LONGVARCHAR: return _Text;
            case ODBC32.SQL_TYPE.WCHAR: return _NChar;
            case ODBC32.SQL_TYPE.WVARCHAR: return _NVarChar;
            case ODBC32.SQL_TYPE.WLONGVARCHAR: return _NText;
            case ODBC32.SQL_TYPE.DECIMAL: return _Decimal;
            case ODBC32.SQL_TYPE.NUMERIC: return _Numeric;
            case ODBC32.SQL_TYPE.SMALLINT: return _SmallInt;
            case ODBC32.SQL_TYPE.INTEGER: return _Int;
            case ODBC32.SQL_TYPE.REAL: return _Real;
            case ODBC32.SQL_TYPE.FLOAT: return _Double;
            case ODBC32.SQL_TYPE.DOUBLE: return _Double;
            case ODBC32.SQL_TYPE.BIT: return _Bit;
            case ODBC32.SQL_TYPE.TINYINT: return _TinyInt;
            case ODBC32.SQL_TYPE.BIGINT: return _BigInt;
            case ODBC32.SQL_TYPE.BINARY: return _Binary;
            case ODBC32.SQL_TYPE.VARBINARY: return _VarBinary;
            case ODBC32.SQL_TYPE.LONGVARBINARY: return _Image;
            case ODBC32.SQL_TYPE.TYPE_DATE: return _Date;
            case ODBC32.SQL_TYPE.TYPE_TIME: return _Time;
            case ODBC32.SQL_TYPE.TIMESTAMP:
            case ODBC32.SQL_TYPE.TYPE_TIMESTAMP: return _DateTime;
            case ODBC32.SQL_TYPE.GUID: return _UniqueId;
            case ODBC32.SQL_TYPE.SS_VARIANT: return _Variant;
            case ODBC32.SQL_TYPE.SS_UDT: return _UDT;
            case ODBC32.SQL_TYPE.SS_XML: return _XML;

            case ODBC32.SQL_TYPE.SS_UTCDATETIME:
            case ODBC32.SQL_TYPE.SS_TIME_EX:
                throw ODBC.UnknownSQLType(sqltype);
            default:
                throw ODBC.UnknownSQLType(sqltype);
            }
        }

        // Upgrade integer datatypes to missinterpretaion of the highest bit
        // (e.g. 0xff could be 255 if unsigned but is -1 if signed)
        //
        static internal TypeMap UpgradeSignedType(TypeMap typeMap, bool unsigned) {
            // upgrade unsigned types to be able to hold data that has the highest bit set
            //
            if (unsigned == true) {
                switch (typeMap._dbType) {
                    case DbType.Int64:
                        return _Decimal;        // upgrade to decimal
                    case DbType.Int32:
                        return _BigInt;         // upgrade to 64 bit
                    case DbType.Int16:
                        return _Int;            // upgrade to 32 bit
                    default:
                        return typeMap;
                } // end switch
            }
            else {
                switch (typeMap._dbType) {
                    case DbType.Byte:
                        return _SmallInt;       // upgrade to 16 bit
                    default:
                        return typeMap;
                } // end switch
            }
        } // end UpgradeSignedType
    }
}
