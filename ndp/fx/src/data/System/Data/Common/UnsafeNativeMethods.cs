//------------------------------------------------------------------------------
// <copyright file="UnsafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

using System;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;

namespace System.Data.Common {

    [SuppressUnmanagedCodeSecurityAttribute()]
    internal static class UnsafeNativeMethods {

        //
        // ODBC32
        //
        [DllImport(ExternDll.Odbc32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLAllocHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/IntPtr InputHandle,
            /*SQLHANDLE* */out IntPtr OutputHandle);

        [DllImport(ExternDll.Odbc32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLAllocHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/OdbcHandle InputHandle,
            /*SQLHANDLE* */out IntPtr OutputHandle);


        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLBindCol(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/UInt16 ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/HandleRef TargetValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */IntPtr StrLen_or_Ind);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLBindCol(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/UInt16 ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/IntPtr TargetValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */IntPtr StrLen_or_Ind);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLBindParameter(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/UInt16 ParameterNumber,
            /*SQLSMALLINT*/Int16 ParamDirection,
            /*SQLSMALLINT*/ODBC32.SQL_C SQLCType,
            /*SQLSMALLINT*/Int16 SQLType,
            /*SQLULEN*/IntPtr    cbColDef,
            /*SQLSMALLINT*/IntPtr ibScale,
            /*SQLPOINTER*/HandleRef rgbValue,
            /*SQLLEN*/IntPtr BufferLength,
            /*SQLLEN* */HandleRef StrLen_or_Ind);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLCancel(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLCloseCursor(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLColAttributeW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/Int16 ColumnNumber,
            /*SQLUSMALLINT*/Int16 FieldIdentifier,
            /*SQLPOINTER*/CNativeBuffer CharacterAttribute,
            /*SQLSMALLINT*/Int16 BufferLength,
            /*SQLSMALLINT* */out Int16 StringLength,
            /*SQLPOINTER*/out IntPtr NumericAttribute);

// note: in sql.h this is defined differently for the 64Bit platform.
// However, for us the code is not different for SQLPOINTER or SQLLEN ...
// frome sql.h:
// #ifdef _WIN64
// SQLRETURN  SQL_API SQLColAttribute (SQLHSTMT StatementHandle,
//            SQLUSMALLINT ColumnNumber, SQLUSMALLINT FieldIdentifier,
//            SQLPOINTER CharacterAttribute, SQLSMALLINT BufferLength,
//            SQLSMALLINT *StringLength, SQLLEN *NumericAttribute);
// #else
// SQLRETURN  SQL_API SQLColAttribute (SQLHSTMT StatementHandle,
//            SQLUSMALLINT ColumnNumber, SQLUSMALLINT FieldIdentifier,
//            SQLPOINTER CharacterAttribute, SQLSMALLINT BufferLength,
//            SQLSMALLINT *StringLength, SQLPOINTER NumericAttribute);
// #endif


        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLColumnsW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/Int16 NameLen1,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/Int16 NameLen2,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/Int16 NameLen3,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string ColumnName,
            /*SQLSMALLINT*/Int16 NameLen4);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLDisconnect(
            /*SQLHDBC*/IntPtr ConnectionHandle);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLDriverConnectW(
            /*SQLHDBC*/OdbcConnectionHandle hdbc,
            /*SQLHWND*/IntPtr hwnd,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string               connectionstring,
            /*SQLSMALLINT*/Int16            cbConnectionstring,
            /*SQLCHAR* */IntPtr               connectionstringout,
            /*SQLSMALLINT*/Int16            cbConnectionstringoutMax,
            /*SQLSMALLINT* */out Int16        cbConnectionstringout,
            /*SQLUSMALLINT*/Int16           fDriverCompletion);

        [DllImport(ExternDll.Odbc32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLEndTran(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/IntPtr Handle,
            /*SQLSMALLINT*/Int16 CompletionType);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLExecDirectW(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string   StatementText,
            /*SQLINTEGER*/Int32 TextLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLExecute(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLFetch(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle);

        [DllImport(ExternDll.Odbc32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLFreeHandle(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE    HandleType,
            /*SQLHSTMT*/IntPtr      StatementHandle);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLFreeStmt(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle,
            /*SQLUSMALLINT*/ODBC32.STMT Option);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/byte[] Value,
            /*SQLINTEGER*/Int32 BufferLength,
            /*SQLINTEGER* */out Int32 StringLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetData(
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/UInt16 ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_C TargetType,
            /*SQLPOINTER*/CNativeBuffer TargetValue,
            /*SQLLEN*/IntPtr BufferLength, // sql.h differs from MSDN
            /*SQLLEN* */out IntPtr StrLen_or_Ind);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetDescFieldW (
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLUSMALLINT*/Int16 RecNumber,
            /*SQLUSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
            /*SQLPOINTER*/CNativeBuffer ValuePointer,
            /*SQLINTEGER*/Int32 BufferLength,
            /*SQLINTEGER* */out Int32 StringLength);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetDiagRecW(
            /*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
            /*SQLHANDLE*/OdbcHandle Handle,
            /*SQLSMALLINT*/Int16 RecNumber,
            /*SQLCHAR* */  StringBuilder rchState,
            /*SQLINTEGER* */out Int32 NativeError,
            /*SQLCHAR* */StringBuilder MessageText,
            /*SQLSMALLINT*/Int16 BufferLength,
            /*SQLSMALLINT* */out Int16 TextLength);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetDiagFieldW(
           /*SQLSMALLINT*/ ODBC32.SQL_HANDLE HandleType,
           /*SQLHANDLE*/   OdbcHandle Handle,
           /*SQLSMALLINT*/ Int16     RecNumber,
           /*SQLSMALLINT*/ Int16     DiagIdentifier,
           [MarshalAs(UnmanagedType.LPWStr)]
           /*SQLPOINTER*/  StringBuilder    rchState,
           /*SQLSMALLINT*/ Int16     BufferLength,
           /*SQLSMALLINT* */ out Int16 StringLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetFunctions(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_API fFunction,
            /*SQLUSMALLINT* */out Int16 pfExists);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetInfoW(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
            /*SQLPOINTER*/byte[] rgbInfoValue,
            /*SQLSMALLINT*/Int16 cbInfoValueMax,
            /*SQLSMALLINT* */out Int16 pcbInfoValue);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetInfoW(
            /*SQLHBDC*/OdbcConnectionHandle hdbc,
            /*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
            /*SQLPOINTER*/byte[] rgbInfoValue,
            /*SQLSMALLINT*/Int16 cbInfoValueMax,
            /*SQLSMALLINT* */IntPtr pcbInfoValue);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetStmtAttrW(
            /*SQLHSTMT*/OdbcStatementHandle          StatementHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR      Attribute,
            /*SQLPOINTER*/out IntPtr     Value,
            /*SQLINTEGER*/Int32      BufferLength,
            /*SQLINTEGER*/out Int32  StringLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetTypeInfo(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle,
            /*SQLSMALLINT*/Int16 fSqlType);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLMoreResults(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLNumResultCols(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle,
            /*SQLSMALLINT* */out Int16 ColumnCount);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLPrepareW(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string   StatementText,
            /*SQLINTEGER*/Int32 TextLength);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLPrimaryKeysW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/Int16 NameLen1,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */ string SchemaName,
            /*SQLSMALLINT*/Int16 NameLen2,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/Int16 NameLen3);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLProcedureColumnsW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string CatalogName,
            /*SQLSMALLINT*/Int16 NameLen1,
            [In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string SchemaName,
            /*SQLSMALLINT*/Int16 NameLen2,
            [In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string ProcName,
            /*SQLSMALLINT*/Int16 NameLen3,
            [In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string ColumnName,
            /*SQLSMALLINT*/Int16 NameLen4);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLProceduresW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string CatalogName,
            /*SQLSMALLINT*/Int16 NameLen1,
            [In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string SchemaName,
            /*SQLSMALLINT*/Int16 NameLen2,
            [In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string ProcName,
            /*SQLSMALLINT*/Int16 NameLen3);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLRowCount(
            /*SQLHSTMT*/OdbcStatementHandle  StatementHandle,
            /*SQLLEN* */out IntPtr RowCount);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/System.Transactions.IDtcTransaction Value,
            /*SQLINTEGER*/Int32 StringLength);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/string Value,
            /*SQLINTEGER*/Int32 StringLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
            /*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/Int32 StringLength);

        [DllImport(ExternDll.Odbc32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW( // used only for AutoCommitOn
            /*SQLHBDC*/IntPtr ConnectionHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/Int32 StringLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetDescFieldW (
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLSMALLINT*/Int16 ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_DESC  FieldIdentifier,
            /*SQLPOINTER*/HandleRef CharacterAttribute,
            /*SQLINTEGER*/Int32 BufferLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetDescFieldW (
            /*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
            /*SQLSMALLINT*/Int16 ColumnNumber,
            /*SQLSMALLINT*/ODBC32.SQL_DESC  FieldIdentifier,
            /*SQLPOINTER*/IntPtr CharacterAttribute,
            /*SQLINTEGER*/Int32 BufferLength);

        [DllImport(ExternDll.Odbc32)]
        // user can set SQL_ATTR_CONNECTION_POOLING attribute with envHandle = null, this attribute is process-level attribute
        [ResourceExposure(ResourceScope.Process)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetEnvAttr(
            /*SQLHENV*/OdbcEnvironmentHandle EnvironmentHandle,
            /*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
            /*SQLPOINTER*/IntPtr Value,
            /*SQLINTEGER*/ODBC32.SQL_IS StringLength);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetStmtAttrW(
            /*SQLHSTMT*/OdbcStatementHandle          StatementHandle,
            /*SQLINTEGER*/Int32      Attribute,
            /*SQLPOINTER*/IntPtr     Value,
            /*SQLINTEGER*/Int32      StringLength);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSpecialColumnsW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            /*SQLUSMALLINT*/ODBC32.SQL_SPECIALCOLS IdentifierType,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/Int16 NameLen1,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/Int16 NameLen2,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/Int16 NameLen3,
            /*SQLUSMALLINT*/ODBC32.SQL_SCOPE Scope,
            /*SQLUSMALLINT*/ ODBC32.SQL_NULLABILITY Nullable);

        [DllImport(ExternDll.Odbc32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLStatisticsW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/Int16 NameLen1,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/Int16 NameLen2,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/Int16 NameLen3,
            /*SQLUSMALLINT*/Int16 Unique,
            /*SQLUSMALLINT*/Int16 Reserved);

        [DllImport(ExternDll.Odbc32)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern /*SQLRETURN*/ODBC32.RetCode SQLTablesW (
            /*SQLHSTMT*/OdbcStatementHandle StatementHandle,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string CatalogName,
            /*SQLSMALLINT*/Int16 NameLen1,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string SchemaName,
            /*SQLSMALLINT*/Int16 NameLen2,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string TableName,
            /*SQLSMALLINT*/Int16 NameLen3,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            /*SQLCHAR* */string TableType,
            /*SQLSMALLINT*/Int16 NameLen4);

        //
        // Oleaut32
        //

        [DllImport(ExternDll.Oleaut32, CharSet=CharSet.Unicode, PreserveSig=true)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern System.Data.OleDb.OleDbHResult GetErrorInfo(
            [In] Int32 dwReserved,
            [Out, MarshalAs(UnmanagedType.Interface)] out IErrorInfo ppIErrorInfo);

        [Guid("00000567-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ADORecordConstruction {

            [return:MarshalAs(UnmanagedType.Interface)] object get_Row ();

            //void put_Row(
            //    [In, MarshalAs(UnmanagedType.Interface)] object pRow);

            //void put_ParentRow(
            //    [In, MarshalAs(UnmanagedType.Interface)]object pRow);
        }

        [Guid("00000283-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ADORecordsetConstruction {

            [return:MarshalAs(UnmanagedType.Interface)] object get_Rowset();

            [ Obsolete("not used", true)] void put_Rowset (/*deleted parameters signature*/);

            /*[return:MarshalAs(UnmanagedType.SysInt)]*/ IntPtr get_Chapter();

            //[[PreserveSig]
            //iint put_Chapter (
            //         [In]
            //         IntPtr pcRefCount);

            //[[PreserveSig]
            //iint get_RowPosition (
            //         [Out, MarshalAs(UnmanagedType.Interface)]
            //         out object ppRowPos);

            //[[PreserveSig]
            //iint put_RowPosition (
            //         [In, MarshalAs(UnmanagedType.Interface)]
            //         object pRowPos);
        }

        [Guid("0000050E-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface Recordset15  {
            [ Obsolete("not used", true)] void get_Properties(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_AbsolutePosition(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_AbsolutePosition(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void putref_ActiveConnection(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_ActiveConnection(/*deleted parameters signature*/);

            /*[return:MarshalAs(UnmanagedType.Variant)]*/object get_ActiveConnection();

            [ Obsolete("not used", true)] void get_BOF(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Bookmark(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_Bookmark(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_CacheSize(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_CacheSize(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_CursorType(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_CursorType(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_EOF(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Fields(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_LockType(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_LockType(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_MaxRecords(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_MaxRecords(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_RecordCount(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void putref_Source(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_Source(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Source(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void AddNew(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void CancelUpdate(/*deleted parameters signature*/);

            [PreserveSig] System.Data.OleDb.OleDbHResult Close();

            [ Obsolete("not used", true)] void Delete(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void GetRows(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void Move(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void MoveNext();
            [ Obsolete("not used", true)] void MovePrevious();
            [ Obsolete("not used", true)] void MoveFirst();
            [ Obsolete("not used", true)] void MoveLast();
            [ Obsolete("not used", true)] void Open(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void Requery(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void _xResync(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void Update(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_AbsolutePage(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_AbsolutePage(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_EditMode(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Filter(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_Filter(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_PageCount(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_PageSize(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_PageSize(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Sort(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_Sort(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Status(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_State(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void _xClone(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void UpdateBatch(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void CancelBatch(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_CursorLocation(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_CursorLocation(/*deleted parameters signature*/);

            [PreserveSig] System.Data.OleDb.OleDbHResult NextRecordset(
                [Out]out object RecordsAffected,
                [Out, MarshalAs(UnmanagedType.Interface)] out object ppiRs);

            //[ Obsolete("not used", true)] void Supports(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void get_Collect(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void put_Collect(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void get_MarshalOptions(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void put_MarshalOptions(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void Find(/*deleted parameters signature*/);
        }

        [Guid("00000562-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface _ADORecord {
            [ Obsolete("not used", true)] void get_Properties(/*deleted parameters signature*/);

            /*[return:MarshalAs(UnmanagedType.Variant)]*/object get_ActiveConnection();

            [ Obsolete("not used", true)] void put_ActiveConnection(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void putref_ActiveConnection(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_State(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Source(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_Source(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void putref_Source(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_Mode(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void put_Mode(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void get_ParentURL(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void MoveRecord(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void CopyRecord(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void DeleteRecord(/*deleted parameters signature*/);
            [ Obsolete("not used", true)] void Open(/*deleted parameters signature*/);

            [PreserveSig] System.Data.OleDb.OleDbHResult Close();

            //[ Obsolete("not used", true)] void get_Fields(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void get_RecordType(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void GetChildren(/*deleted parameters signature*/);
            //[ Obsolete("not used", true)] void Cancel();
        }

/*
        typedef ULONGLONG           DBLENGTH;

        // Offset within a rowset
        typedef LONGLONG                DBROWOFFSET;

        // Number of rows
        typedef LONGLONG                DBROWCOUNT;

        typedef ULONGLONG           DBCOUNTITEM;

        // Ordinal (column number, etc.)
        typedef ULONGLONG           DBORDINAL;

        typedef LONGLONG                DB_LORDINAL;

        // Bookmarks
        typedef ULONGLONG           DBBKMARK;
        // Offset in the buffer

        typedef ULONGLONG           DBBYTEOFFSET;
        // Reference count of each row/accessor  handle

        typedef ULONG               DBREFCOUNT;

        // Parameters
        typedef ULONGLONG           DB_UPARAMS;

        typedef LONGLONG                DB_LPARAMS;

        // hash values corresponding to the elements (bookmarks)
        typedef DWORDLONG           DBHASHVALUE;

        // For reserve
        typedef DWORDLONG           DB_DWRESERVE;

        typedef LONGLONG                DB_LRESERVE;

        typedef ULONGLONG           DB_URESERVE;
*/



        [ComImport, Guid("0C733A8C-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
        internal interface IAccessor {

            [ Obsolete("not used", true)] void AddRefAccessor(/*deleted parameters signature*/);

            /*[local]
            HRESULT CreateAccessor(
                [in] DBACCESSORFLAGS dwAccessorFlags,
                [in] DBCOUNTITEM cBindings,
                [in, size_is(cBindings)] const DBBINDING rgBindings[],
                [in] DBLENGTH cbRowSize,
                [out] HACCESSOR * phAccessor,
                [out, size_is(cBindings)] DBBINDSTATUS rgStatus[]
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult CreateAccessor(
                [In] int dwAccessorFlags,
                [In] IntPtr cBindings,
                [In] /*tagDBBINDING[]*/SafeHandle rgBindings,
                [In] IntPtr cbRowSize,
                [Out] out IntPtr phAccessor,
                [In, Out, MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.I4)] int[] rgStatus);

            [ Obsolete("not used", true)] void GetBindings(/*deleted parameters signature*/);

            /*[local]
        	HRESULT ReleaseAccessor(
        		[in] HACCESSOR hAccessor,
        		[in, out, unique] DBREFCOUNT * pcRefCount
        	);*/
        	[PreserveSig] System.Data.OleDb.OleDbHResult ReleaseAccessor(
                [In] IntPtr hAccessor,
                [Out] out int pcRefCount);
        }

        [Guid("0C733A93-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IChapteredRowset {

            [ Obsolete("not used", true)] void AddRefChapter(/*deleted parameters signature*/);

            /*[local]
            HRESULT ReleaseChapter(
                [in] HCHAPTER hChapter,
                [out] DBREFCOUNT * pcRefCount
            );*/
            [PreserveSig, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] System.Data.OleDb.OleDbHResult ReleaseChapter(
                [In] IntPtr hChapter,
                [Out] out int pcRefCount);
        }

        [Guid("0C733A11-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IColumnsInfo {

            /*[local]
        	HRESULT GetColumnInfo(
        		[in, out] DBORDINAL * pcColumns,
        		[out, size_is(,(ULONG)*pcColumns)] DBCOLUMNINFO ** prgInfo,
        		[out] OLECHAR ** ppStringsBuffer
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetColumnInfo(
                [Out] out IntPtr pcColumns,
                [Out] out IntPtr prgInfo,
                [Out] out IntPtr ppStringsBuffer);

            //[PreserveSig]
            //int MapColumnIDs(/* deleted parameters*/);
        }

        [Guid("0C733A10-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IColumnsRowset {

            /*[local]
            HRESULT GetAvailableColumns(
                [in, out] DBORDINAL * pcOptColumns,
                [out, size_is(,(ULONG)*pcOptColumns)] DBID ** prgOptColumns
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetAvailableColumns(
                [Out] out IntPtr pcOptColumns,
                [Out] out IntPtr prgOptColumns);

            /*[local]
            HRESULT GetColumnsRowset(
                [in] IUnknown * pUnkOuter,
                [in] DBORDINAL cOptColumns,
                [in, size_is((ULONG)cOptColumns)] const DBID rgOptColumns[],
                [in] REFIID riid,
                [in] ULONG cPropertySets,
                [in, out, size_is((ULONG)cPropertySets)] DBPROPSET rgPropertySets[],
                [out, iid_is(riid)] IUnknown ** ppColRowset
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetColumnsRowset(
                [In] IntPtr pUnkOuter,
                [In] IntPtr cOptColumns,
                [In] SafeHandle rgOptColumns,
                [In] ref Guid riid,
                [In] int cPropertySets,
                [In] IntPtr rgPropertySets,
                [Out, MarshalAs(UnmanagedType.Interface)] out IRowset ppColRowset);
        }


        [Guid("0C733A26-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ICommandPrepare {

            /*[local]
            HRESULT Prepare(
                [in] ULONG cExpectedRuns
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult Prepare(
                [In] int cExpectedRuns);

            //[PreserveSig]
            //int Unprepare();
        }

        [Guid("0C733A79-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ICommandProperties {

            /*[local]
        	HRESULT GetProperties(
        		[in] const ULONG cPropertyIDSets,
        		[in, size_is(cPropertyIDSets)] const DBPROPIDSET rgPropertyIDSets[],
        		[in, out] ULONG * pcPropertySets,
        		[out, size_is(,*pcPropertySets)] DBPROPSET ** prgPropertySets
        	);*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetProperties(
                [In] int cPropertyIDSets,
                [In] SafeHandle rgPropertyIDSets,
                [Out] out int pcPropertySets,
                [Out] out IntPtr prgPropertySets);

            /*[local]
            HRESULT SetProperties(
                [in] ULONG cPropertySets,
                [in, out, unique, size_is(cPropertySets)] DBPROPSET rgPropertySets[]
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult SetProperties(
                [In] int cPropertySets,
                [In] SafeHandle rgPropertySets);
        }

        [Guid("0C733A27-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ICommandText {

            /*[local]
            HRESULT Cancel(
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult Cancel();

            /*[local]
            HRESULT Execute(
                [in] IUnknown * pUnkOuter,
                [in] REFIID riid,
                [in, out] DBPARAMS * pParams,
                [out] DBROWCOUNT * pcRowsAffected,
                [out, iid_is(riid)] IUnknown ** ppRowset
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult Execute(
                [In] IntPtr pUnkOuter,
                [In] ref Guid riid,
                [In] System.Data.OleDb.tagDBPARAMS pDBParams,
                [Out] out IntPtr pcRowsAffected,
                [Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);

            [ Obsolete("not used", true)] void GetDBSession(/*deleted parameter signature*/);

            [ Obsolete("not used", true)] void GetCommandText(/*deleted parameter signature*/);

            /*[local]
            HRESULT SetCommandText(
                [in] REFGUID rguidDialect,
                [in, unique] LPCOLESTR pwszCommand
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult SetCommandText(
                [In] ref Guid rguidDialect,
                [In, MarshalAs(UnmanagedType.LPWStr)] string pwszCommand);
        }

        [Guid("0C733A64-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ICommandWithParameters {

            [ Obsolete("not used", true)] void GetParameterInfo(/*deleted parameters signature*/);

            [ Obsolete("not used", true)] void MapParameterNames(/*deleted parameter signature*/);

            /*[local]
            HRESULT SetParameterInfo(
                [in] DB_UPARAMS cParams,
                [in, unique, size_is((ULONG)cParams)] const DB_UPARAMS rgParamOrdinals[],
                [in, unique, size_is((ULONG)cParams)] const DBPARAMBINDINFO rgParamBindInfo[]
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult SetParameterInfo(
                [In] IntPtr cParams,
                [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] rgParamOrdinals,
                [In, MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.Struct)] System.Data.OleDb.tagDBPARAMBINDINFO[] rgParamBindInfo);
        }

        [Guid("2206CCB1-19C1-11D1-89E0-00C04FD7A829"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IDataInitialize {

        }

        [Guid("0C733A89-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IDBInfo {

            /*[local]
        	HRESULT	GetKeywords(
        		[out] LPOLESTR * ppwszKeywords
        	);*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetKeywords(
                [Out, MarshalAs(UnmanagedType.LPWStr)] out string ppwszKeywords);

            /*[local]
            HRESULT GetLiteralInfo(
                [in] ULONG cLiterals,
                [in, size_is(cLiterals)] const DBLITERAL rgLiterals[],
                [in, out] ULONG * pcLiteralInfo,
                [out, size_is(,*pcLiteralInfo)] DBLITERALINFO ** prgLiteralInfo,
                [out] OLECHAR ** ppCharBuffer
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetLiteralInfo(
                [In] int cLiterals,
                [In, MarshalAs(UnmanagedType.LPArray)] int[] rgLiterals,
                [Out] out int pcLiteralInfo,
                [Out] out IntPtr prgLiteralInfo,
                [Out] out IntPtr ppCharBuffer);
        }

        [Guid("0C733A8A-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IDBProperties {

            /*[local]
            HRESULT GetProperties(
                [in] const ULONG cPropertyIDSets,
                [in, size_is(cPropertyIDSets)] const DBPROPIDSET rgPropertyIDSets[],
                [in, out] ULONG * pcPropertySets,
                [out, size_is(,*pcPropertySets)] DBPROPSET ** prgPropertySets
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetProperties(
                [In] int cPropertyIDSets,
                [In] SafeHandle rgPropertyIDSets,
                [Out] out int pcPropertySets,
                [Out] out IntPtr prgPropertySets);

            [PreserveSig] System.Data.OleDb.OleDbHResult GetPropertyInfo(
                [In] int cPropertyIDSets,
                [In] SafeHandle rgPropertyIDSets,
                [Out] out int pcPropertySets,
                [Out] out IntPtr prgPropertyInfoSets,
                [Out] out IntPtr ppDescBuffer);

            [PreserveSig] System.Data.OleDb.OleDbHResult SetProperties(
                [In] int cPropertySets,
                [In] SafeHandle rgPropertySets);
        }

        [Guid("0C733A7B-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IDBSchemaRowset {

            /*[local]
            HRESULT GetRowset(
                [in] IUnknown * pUnkOuter,
                [in] REFGUID rguidSchema,
                [in] ULONG cRestrictions,
                [in, size_is(cRestrictions)] const VARIANT rgRestrictions[],
                [in] REFIID riid,
                [in] ULONG cPropertySets,
                [in, out, unique, size_is(cPropertySets)] DBPROPSET rgPropertySets[],
                [out, iid_is(riid)] IUnknown ** ppRowset
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetRowset(
                [In] IntPtr pUnkOuter,
                [In] ref Guid rguidSchema,
                [In] int cRestrictions,
                [In, MarshalAs(UnmanagedType.LPArray)] object[] rgRestrictions,
                [In] ref Guid riid,
                [In] int cPropertySets,
                [In] IntPtr rgPropertySets,
                [Out, MarshalAs(UnmanagedType.Interface)] out IRowset ppRowset);

            /*[local]
        	HRESULT GetSchemas(
        		[in, out] ULONG * pcSchemas,
        		[out, size_is(,*pcSchemas)] GUID ** prgSchemas,
        		[out, size_is(,*pcSchemas)] ULONG ** prgRestrictionSupport
        	);*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetSchemas(
                [Out] out int pcSchemas,
                [Out] out IntPtr rguidSchema,
                [Out] out IntPtr prgRestrictionSupport);
        }

        [Guid("1CF2B120-547D-101B-8E65-08002B2BD119"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IErrorInfo {

            [ Obsolete("not used", true)] void GetGUID(/*deleted parameter signature*/);

            [PreserveSig] System.Data.OleDb.OleDbHResult GetSource(
                [Out, MarshalAs(UnmanagedType.BStr)] out string pBstrSource);

            [PreserveSig] System.Data.OleDb.OleDbHResult GetDescription(
                [Out, MarshalAs(UnmanagedType.BStr)] out string pBstrDescription);

            //[ Obsolete("not used", true)] void GetHelpFile(/*deleted parameter signature*/);

            //[ Obsolete("not used", true)] void GetHelpContext(/*deleted parameter signature*/);
        }
#if false
        MIDL_INTERFACE("1CF2B120-547D-101B-8E65-08002B2BD119")
        IErrorInfo : public IUnknown
            virtual HRESULT STDMETHODCALLTYPE GetGUID(
                /* [out] */ GUID *pGUID) = 0;
            virtual HRESULT STDMETHODCALLTYPE GetSource(
                /* [out] */ BSTR *pBstrSource) = 0;
            virtual HRESULT STDMETHODCALLTYPE GetDescription(
                /* [out] */ BSTR *pBstrDescription) = 0;
            virtual HRESULT STDMETHODCALLTYPE GetHelpFile(
                /* [out] */ BSTR *pBstrHelpFile) = 0;
            virtual HRESULT STDMETHODCALLTYPE GetHelpContext(
                /* [out] */ DWORD *pdwHelpContext) = 0;
#endif

        [Guid("0C733A67-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IErrorRecords {

            [ Obsolete("not used", true)] void AddErrorRecord(/*deleted parameter signature*/);

            [ Obsolete("not used", true)] void GetBasicErrorInfo(/*deleted parameter signature*/);

            [PreserveSig] System.Data.OleDb.OleDbHResult GetCustomErrorObject( // may return E_NOINTERFACE when asking for IID_ISQLErrorInfo
                [In] Int32 ulRecordNum,
                [In] ref Guid riid,
                [Out, MarshalAs(UnmanagedType.Interface)] out ISQLErrorInfo ppObject);

            [return:MarshalAs(UnmanagedType.Interface)] IErrorInfo GetErrorInfo(
                [In] Int32 ulRecordNum,
                [In] Int32 lcid);

            [ Obsolete("not used", true)] void GetErrorParameters(/*deleted parameter signature*/);

            Int32 GetRecordCount();
        }
#if false
    MIDL_INTERFACE("0c733a67-2a1c-11ce-ade5-00aa0044773d")
    IErrorRecords : public IUnknown
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE AddErrorRecord(
            /* [in] */ ERRORINFO *pErrorInfo,
            /* [in] */ DWORD dwLookupID,
            /* [in] */ DISPPARAMS *pdispparams,
            /* [in] */ IUnknown *punkCustomError,
            /* [in] */ DWORD dwDynamicErrorID) = 0;
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetBasicErrorInfo(
            /* [in] */ ULONG ulRecordNum,
            /* [out] */ ERRORINFO *pErrorInfo) = 0;
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetCustomErrorObject(
            /* [in] */ ULONG ulRecordNum,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppObject) = 0;
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetErrorInfo(
            /* [in] */ ULONG ulRecordNum,
            /* [in] */ LCID lcid,
            /* [out] */ IErrorInfo **ppErrorInfo) = 0;
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetErrorParameters(
            /* [in] */ ULONG ulRecordNum,
            /* [out] */ DISPPARAMS *pdispparams) = 0;
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetRecordCount(
            /* [out] */ ULONG *pcRecords) = 0;
#endif

        [Guid("0C733A90-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IMultipleResults {

            /*[local]
            HRESULT GetResult(
                [in] IUnknown * pUnkOuter,
                [in] DBRESULTFLAG lResultFlag,
                [in] REFIID riid,
                [out] DBROWCOUNT * pcRowsAffected,
                [out, iid_is(riid)] IUnknown ** ppRowset
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetResult(
                [In] IntPtr pUnkOuter,
                [In] IntPtr lResultFlag,
                [In] ref Guid riid,
                [Out] out IntPtr pcRowsAffected,
                [Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);
        }
#if false
        enum DBRESULTFLAGENUM {
            DBRESULTFLAG_DEFAULT = 0,
            DBRESULTFLAG_ROWSET = 1,
            DBRESULTFLAG_ROW = 2
        }
        MIDL_INTERFACE("0c733a90-2a1c-11ce-ade5-00aa0044773d")
        IMultipleResults : public IUnknown
            virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetResult(
                /* [in] */ IUnknown *pUnkOuter,
                /* [in] */ DBRESULTFLAG lResultFlag,
                /* [in] */ REFIID riid,
                /* [out] */ DBROWCOUNT *pcRowsAffected,
                /* [iid_is][out] */ IUnknown **ppRowset) = 0;
#endif

        [Guid("0C733A69-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IOpenRowset {

            [PreserveSig] System.Data.OleDb.OleDbHResult OpenRowset(
                [In] IntPtr pUnkOuter,
                [In] System.Data.OleDb.tagDBID pTableID,
                [In] IntPtr pIndexID,
                [In] ref Guid riid,
                [In] int cPropertySets,
                [In] IntPtr rgPropertySets,
                [Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);
        }

        [Guid("0C733AB4-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IRow {

            [PreserveSig] System.Data.OleDb.OleDbHResult GetColumns(
                [In] IntPtr cColumns,
                [In, Out, MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.Struct)] System.Data.OleDb.tagDBCOLUMNACCESS[] rgColumns);

            //[ Obsolete("not used", true)] void GetSourceRowset(/*deleted parameter signature*/);
            //[ Obsolete("not used", true)] void Open(/*deleted parameter signature*/);
        }

        [Guid("0C733A7C-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IRowset {

            [ Obsolete("not used", true)] void AddRefRows(/*deleted parameter signature*/);

            /*HRESULT GetData(
                [in] HROW hRow,
                [in] HACCESSOR hAccessor,
                [out] void * pData
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetData(
                [In] IntPtr hRow,
                [In] IntPtr hAccessor,
                [In] IntPtr pData);

            /*HRESULT GetNextRows(
                [in] HCHAPTER hReserved,
                [in] DBROWOFFSET lRowsOffset,
                [in] DBROWCOUNT cRows,
                [out] DBCOUNTITEM * pcRowsObtained,
                [out, size_is(,cRows)] HROW ** prghRows
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetNextRows(
                [In] IntPtr hChapter,
                [In] IntPtr lRowsOffset,
                [In] IntPtr cRows,
                [Out] out IntPtr pcRowsObtained,
                [In] ref IntPtr pprghRows);

            /*HRESULT ReleaseRows(
                [in] DBCOUNTITEM cRows,
                [in, size_is(cRows)] const HROW rghRows[],
                [in, size_is(cRows)] DBROWOPTIONS rgRowOptions[],
                [out, size_is(cRows)] DBREFCOUNT rgRefCounts[],
                [out, size_is(cRows)] DBROWSTATUS rgRowStatus[]
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult ReleaseRows(
                [In] IntPtr cRows,
                [In] SafeHandle rghRows,
                [In/*, MarshalAs(UnmanagedType.LPArray)*/] IntPtr/*int[]*/ rgRowOptions,
                [In/*, Out, MarshalAs(UnmanagedType.LPArray)*/] IntPtr/*int[]*/ rgRefCounts,
                [In/*, Out, MarshalAs(UnmanagedType.LPArray)*/] IntPtr/*int[]*/ rgRowStatus);

            [ Obsolete("not used", true)] void RestartPosition(/*deleted parameter signature*/);
        }

        [Guid("0C733A55-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface IRowsetInfo {

            /*[local]
            HRESULT GetProperties(
                [in] const ULONG cPropertyIDSets,
                [in, size_is(cPropertyIDSets)] const DBPROPIDSET rgPropertyIDSets[],
                [in, out] ULONG * pcPropertySets,
                [out, size_is(,*pcPropertySets)] DBPROPSET ** prgPropertySets
            );*/
            [PreserveSig] System.Data.OleDb.OleDbHResult GetProperties(
                [In] int cPropertyIDSets,
                [In] SafeHandle rgPropertyIDSets,
                [Out] out int pcPropertySets,
                [Out] out IntPtr prgPropertySets);

            [PreserveSig] System.Data.OleDb.OleDbHResult GetReferencedRowset(
                [In] IntPtr iOrdinal,
                [In] ref Guid riid,
                [Out, MarshalAs(UnmanagedType.Interface)] out IRowset ppRowset);

            //[PreserveSig]
            //int GetSpecification(/*deleted parameter signature*/);
        }

        [Guid("0C733A74-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ISQLErrorInfo {

            [return:MarshalAs(UnmanagedType.I4)] Int32 GetSQLInfo(
                [Out, MarshalAs(UnmanagedType.BStr)] out String pbstrSQLState);
        }

        [Guid("0C733A5F-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
        internal interface ITransactionLocal {

            [ Obsolete("not used", true)] void Commit(/*deleted parameter signature*/);

            [ Obsolete("not used", true)] void Abort(/*deleted parameter signature*/);

            [ Obsolete("not used", true)] void GetTransactionInfo(/*deleted parameter signature*/);

            [ Obsolete("not used", true)] void GetOptionsObject(/*deleted parameter signature*/);

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [PreserveSig] System.Data.OleDb.OleDbHResult StartTransaction(
                [In] int isoLevel,
                [In] int isoFlags,
                [In] IntPtr pOtherOptions,
                [Out] out int pulTransactionLevel);
        }

        // we wrap the vtable entry which is just a function pointer as a delegate
        // since code (unlike data) doesn't move around within the process, it is safe to cache the delegate

        // we do not expect native to change its vtable entry at run-time (especially since these are free-threaded objects)
        // however to be extra safe double check the function pointer is the same as the cached delegate
        // whenever we encounter a new instance of the data


        // dangerous delegate around IUnknown::QueryInterface (0th vtable entry)
        [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
        internal delegate int IUnknownQueryInterface(
                IntPtr pThis,
                ref Guid riid,
                ref IntPtr ppInterface);

        // dangerous delegate around IDataInitialize::GetDataSource (4th vtable entry)
        [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
        internal delegate System.Data.OleDb.OleDbHResult IDataInitializeGetDataSource(
                IntPtr pThis, // first parameter is always the 'this' value, must use use result from QI
                IntPtr pUnkOuter,
                int dwClsCtx,
                [MarshalAs(UnmanagedType.LPWStr)] string pwszInitializationString,
                ref Guid riid,
                ref System.Data.OleDb.DataSourceWrapper ppDataSource);

        // dangerous wrapper around IDBInitialize::Initialize (4th vtable entry)
        [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
        internal delegate System.Data.OleDb.OleDbHResult IDBInitializeInitialize(
                IntPtr pThis); // first parameter is always the 'this' value, must use use result from QI

        // dangerous wrapper around IDBCreateSession::CreateSession (4th vtable entry)
        [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
        internal delegate System.Data.OleDb.OleDbHResult IDBCreateSessionCreateSession(
                IntPtr pThis, // first parameter is always the 'this' value, must use use result from QI
                IntPtr pUnkOuter,
                ref Guid riid,
                ref System.Data.OleDb.SessionWrapper ppDBSession);

        // dangerous wrapper around IDBCreateCommand::CreateCommand (4th vtable entry)
        [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
        internal delegate System.Data.OleDb.OleDbHResult IDBCreateCommandCreateCommand(
                IntPtr pThis, // first parameter is always the 'this' value, must use use result from QI
                IntPtr pUnkOuter,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] ref object ppCommand);

        //
        // Advapi32.dll Integrated security functions
        //

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct Trustee {
            internal IntPtr _pMultipleTrustee;        // PTRUSTEE
            internal int _MultipleTrusteeOperation;   // MULTIPLE_TRUSTEE_OPERATION
            internal int _TrusteeForm;                // TRUSTEE_FORM
            internal int _TrusteeType;                // TRUSTEE_TYPE
            [MarshalAs(UnmanagedType.LPTStr)]
            internal string _name;

            internal Trustee(string name) {
                _pMultipleTrustee         = IntPtr.Zero;
                _MultipleTrusteeOperation = 0;              // NO_MULTIPLE_TRUSTEE
                _TrusteeForm              = 1;              // TRUSTEE_IS_NAME
                _TrusteeType              = 1;              // TRUSTEE_IS_USER
                _name                     = name;
            }
        }

        [DllImport(ExternDll.Advapi32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern uint GetEffectiveRightsFromAclW (byte[] pAcl, ref Trustee pTrustee, out uint pAccessMask);

        [DllImport(ExternDll.Advapi32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        [return:MarshalAs(UnmanagedType.Bool)]
        static internal extern bool CheckTokenMembership (IntPtr tokenHandle, byte[] sidToCheck, out bool isMember);

        [DllImport(ExternDll.Advapi32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        [return:MarshalAs(UnmanagedType.Bool)]
        static internal extern bool ConvertSidToStringSidW(IntPtr sid, out IntPtr stringSid);

        [DllImport(ExternDll.Advapi32, EntryPoint="CreateWellKnownSid", SetLastError=true, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        static internal extern int CreateWellKnownSid(
            int sidType,
            byte[] domainSid,
            [Out] byte[] resultSid,
            ref uint resultSidLength );

        [DllImport(ExternDll.Advapi32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        [return:MarshalAs(UnmanagedType.Bool)]
        static internal extern bool GetTokenInformation(IntPtr tokenHandle, uint token_class, IntPtr tokenStruct, uint tokenInformationLength, ref uint tokenString);

        [DllImport(ExternDll.Kernel32, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlenW(IntPtr ptr);
    
        /* For debugging purposes...
        [DllImport(ExternDll.Advapi32)]
        [return:MarshalAs(UnmanagedType.I4)]
        static internal extern int GetLengthSid(IntPtr sid1);
        */
    }
}
