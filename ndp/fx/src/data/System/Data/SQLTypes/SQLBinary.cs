//------------------------------------------------------------------------------
// <copyright file="SQLBinary.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">junfang</owner>
// <owner current="true" primary="false">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

//**************************************************************************
// @File: SqlBinary.cs
//
// Create by:    JunFang
//
// Purpose: Implementation of SqlBinary which is corresponding to 
//            data type "binary/varbinary" in SQL Server
//
// Notes: 
//    
// History:
//
//   1/30/2000  JunFang        Created and implemented as first drop.
//
// @EndHeader@
//**************************************************************************

using System;
using System.Data.Common;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes {

    [Serializable]
    [XmlSchemaProvider("GetXsdType")]
    public struct SqlBinary : INullable, IComparable, IXmlSerializable {
        private byte[] m_value; // null if m_value is null

        // constructor
        // construct a Null
        private SqlBinary(bool fNull) {
            m_value = null;
    }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlBinary'/> class with a binary object to be stored.
        ///    </para>
        /// </devdoc>
        public SqlBinary(byte[] value) {
            // if value is null, this generates a SqlBinary.Null
            if (value == null)
                m_value = null;
            else {
                m_value = new byte[value.Length];
                value.CopyTo(m_value, 0);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Data.SqlTypes.SqlBinary'/> class with a binary object to be stored.  This constructor will not copy the value.
        ///    </para>
        /// </devdoc>
        internal SqlBinary(byte[] value, bool ignored) {
            // if value is null, this generates a SqlBinary.Null
            if (value == null)
                m_value = null;
            else {
                m_value = value;
            }
        }

        // INullable
        /// <devdoc>
        ///    <para>
        ///       Gets whether or not <see cref='System.Data.SqlTypes.SqlBinary.Value'/> is null.
        ///    </para>
        /// </devdoc>
        public bool IsNull {
            get { return(m_value == null);}
        }

        // property: Value
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       or sets the
        ///       value of the SQL binary object retrieved.
        ///    </para>
        /// </devdoc>
        public byte[] Value {
            get {
                if (IsNull)
                    throw new SqlNullValueException();
                else {
                    byte[] value = new byte[m_value.Length];
                    m_value.CopyTo(value, 0);
                    return value;
                }
            }
        }

		// class indexer
		public byte this[int index] {
			get {
				if (IsNull)
                    throw new SqlNullValueException();
				else
					return m_value[index];
			}
		}

        // property: Length
        /// <devdoc>
        ///    <para>
        ///       Gets the length in bytes of <see cref='System.Data.SqlTypes.SqlBinary.Value'/>.
        ///    </para>
        /// </devdoc>
        public int Length {
            get {
                if (!IsNull)
                    return m_value.Length;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from byte[] to SqlBinary
		// Alternative: constructor SqlBinary(bytep[])
        /// <devdoc>
        ///    <para>
        ///       Converts a binary object to a <see cref='System.Data.SqlTypes.SqlBinary'/>.
        ///    </para>
        /// </devdoc>
        public static implicit operator SqlBinary(byte[] x) {
            return new SqlBinary(x);
        }

        // Explicit conversion from SqlBinary to byte[]. Throw exception if x is Null.
		// Alternative: Value property
        /// <devdoc>
        ///    <para>
        ///       Converts a <see cref='System.Data.SqlTypes.SqlBinary'/> to a binary object.
        ///    </para>
        /// </devdoc>
        public static explicit operator byte[](SqlBinary x) {
            return x.Value;
        }

        /// <devdoc>
        ///    <para>
        ///       Returns a string describing a <see cref='System.Data.SqlTypes.SqlBinary'/> object.
        ///    </para>
        /// </devdoc>
        public override String ToString() {
            return IsNull ? SQLResource.NullString : "SqlBinary(" + m_value.Length.ToString(CultureInfo.InvariantCulture) + ")";
        }


        // Unary operators

        // Binary operators

        // Arithmetic operators
        /// <devdoc>
        ///    <para>
        ///       Adds two instances of <see cref='System.Data.SqlTypes.SqlBinary'/> together.
        ///    </para>
        /// </devdoc>
        // Alternative method: SqlBinary.Concat
        public static SqlBinary operator +(SqlBinary x, SqlBinary y) {
            if (x.IsNull || y.IsNull)
                return Null;

            byte[] rgbResult = new byte[x.Value.Length + y.Value.Length];
            x.Value.CopyTo(rgbResult, 0);
            y.Value.CopyTo(rgbResult, x.Value.Length);

            return new SqlBinary(rgbResult);
        }


        // Comparisons

        private static EComparison PerformCompareByte(byte[] x, byte[] y) {
            // the smaller length of two arrays
            int len = (x.Length < y.Length) ? x.Length : y.Length;
            int i;

            for (i = 0; i < len; ++i) {
                if (x[i] != y[i]) {
                    if (x[i] < y[i])
                        return EComparison.LT;
                    else
                        return EComparison.GT;
                }
            }

            if (x.Length == y.Length)
                return EComparison.EQ;
            else {
                // if the remaining bytes are all zeroes, they are still equal.

                byte bZero = (byte)0;

                if (x.Length < y.Length) {
                    // array X is shorter
                    for (i = len; i < y.Length; ++i) {
                        if (y[i] != bZero)
                            return EComparison.LT;
                    }
                }
                else {
                    // array Y is shorter
                    for (i = len; i < x.Length; ++i) {
                        if (x[i] != bZero)
                            return EComparison.GT;
                    }
                }

                return EComparison.EQ;
            }
        }


        // Implicit conversions

        // Explicit conversions

        // Explicit conversion from SqlGuid to SqlBinary
        /// <devdoc>
        ///    <para>
        ///       Converts a <see cref='System.Data.SqlTypes.SqlGuid'/> to a <see cref='System.Data.SqlTypes.SqlBinary'/>
        ///       .
        ///    </para>
        /// </devdoc>
        // Alternative method: SqlGuid.ToSqlBinary
        public static explicit operator SqlBinary(SqlGuid x) {
            return x.IsNull ? SqlBinary.Null : new SqlBinary(x.ToByteArray());
        }

        // Builtin functions

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>
        ///       Compares two instances of <see cref='System.Data.SqlTypes.SqlBinary'/> for
        ///       equality.
        ///    </para>
        /// </devdoc>
        public static SqlBoolean operator==(SqlBinary x, SqlBinary y) {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.EQ);
        }

        /// <devdoc>
        ///    <para>
        ///       Compares two instances of <see cref='System.Data.SqlTypes.SqlBinary'/>
        ///       for equality.
        ///    </para>
        /// </devdoc>
        public static SqlBoolean operator!=(SqlBinary x, SqlBinary y) {
            return ! (x == y);
        }

        /// <devdoc>
        ///    <para>
        ///       Compares the first <see cref='System.Data.SqlTypes.SqlBinary'/> for being less than the
        ///       second <see cref='System.Data.SqlTypes.SqlBinary'/>.
        ///    </para>
        /// </devdoc>
        public static SqlBoolean operator<(SqlBinary x, SqlBinary y) {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.LT);
        }

        /// <devdoc>
        ///    <para>
        ///       Compares the first <see cref='System.Data.SqlTypes.SqlBinary'/> for being greater than the second <see cref='System.Data.SqlTypes.SqlBinary'/>.
        ///    </para>
        /// </devdoc>
        public static SqlBoolean operator>(SqlBinary x, SqlBinary y) {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.GT);
        }

        /// <devdoc>
        ///    <para>
        ///       Compares the first <see cref='System.Data.SqlTypes.SqlBinary'/> for being less than or equal to the second <see cref='System.Data.SqlTypes.SqlBinary'/>.
        ///    </para>
        /// </devdoc>
        public static SqlBoolean operator<=(SqlBinary x, SqlBinary y) {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            EComparison cmpResult = PerformCompareByte(x.Value, y.Value);
            return new SqlBoolean(cmpResult == EComparison.LT || cmpResult == EComparison.EQ);
        }

        /// <devdoc>
        ///    <para>
        ///       Compares the first <see cref='System.Data.SqlTypes.SqlBinary'/> for being greater than or equal the second <see cref='System.Data.SqlTypes.SqlBinary'/>.
        ///    </para>
        /// </devdoc>
        public static SqlBoolean operator>=(SqlBinary x, SqlBinary y) {
            if (x.IsNull || y.IsNull)
                return SqlBoolean.Null;

            EComparison cmpResult = PerformCompareByte(x.Value, y.Value);
            return new SqlBoolean(cmpResult == EComparison.GT || cmpResult == EComparison.EQ);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlBinary Add(SqlBinary x, SqlBinary y) {
                return x + y;
        }
        
         public static SqlBinary Concat(SqlBinary x, SqlBinary y) {
                return x + y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlBinary x, SqlBinary y) {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlBinary x, SqlBinary y) {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlBinary x, SqlBinary y) {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlBinary x, SqlBinary y) {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlBinary x, SqlBinary y) {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlBinary x, SqlBinary y) {
            return (x >= y);
        }

        // Alternative method for conversions.
        public SqlGuid ToSqlGuid() {
            return (SqlGuid)this;
        }

        // IComparable
        // Compares this object to another object, returning an integer that
        // indicates the relationship. 
        // Returns a value less than zero if this < object, zero if this = object, 
        // or a value greater than zero if this > object.
        // null is considered to be less than any instance.
        // If object is not of same type, this method throws an ArgumentException.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int CompareTo(Object value) {
            if (value is SqlBinary) {
                SqlBinary i = (SqlBinary)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlBinary));
        }

        public int CompareTo(SqlBinary value) {
            // If both Null, consider them equal.
            // Otherwise, Null is less than anything.
            if (IsNull)
                return value.IsNull ? 0  : -1;
            else if (value.IsNull)
                return 1;

            if (this < value) return -1;
            if (this > value) return 1;
            return 0;
        }

        // Compares this instance with a specified object
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override bool Equals(Object value) {
            if (!(value is SqlBinary)) {
                return false;
            }

            SqlBinary i = (SqlBinary)value;

            if (i.IsNull || IsNull)
                return (i.IsNull && IsNull);
            else
                return (this == i).Value;
        }

        // Hash a byte array.
        // Trailing zeroes/spaces would affect the hash value, so caller needs to
        // perform trimming as necessary.
        internal static int HashByteArray(byte[] rgbValue, int length)
        {
            SQLDebug.Check(length >= 0);
        
            if (length <= 0)
                return 0;
        
            SQLDebug.Check(rgbValue.Length >= length);
        
            int ulValue = 0;
            int ulHi;

            // Size of CRC window (hashing bytes, ssstr, sswstr, numeric)
            const int x_cbCrcWindow = 4;
            // const int iShiftVal = (sizeof ulValue) * (8*sizeof(char)) - x_cbCrcWindow;
            const int iShiftVal = 4 * 8 - x_cbCrcWindow;

            for(int i = 0; i < length; i++)
                {
                ulHi = (ulValue >> iShiftVal) & 0xff;
                ulValue <<= x_cbCrcWindow;
                ulValue = ulValue ^ rgbValue[i] ^ ulHi;
                }

            return ulValue;
        }
        // For hashing purpose
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode() 
        {
                    if (IsNull)
                        return 0;
        
                    //First trim off extra '\0's
                    int cbLen = m_value.Length;
                    while (cbLen > 0 && m_value[cbLen - 1] == 0)
                        --cbLen;
        
                    return HashByteArray(m_value, cbLen);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        XmlSchema IXmlSerializable.GetSchema() { return null; }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        void IXmlSerializable.ReadXml(XmlReader reader) {
            string isNull = reader.GetAttribute("nil", XmlSchema.InstanceNamespace);
            if (isNull != null && XmlConvert.ToBoolean(isNull)) {
                // VSTFDevDiv# 479603 - SqlTypes read null value infinitely and never read the next value. Fix - Read the next value.
                reader.ReadElementString();
                m_value = null;
            }
            else {
                string base64 = reader.ReadElementString();
                if (base64 == null) {
                    m_value = new byte[0];
                }
                else {
                    base64 = base64.Trim();

                    if (base64.Length == 0) {
                        m_value = new byte[0];
                    }
                    else {
                        m_value = Convert.FromBase64String(base64);
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        void IXmlSerializable.WriteXml(XmlWriter writer) {
            if (IsNull) {
                writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
            }
            else {
                writer.WriteString(Convert.ToBase64String(m_value));
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet) {
            return new XmlQualifiedName("base64Binary", XmlSchema.Namespace);
        }

        /// <devdoc>
        ///    <para>
        ///       Represents a null value that can be assigned to
        ///       the <see cref='System.Data.SqlTypes.SqlBinary.Value'/> property of an
        ///       instance of the <see cref='System.Data.SqlTypes.SqlBinary'/> class.
        ///    </para>
        /// </devdoc>
        public static readonly SqlBinary Null       = new SqlBinary(true);

    } // SqlBinary

} // namespace System.Data.SqlTypes
