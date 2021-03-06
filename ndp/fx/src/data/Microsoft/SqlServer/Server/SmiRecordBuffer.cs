//------------------------------------------------------------------------------
// <copyright file="SmiRecordBuffer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace Microsoft.SqlServer.Server {

    using System;
    using System.Data;
    using System.Data.SqlTypes;

    // Class for implementing a record object that could take advantage of the
    // environment available to a particular protocol level (such as storing data 
    // in native structures for in-proc data access).  Includes methods to send 
    // the record to a context pipe (useful for in-proc scenarios).
    internal abstract class SmiRecordBuffer : SmiTypedGetterSetter, ITypedGettersV3, ITypedSettersV3, ITypedGetters, ITypedSetters, IDisposable {

        #region SMI active methods as of V200

        #region control methods
        public virtual void Close(
            SmiEventSink        eventSink
        ) {
            // Adding as of V3

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V2- and hasn't implemented V3 yet.
            //  2) Server didn't implement V3 on some interface, but negotiated V3+.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        #endregion

        #region Supported access method types (Get] vs. Set)

        // SmiRecordBuffer defaults both CanGet and CanSet to true to support
        //  already-shipped SMIV3 record buffer classes.  Sub-classes are free to override.
        internal override bool CanGet {
            get {
                return true;
            }
        }

        internal override bool CanSet {
            get {
                return true;
            }
        }
        #endregion



        #region Value getters

        //  SmiRecordBuffer subclasses that expose CanGet == true must implement all Getters from SmiTypedGetterSetter
        //  SmiRecordBuffer itself does not need to implement these, since it inherits the default implementation from 
        //      SmiTypedGetterSetter

        #endregion

        #region Value setters

        // SmiRecordBuffer subclasses that expose CanSet == true must implement all Setters from SmiTypedGetterSetter
        //  SmiRecordBuffer itself does not need to implement these, since it inherits the default implementation from 
        //      SmiTypedGetterSetter

        #endregion

        #endregion

        #region OBSOLETE STUFF than never shipped without obsolete attribute

        //
        //  IDisposable
        //
        public virtual void Dispose( ) {
            // ******** OBSOLETING from SMI -- use Close instead
            //  Intended to be removed (along with inheriting IDisposable) prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        //
        //  ITypedGetters methods
        //
        public virtual bool IsDBNull( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlDbType GetVariantType( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Boolean GetBoolean( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Byte GetByte( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual long GetBytes( int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Char GetChar( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual long GetChars( int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Int16 GetInt16( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Int32 GetInt32( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Int64 GetInt64( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Single GetFloat( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Double GetDouble( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual String GetString( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Decimal GetDecimal( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual DateTime GetDateTime( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual Guid GetGuid( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlBoolean GetSqlBoolean( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlByte GetSqlByte( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlInt16 GetSqlInt16( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlInt32 GetSqlInt32( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlInt64 GetSqlInt64( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlSingle GetSqlSingle( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlDouble GetSqlDouble( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlMoney GetSqlMoney( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlDateTime GetSqlDateTime( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlDecimal GetSqlDecimal( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlString GetSqlString( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlBinary GetSqlBinary( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlGuid GetSqlGuid( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlChars GetSqlChars( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlBytes GetSqlBytes( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlXml GetSqlXml( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlXml GetSqlXmlRef( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlBytes GetSqlBytesRef( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual SqlChars GetSqlCharsRef( int ordinal ) {
            // ******** OBSOLETING from SMI -- use corresponding ITypedGettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        //
        //  ITypedSetters methods
        //
        public virtual void SetDBNull( int ordinal ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetBoolean( int ordinal, Boolean value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetByte( int ordinal, Byte value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetBytes( int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetChar( int ordinal, char value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetChars( int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetInt16( int ordinal, Int16 value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetInt32( int ordinal, Int32 value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetInt64( int ordinal, Int64 value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetFloat( int ordinal, Single value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetDouble( int ordinal,  Double value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetString( int ordinal, string value )
            {
            // Implemented as empty virtual method to allow transport to remove it's implementation

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V2 and dropped support for V1.
            //  2) Server didn't implement V1 on some interface and negotiated V1.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
            }

        public virtual void SetString( int ordinal, string value, int offset ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetDecimal( int ordinal, Decimal value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetDateTime( int ordinal, DateTime value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetGuid( int ordinal, Guid value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlBoolean( int ordinal, SqlBoolean value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlByte( int ordinal, SqlByte value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlInt16( int ordinal, SqlInt16 value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlInt32( int ordinal, SqlInt32 value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlInt64( int ordinal, SqlInt64 value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlSingle( int ordinal, SqlSingle value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlDouble( int ordinal, SqlDouble value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlMoney( int ordinal, SqlMoney value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlDateTime( int ordinal, SqlDateTime value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlDecimal( int ordinal, SqlDecimal value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlString( int ordinal, SqlString value )
            {
            // Implemented as empty virtual method to allow transport to remove it's implementation

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V2 and dropped support for V1.
            //  2) Server didn't implement V1 on some interface and negotiated V1.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
            }

        public virtual void SetSqlString( int ordinal, SqlString value, int offset ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlBinary( int ordinal, SqlBinary value )
            {
            // Implemented as empty virtual method to allow transport to remove it's implementation

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V2 and dropped support for V1.
            //  2) Server didn't implement V1 on some interface and negotiated V1.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
            }

        public virtual void SetSqlBinary( int ordinal, SqlBinary value, int offset ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlGuid( int ordinal, SqlGuid value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlChars( int ordinal, SqlChars value )
            {
            // Implemented as empty virtual method to allow transport to remove it's implementation

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V2 and dropped support for V1.
            //  2) Server didn't implement V1 on some interface and negotiated V1.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
            }

        public virtual void SetSqlChars( int ordinal, SqlChars value, int offset ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlBytes( int ordinal, SqlBytes value )
            {
            // Implemented as empty virtual method to allow transport to remove it's implementation

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V2 and dropped support for V1.
            //  2) Server didn't implement V1 on some interface and negotiated V1.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
            }

        public virtual void SetSqlBytes( int ordinal, SqlBytes value, int offset ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        public virtual void SetSqlXml( int ordinal, SqlXml value ) {
            // ******** OBSOLETING from SMI -- use ITypedSettersV3 method instead
            //  Intended to be removed prior to RTM.

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        #endregion
    }
}
