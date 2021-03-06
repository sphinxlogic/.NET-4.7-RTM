//------------------------------------------------------------------------------
// <copyright file="SqlDouble.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// <owner current="true" primary="true">junfang</owner>
// <owner current="true" primary="false">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

//**************************************************************************
// @File: SqlDouble.cs
//
// Create by:    JunFang
//
// Purpose: Implementation of SqlDouble which is equivalent to
//            data type "float" in SQL Server
//
// Notes:
//
// History:
//
//   09/17/99  JunFang    Created and implemented as first drop.
//
// @EndHeader@
//**************************************************************************

using System;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes {

    /// <devdoc>
    ///    <para>
    ///       Represents a floating-point number within the range of
    ///       -1.79E
    ///       +308 through 1.79E +308 to be stored in or retrieved from
    ///       a database.
    ///    </para>
    /// </devdoc>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [XmlSchemaProvider("GetXsdType")]
    public struct SqlDouble : INullable, IComparable, IXmlSerializable {

        private bool m_fNotNull; // false if null
        private double m_value;

        // constructor
        // construct a Null
        private SqlDouble(bool fNull) {
            m_fNotNull = false;
            m_value = 0.0;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SqlDouble(double value) {
            if (Double.IsInfinity(value) || Double.IsNaN(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);
            else {
                m_value = value;
                m_fNotNull = true;
            }
        }

        // INullable
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNull {
            get { return !m_fNotNull;}
        }

        // property: Value
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public double Value {
            get {
                if (m_fNotNull)
                    return m_value;
                else
                    throw new SqlNullValueException();
            }
        }

        // Implicit conversion from double to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(double x) {
            return new SqlDouble(x);
        }

        // Explicit conversion from SqlDouble to double. Throw exception if x is Null.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator double(SqlDouble x) {
            return x.Value;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override String ToString() {
            return IsNull ? SQLResource.NullString : m_value.ToString((IFormatProvider)null);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlDouble Parse(String s) {
            if (s == SQLResource.NullString)
                return SqlDouble.Null;
            else
                return new SqlDouble(Double.Parse(s, CultureInfo.InvariantCulture));
        }


        // Unary operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlDouble operator -(SqlDouble x) {
            return x.IsNull ? Null : new SqlDouble(-x.m_value);
        }


        // Binary operators

        // Arithmetic operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlDouble operator +(SqlDouble x, SqlDouble y) {
            if (x.IsNull || y.IsNull)
                return Null;

            double value = x.m_value + y.m_value;

            if (Double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlDouble operator -(SqlDouble x, SqlDouble y) {
            if (x.IsNull || y.IsNull)
                return Null;

            double value = x.m_value - y.m_value;

            if (Double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlDouble operator *(SqlDouble x, SqlDouble y) {
            if (x.IsNull || y.IsNull)
                return Null;

            double value = x.m_value * y.m_value;

            if (Double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlDouble operator /(SqlDouble x, SqlDouble y) {
            if (x.IsNull || y.IsNull)
                return Null;

            if (y.m_value == (double)0.0)
                throw new DivideByZeroException(SQLResource.DivideByZeroMessage);

            double value = x.m_value / y.m_value;

            if (Double.IsInfinity(value))
                throw new OverflowException(SQLResource.ArithOverflowMessage);

            return new SqlDouble(value);
        }



        // Implicit conversions

        // Implicit conversion from SqlBoolean to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlDouble(SqlBoolean x) {
            return x.IsNull ? Null : new SqlDouble((double)(x.ByteValue));
        }

        // Implicit conversion from SqlByte to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(SqlByte x) {
            return x.IsNull ? Null : new SqlDouble((double)(x.Value));
        }

        // Implicit conversion from SqlInt16 to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(SqlInt16 x) {
            return x.IsNull ? Null : new SqlDouble((double)(x.Value));
        }

        // Implicit conversion from SqlInt32 to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(SqlInt32 x) {
            return x.IsNull ? Null : new SqlDouble((double)(x.Value));
        }

        // Implicit conversion from SqlInt64 to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(SqlInt64 x) {
            return x.IsNull ? Null : new SqlDouble((double)(x.Value));
        }

        // Implicit conversion from SqlSingle to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(SqlSingle x) {
            return x.IsNull ? Null : new SqlDouble((double)(x.Value));
        }

        // Implicit conversion from SqlMoney to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(SqlMoney x) {
            return x.IsNull ? Null : new SqlDouble(x.ToDouble());
        }

        // Implicit conversion from SqlDecimal to SqlDouble
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static implicit operator SqlDouble(SqlDecimal x) {
            return x.IsNull ? Null : new SqlDouble(x.ToDouble());
        }


        // Explicit conversions



        // Explicit conversion from SqlString to SqlDouble
        // Throws FormatException or OverflowException if necessary.
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static explicit operator SqlDouble(SqlString x) {
            if (x.IsNull)
                return SqlDouble.Null;

            return Parse(x.Value);
        }

        // Overloading comparison operators
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator==(SqlDouble x, SqlDouble y) {
            return(x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value == y.m_value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator!=(SqlDouble x, SqlDouble y) {
            return ! (x == y);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator<(SqlDouble x, SqlDouble y) {
            return(x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value < y.m_value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator>(SqlDouble x, SqlDouble y) {
            return(x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value > y.m_value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator<=(SqlDouble x, SqlDouble y) {
            return(x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value <= y.m_value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static SqlBoolean operator>=(SqlDouble x, SqlDouble y) {
            return(x.IsNull || y.IsNull) ? SqlBoolean.Null : new SqlBoolean(x.m_value >= y.m_value);
        }

        //--------------------------------------------------
        // Alternative methods for overloaded operators
        //--------------------------------------------------

        // Alternative method for operator +
        public static SqlDouble Add(SqlDouble x, SqlDouble y) {
            return x + y;
        }
        // Alternative method for operator -
        public static SqlDouble Subtract(SqlDouble x, SqlDouble y) {
            return x - y;
        }

        // Alternative method for operator *
        public static SqlDouble Multiply(SqlDouble x, SqlDouble y) {
            return x * y;
        }

        // Alternative method for operator /
        public static SqlDouble Divide(SqlDouble x, SqlDouble y) {
            return x / y;
        }

        // Alternative method for operator ==
        public static SqlBoolean Equals(SqlDouble x, SqlDouble y) {
            return (x == y);
        }

        // Alternative method for operator !=
        public static SqlBoolean NotEquals(SqlDouble x, SqlDouble y) {
            return (x != y);
        }

        // Alternative method for operator <
        public static SqlBoolean LessThan(SqlDouble x, SqlDouble y) {
            return (x < y);
        }

        // Alternative method for operator >
        public static SqlBoolean GreaterThan(SqlDouble x, SqlDouble y) {
            return (x > y);
        }

        // Alternative method for operator <=
        public static SqlBoolean LessThanOrEqual(SqlDouble x, SqlDouble y) {
            return (x <= y);
        }

        // Alternative method for operator >=
        public static SqlBoolean GreaterThanOrEqual(SqlDouble x, SqlDouble y) {
            return (x >= y);
        }

        // Alternative method for conversions.

        public SqlBoolean ToSqlBoolean() {
            return (SqlBoolean)this;
        }

        public SqlByte ToSqlByte() {
            return (SqlByte)this;
        }

        public SqlInt16 ToSqlInt16() {
            return (SqlInt16)this;
        }

        public SqlInt32 ToSqlInt32() {
            return (SqlInt32)this;
        }

        public SqlInt64 ToSqlInt64() {
            return (SqlInt64)this;
        }

        public SqlMoney ToSqlMoney() {
            return (SqlMoney)this;
        }

        public SqlDecimal ToSqlDecimal() {
            return (SqlDecimal)this;
        }

        public SqlSingle ToSqlSingle() {
            return (SqlSingle)this;
        }

        public SqlString ToSqlString() {
            return (SqlString)this;
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
            if (value is SqlDouble) {
                SqlDouble i = (SqlDouble)value;

                return CompareTo(i);
            }
            throw ADP.WrongType(value.GetType(), typeof(SqlDouble));
        }

        public int CompareTo(SqlDouble value) {
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
            if (!(value is SqlDouble)) {
                return false;
            }

            SqlDouble i = (SqlDouble)value;

            if (i.IsNull || IsNull)
                return (i.IsNull && IsNull);
            else
                return (this == i).Value;
        }

        // For hashing purpose
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode() {
            return IsNull ? 0 : Value.GetHashCode();
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
                m_fNotNull = false;
            }
            else {
                m_value = XmlConvert.ToDouble(reader.ReadElementString());
                m_fNotNull = true;
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
                writer.WriteString(XmlConvert.ToString(m_value));
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet) {
            return new XmlQualifiedName("double", XmlSchema.Namespace);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlDouble Null       = new SqlDouble(true);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlDouble Zero       = new SqlDouble(0.0);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlDouble MinValue   = new SqlDouble(Double.MinValue);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly SqlDouble MaxValue   = new SqlDouble(Double.MaxValue);

    } // SqlDouble

} // namespace System.Data.SqlTypes
