//------------------------------------------------------------------------------
// <copyright file="SmiTypedGetterSetter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------


namespace Microsoft.SqlServer.Server {

    using System;
    using System.Data;
    using System.Data.SqlTypes;

    // Central interface for getting/setting data values from/to a set of values indexed by ordinal 
    //  (record, row, array, etc)
    //  Which methods are allowed to be called depends on SmiMetaData type of data offset.
    internal abstract class SmiTypedGetterSetter : ITypedGettersV3, ITypedSettersV3 {
        #region Read/Write
        // Are calls to Get methods allowed?
        internal abstract bool CanGet {
            get;
        }

        // Are calls to Set methods allowed?
        internal abstract bool CanSet {
            get;
        }
        #endregion
        
        #region Getters
        // Null test
        //      valid for all types
        public virtual bool IsDBNull(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // Check what type current sql_variant value is
        //      valid for SqlDbType.Variant
        public virtual SmiMetaData GetVariantType(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.Bit
        public virtual Boolean GetBoolean(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.TinyInt
        public virtual Byte GetByte(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbTypes: Binary, VarBinary, Image, Udt, Xml, Char, VarChar, Text, NChar, NVarChar, NText
        //  (Character type support needed for ExecuteXmlReader handling)
        public virtual Int64 GetBytesLength(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual int GetBytes(SmiEventSink sink, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        public virtual Int64 GetCharsLength(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual int GetChars(SmiEventSink sink, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual String GetString(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.SmallInt
        public virtual Int16 GetInt16(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Int
        public virtual Int32 GetInt32(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.BigInt, SqlDbType.Money, SqlDbType.SmallMoney
        public virtual Int64 GetInt64(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Real
        public virtual Single GetSingle(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Float
        public virtual Double GetDouble(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Numeric (uses SqlDecimal since Decimal cannot hold full range)
        public virtual SqlDecimal GetSqlDecimal(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTime, SmallDateTime, Date, and DateTime2
        public virtual DateTime GetDateTime(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for UniqueIdentifier
        public virtual Guid GetGuid(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Time
        public virtual TimeSpan GetTimeSpan(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            } else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTimeOffset
        public virtual DateTimeOffset GetDateTimeOffset(SmiEventSink sink, int ordinal) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            } else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for structured types
        //  This method called for both get and set.
        internal virtual SmiTypedGetterSetter GetTypedGetterSetter(SmiEventSink sink, int ordinal) {
            throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
        }

        // valid for multi-valued types only
        internal virtual bool NextElement(SmiEventSink sink) {
            if (!CanGet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        #endregion

        #region Setters

        // Set value to null
        //  valid for all types
        public virtual void SetDBNull(SmiEventSink sink, int ordinal) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.Bit
        public virtual void SetBoolean(SmiEventSink sink, int ordinal, Boolean value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.TinyInt
        public virtual void SetByte(SmiEventSink sink, int ordinal, Byte value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // Semantics for SetBytes are to modify existing value, not overwrite
        //  Use in combination with SetLength to ensure overwriting when necessary
        // valid for SqlDbTypes: Binary, VarBinary, Image, Udt, Xml
        //      (VarBinary assumed for variants)
        public virtual int SetBytes(SmiEventSink sink, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual void SetBytesLength(SmiEventSink sink, int ordinal, long length) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // Semantics for SetChars are to modify existing value, not overwrite
        //  Use in combination with SetLength to ensure overwriting when necessary
        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        //      (NVarChar and global clr collation assumed for variants)
        public virtual int SetChars(SmiEventSink sink, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual void SetCharsLength(SmiEventSink sink, int ordinal, long length) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        public virtual void SetString(SmiEventSink sink, int ordinal, string value, int offset, int length) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.SmallInt
        public virtual void SetInt16(SmiEventSink sink, int ordinal, Int16 value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Int
        public virtual void SetInt32(SmiEventSink sink, int ordinal, Int32 value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.BigInt, SqlDbType.Money, SqlDbType.SmallMoney
        public virtual void SetInt64(SmiEventSink sink, int ordinal, Int64 value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Real
        public virtual void SetSingle(SmiEventSink sink, int ordinal, Single value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Float
        public virtual void SetDouble(SmiEventSink sink, int ordinal,  Double value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Numeric (uses SqlDecimal since Decimal cannot hold full range)
        public virtual void SetSqlDecimal(SmiEventSink sink, int ordinal, SqlDecimal value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTime, SmallDateTime, Date, and DateTime2
        public virtual void SetDateTime(SmiEventSink sink, int ordinal, DateTime value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for UniqueIdentifier
        public virtual void SetGuid(SmiEventSink sink, int ordinal, Guid value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Time
        public virtual void SetTimeSpan(SmiEventSink sink, int ordinal, TimeSpan value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            } else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTimeOffset
        public virtual void SetDateTimeOffset(SmiEventSink sink, int ordinal, DateTimeOffset value) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            } else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        public virtual void SetVariantMetaData( SmiEventSink sink, int ordinal, SmiMetaData metaData ) {
            // ******** OBSOLETING from SMI -- this should have been removed from ITypedSettersV3
            //  Intended to be removed prior to RTM.  Sub-classes need not implement

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError( System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod );
        }

        // valid for multi-valued types only
        internal virtual void NewElement(SmiEventSink sink) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        internal virtual void EndElements(SmiEventSink sink) {
            if (!CanSet) {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        #endregion

    }
}
