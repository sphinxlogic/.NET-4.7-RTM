//------------------------------------------------------------------------------
// <copyright file="SqlChars.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>																
// <owner current="true" primary="true">junfang</owner>
// <owner current="true" primary="false">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

//**************************************************************************
// @File: SqlChars.cs
// @Owner: junfang
//
// Created by:	JunFang
//
// Description: Class SqlChars is used to represent a char/varchar/nchar/nvarchar
//		data from SQL Server. It contains a char array buffer, which can
//		be refilled. For example, in data access, user could use one instance
//		of SqlChars to bind to a binary column, and we will just keep copying
//		the data into the same instance, and avoid allocation per row.
//
// Notes: 
//	
// History:
//
//     @Version: Yukon
//     120214 JXF  09/23/02 SqlBytes/SqlChars class indexer
//     112296 AZA  07/06/02 Seal SqlAccess classes.
//     107151 AZA  04/18/02 Track byte array buffer as well as SqlBytes in 
//                          sqlaccess.
//     107216 JXF  04/17/02 Bug 514927
//     106854 JXF  04/15/02 Fix http suites due to SqlChars
//     106448 JXF  04/12/02 Bugs on sqlchars
//     105715 JXF  04/05/02 Handle NULL properly in SqlBytes.SetLength
//     91128 JXF  10/17/01 Make SqlBytes not unsafe
//
//   04/20/01  JunFang	Created.
//
// @EndHeader@
//**************************************************************************


namespace System.Data.SqlTypes {
	using System;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Diagnostics;
	using System.Data.Common;
	using System.Data.Sql;
    using System.Data.SqlClient;
	using System.Data.SqlTypes;
	using System.Xml;
	using System.Xml.Schema;
	using System.Xml.Serialization;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable,XmlSchemaProvider("GetXsdType")]
    public sealed class SqlChars : System.Data.SqlTypes.INullable, IXmlSerializable, ISerializable {
		// --------------------------------------------------------------
		//	  Data members
		// --------------------------------------------------------------

		// SqlChars has five possible states
		// 1) SqlChars is Null
		//		- m_stream must be null, m_lCuLen must be x_lNull
		// 2) SqlChars contains a valid buffer, 
		//		- m_rgchBuf must not be null, and m_stream must be null
		// 3) SqlChars contains a valid pointer
		//		- m_rgchBuf could be null or not,
		//			if not null, content is garbage, should never look into it.
		//      - m_stream must be null.
		// 4) SqlChars contains a SqlStreamChars
		//      - m_stream must not be null
		//      - m_rgchBuf could be null or not. if not null, content is garbage, should never look into it.
		//		- m_lCurLen must be x_lNull.
		// 5) SqlChars contains a Lazy Materialized Blob (ie, StorageState.Delayed)
		//
		internal char[]	            m_rgchBuf;	// Data buffer
		private  long	            m_lCurLen;	// Current data length
		internal SqlStreamChars     m_stream;
		private  SqlBytesCharsState m_state;

		private  char[]	            m_rgchWorkBuf;	// A 1-char work buffer.

		// The max data length that we support at this time.
		private const long x_lMaxLen = (long)System.Int32.MaxValue;

		private const long x_lNull = -1L;

		// --------------------------------------------------------------
		//	  Constructor(s)
		// --------------------------------------------------------------

		// Public default constructor used for XML serialization
		public SqlChars() {
			SetNull();
		}

		// Create a SqlChars with an in-memory buffer
		public SqlChars(char[] buffer) {
			m_rgchBuf = buffer;
			m_stream = null;
			if (m_rgchBuf == null) {
				m_state = SqlBytesCharsState.Null;
				m_lCurLen = x_lNull;
            }
			else {
				m_state = SqlBytesCharsState.Buffer;
				m_lCurLen = (long)m_rgchBuf.Length;
            }

			m_rgchWorkBuf = null;

			AssertValid();
        }

	// Create a SqlChars from a SqlString
	public SqlChars(SqlString value) : this (value.IsNull ? (char[])null : value.Value.ToCharArray()) {
        }

		// Create a SqlChars from a SqlStreamChars
		internal SqlChars(SqlStreamChars s) {
			m_rgchBuf = null;
			m_lCurLen = x_lNull;
			m_stream = s;
			m_state = (s == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Stream;

			m_rgchWorkBuf = null;

			AssertValid();
        }

		// Constructor required for serialization. Deserializes as a Buffer. If the bits have been tampered with
		// then this will throw a SerializationException or a InvalidCastException.
		private SqlChars(SerializationInfo info, StreamingContext context)
			{
			m_stream = null;
			m_rgchWorkBuf = null;

			if (info.GetBoolean("IsNull"))
				{
				m_state = SqlBytesCharsState.Null;
				m_rgchBuf = null;
				}
			else
				{
				m_state = SqlBytesCharsState.Buffer;
				m_rgchBuf = (char[]) info.GetValue("data", typeof(char[]));
				m_lCurLen = m_rgchBuf.Length;
				}

			AssertValid();
			}

		// --------------------------------------------------------------
		//	  Public properties
		// --------------------------------------------------------------

		// INullable
		public bool IsNull {
			get {
			    return m_state == SqlBytesCharsState.Null;
			}
		}

		// Property: the in-memory buffer of SqlChars
		//		Return Buffer even if SqlChars is Null.

		public char[] Buffer {
			get {
				if (FStream())	{
					CopyStreamToBuffer();
                }
				return m_rgchBuf;
            }
        }

		// Property: the actual length of the data
		public long Length {
			get {
				switch (m_state) {
					case SqlBytesCharsState.Null: 
                        throw new SqlNullValueException();

					case SqlBytesCharsState.Stream:
                        return m_stream.Length;

					default:
						return m_lCurLen;
                }
            }
        }

		// Property: the max length of the data
		//		Return MaxLength even if SqlChars is Null.
		//		When the buffer is also null, return -1.
		//		If containing a Stream, return -1.

		public long MaxLength {
			get {
				switch (m_state) {
					case SqlBytesCharsState.Stream:
						return -1L;

					default:
						return (m_rgchBuf == null) ? -1L : (long)m_rgchBuf.Length;
                }
            }
        }

		// Property: get a copy of the data in a new char[] array.

		public char[] Value {
			get {
                char[] buffer;

				switch (m_state) {
					case SqlBytesCharsState.Null: 
						throw new SqlNullValueException();

					case SqlBytesCharsState.Stream:
						if (m_stream.Length > x_lMaxLen)
                                            throw new SqlTypeException(Res.GetString(Res.SqlMisc_BufferInsufficientMessage));
						buffer = new char[m_stream.Length];
						if (m_stream.Position != 0)
							m_stream.Seek(0, SeekOrigin.Begin);
						m_stream.Read(buffer, 0, checked((int)m_stream.Length));
						break;

					default:
						buffer = new char[m_lCurLen];
						Array.Copy(m_rgchBuf, buffer, (int)m_lCurLen);
						break;
                }

				return buffer;
            }
        }

		// class indexer

		public char this[long offset] {
			get {
                if (offset < 0 || offset >= this.Length)
                    throw new ArgumentOutOfRangeException("offset");

                if (m_rgchWorkBuf == null)
					m_rgchWorkBuf = new char[1];

				Read(offset, m_rgchWorkBuf, 0, 1);
				return m_rgchWorkBuf[0];
            }
			set {
				if (m_rgchWorkBuf == null)
					m_rgchWorkBuf = new char[1];
				m_rgchWorkBuf[0] = value;
				Write(offset, m_rgchWorkBuf, 0, 1);
            }
        }

        internal SqlStreamChars Stream {
            get {
                return FStream() ? m_stream : new StreamOnSqlChars(this);
            }
            set {
                m_lCurLen = x_lNull;
                m_stream = value;
                m_state = (value == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Stream;

                AssertValid();                   
            }
        }

	    public StorageState Storage {
        	get {
        		switch (m_state) {
        			case SqlBytesCharsState.Null: 
        				throw new SqlNullValueException();

        			case SqlBytesCharsState.Stream:
        			    return StorageState.Stream;

        			case SqlBytesCharsState.Buffer:
        			    return StorageState.Buffer;

        			default:
        			    return StorageState.UnmanagedBuffer;
                }
            }
        }

		// --------------------------------------------------------------
		//	  Public methods
		// --------------------------------------------------------------

		public void SetNull() {
    		m_lCurLen = x_lNull;
    		m_stream = null;
    		m_state = SqlBytesCharsState.Null;

    		AssertValid();
		}

		// Set the current length of the data
		// If the SqlChars is Null, setLength will make it non-Null.
		public void SetLength(long value) {
			if (value < 0)
				throw new ArgumentOutOfRangeException("value");

			if (FStream()) {
				m_stream.SetLength(value);
            }
			else {
				// If there is a buffer, even the value of SqlChars is Null,
				// still allow setting length to zero, which will make it not Null.
				// If the buffer is null, raise exception
				//
				if (null == m_rgchBuf)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_NoBufferMessage));

				if (value > (long)m_rgchBuf.Length)
					throw new ArgumentOutOfRangeException("value");

				else if (IsNull)
					// At this point we know that value is small enough
					// Go back in buffer mode
					m_state = SqlBytesCharsState.Buffer;
                                
				m_lCurLen = value;
				}

			AssertValid();
        }

		// Read data of specified length from specified offset into a buffer

		public long Read(long offset, char[] buffer, int offsetInBuffer, int count) {
			if (IsNull)
                throw new SqlNullValueException();

			// Validate the arguments
			if (buffer == null)
				throw new ArgumentNullException("buffer");

			if (offset > this.Length || offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			if (offsetInBuffer > buffer.Length || offsetInBuffer < 0)
				throw new ArgumentOutOfRangeException("offsetInBuffer");

			if (count < 0 || count > buffer.Length - offsetInBuffer)
				throw new ArgumentOutOfRangeException("count");

			// Adjust count based on data length
			if (count > this.Length - offset)
				count = (int)(this.Length - offset);

			if (count != 0)	{
				switch (m_state) {
					case SqlBytesCharsState.Stream:
					    if (m_stream.Position != offset)
							m_stream.Seek(offset, SeekOrigin.Begin);
						m_stream.Read(buffer, offsetInBuffer, count);
						break;

					default:
						Array.Copy(m_rgchBuf, offset, buffer, offsetInBuffer, count);
						break;
                }
            }
			return count;
        }

		// Write data of specified length into the SqlChars from specified offset

		public void Write(long offset, char[] buffer, int offsetInBuffer, int count) {
			if (FStream()) {
				if (m_stream.Position != offset)
					m_stream.Seek(offset, SeekOrigin.Begin);
				m_stream.Write(buffer, offsetInBuffer, count);
            }
			else {
				// Validate the arguments
				if (buffer == null)
					throw new ArgumentNullException("buffer");

				if (m_rgchBuf == null)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_NoBufferMessage));

				if (offset < 0)
					throw new ArgumentOutOfRangeException("offset");
				if (offset > m_rgchBuf.Length)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_BufferInsufficientMessage));

				if (offsetInBuffer < 0 || offsetInBuffer > buffer.Length)
					throw new ArgumentOutOfRangeException("offsetInBuffer");

				if (count < 0 || count > buffer.Length - offsetInBuffer)
					throw new ArgumentOutOfRangeException("count");

				if (count > m_rgchBuf.Length - offset)
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_BufferInsufficientMessage));

				if (IsNull) {
					// If NULL and there is buffer inside, we only allow writing from 
					// offset zero.
					//
					if (offset != 0)
                        throw new SqlTypeException(Res.GetString(Res.SqlMisc_WriteNonZeroOffsetOnNullMessage));

					// treat as if our current length is zero.
					// Note this has to be done after all inputs are validated, so that
					// we won't throw exception after this point.
					//
					m_lCurLen = 0;
                    m_state = SqlBytesCharsState.Buffer;
                }
				else if (offset > m_lCurLen) {
					// Don't allow writing from an offset that this larger than current length.
					// It would leave uninitialized data in the buffer.
					//
                    throw new SqlTypeException(Res.GetString(Res.SqlMisc_WriteOffsetLargerThanLenMessage));
				}

				if (count != 0)	{
					Array.Copy(buffer, offsetInBuffer, m_rgchBuf, offset, count);

					// If the last position that has been written is after
					// the current data length, reset the length
					if (m_lCurLen < offset + count)
						m_lCurLen = offset + count;
                }
            }

			AssertValid();
        }

		public SqlString ToSqlString() {
			return IsNull ? SqlString.Null : new String(Value);
        }

		// --------------------------------------------------------------
		//	  Conversion operators
		// --------------------------------------------------------------

		// Alternative method: ToSqlString()
		public static explicit operator SqlString(SqlChars value) {
			return value.ToSqlString();
		}

		// Alternative method: constructor SqlChars(SqlString)
		public static explicit operator SqlChars(SqlString value) {
			return new SqlChars(value);
		}

		// --------------------------------------------------------------
		//	  Private utility functions
		// --------------------------------------------------------------

		[System.Diagnostics.Conditional("DEBUG")] 
		private void AssertValid() {
			Debug.Assert(m_state >= SqlBytesCharsState.Null && m_state <= SqlBytesCharsState.Stream);

			if (IsNull)	{
            }
			else {
				Debug.Assert((m_lCurLen >= 0 && m_lCurLen <= x_lMaxLen) || FStream());
				Debug.Assert(FStream() || (m_rgchBuf != null && m_lCurLen <= m_rgchBuf.Length));
				Debug.Assert(!FStream() || (m_lCurLen == x_lNull));
			}
			Debug.Assert(m_rgchWorkBuf == null || m_rgchWorkBuf.Length == 1);
		}

		// whether the SqlChars contains a Stream
		internal bool FStream() {
			return m_state == SqlBytesCharsState.Stream;
        }

		// Copy the data from the Stream to the array buffer.
		// If the SqlChars doesn't hold a buffer or the buffer
		// is not big enough, allocate new char array.
		private void CopyStreamToBuffer() {
			Debug.Assert(FStream());

			long lStreamLen = m_stream.Length;
			if (lStreamLen >= x_lMaxLen)
                           throw new SqlTypeException(Res.GetString(Res.SqlMisc_BufferInsufficientMessage));

			if (m_rgchBuf == null || m_rgchBuf.Length < lStreamLen)
				m_rgchBuf = new char[lStreamLen];

			if (m_stream.Position != 0)
				m_stream.Seek(0, SeekOrigin.Begin);

			m_stream.Read(m_rgchBuf, 0, (int)lStreamLen);
			m_stream = null;
			m_lCurLen = lStreamLen;
			m_state = SqlBytesCharsState.Buffer;

			AssertValid();
        }

		private void SetBuffer(char[] buffer) {
			m_rgchBuf = buffer;
			m_lCurLen = (m_rgchBuf == null) ? x_lNull : (long)m_rgchBuf.Length;
			m_stream = null;
			m_state = (m_rgchBuf == null) ? SqlBytesCharsState.Null : SqlBytesCharsState.Buffer;

			AssertValid();
		}

		// --------------------------------------------------------------
		// 		XML Serialization
		// --------------------------------------------------------------


		XmlSchema IXmlSerializable.GetSchema() { 
			return null; 
		}
		
		void IXmlSerializable.ReadXml(XmlReader r) {
			char[] value = null;
			
 			string isNull = r.GetAttribute("nil", XmlSchema.InstanceNamespace);
 			
			if (isNull != null && XmlConvert.ToBoolean(isNull)) {
                // VSTFDevDiv# 479603 - SqlTypes read null value infinitely and never read the next value. Fix - Read the next value.
                r.ReadElementString();
                SetNull();
			}
			else {
				value = r.ReadElementString().ToCharArray();
				SetBuffer(value);
			}
		}

		void IXmlSerializable.WriteXml(XmlWriter writer) {
			if (IsNull) {
				writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
			}
			else {
				char[] value = this.Buffer;
				writer.WriteString(new String(value, 0, (int)(this.Length)));
			}
		}

		public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet) {
			return new XmlQualifiedName("string", XmlSchema.Namespace);
		}

		// --------------------------------------------------------------
		// 		Serialization using ISerializable
		// --------------------------------------------------------------

		// State information is not saved. The current state is converted to Buffer and only the underlying
		// array is serialized, except for Null, in which case this state is kept.
		[SecurityPermissionAttribute(SecurityAction.LinkDemand,SerializationFormatter=true)]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
			switch (m_state)
				{
				case SqlBytesCharsState.Null:
					info.AddValue("IsNull", true);
					break;

				case SqlBytesCharsState.Buffer:
					info.AddValue("IsNull", false);
					info.AddValue("data", m_rgchBuf);
					break;

				case SqlBytesCharsState.Stream:
					CopyStreamToBuffer();
					goto case SqlBytesCharsState.Buffer;

				default:
					Debug.Assert(false);
					goto case SqlBytesCharsState.Null;
				}
			}

		// --------------------------------------------------------------
		//	  Static fields, properties
		// --------------------------------------------------------------

		// Get a Null instance. 
		// Since SqlChars is mutable, have to be property and create a new one each time.
		public static SqlChars Null {
			get	{
				return new SqlChars((char[])null);
			}
		}
	} // class SqlChars

	// StreamOnSqlChars is a stream build on top of SqlChars, and
	// provides the Stream interface. The purpose is to help users
	// to read/write SqlChars object. 
	internal sealed class StreamOnSqlChars : SqlStreamChars
		{
		// --------------------------------------------------------------
		//	  Data members
		// --------------------------------------------------------------

		private SqlChars	m_sqlchars;		// the SqlChars object 
		private long		m_lPosition;

		// --------------------------------------------------------------
		//	  Constructor(s)
		// --------------------------------------------------------------

		internal StreamOnSqlChars(SqlChars s) {
			m_sqlchars = s;
			m_lPosition = 0;
        }

		// --------------------------------------------------------------
		//	  Public properties
		// --------------------------------------------------------------

		public override bool IsNull	{
			get	{
    			return m_sqlchars == null || m_sqlchars.IsNull;
            }
        }

		// Always can read/write/seek, unless sb is null, 
		// which means the stream has been closed.
		public override bool CanRead {
            get {
                return m_sqlchars != null && !m_sqlchars.IsNull;
            }
        }

		public override bool CanSeek {
			get	{
                return m_sqlchars != null;
            }
        }

		public override bool CanWrite {
			get {
				return m_sqlchars != null && (!m_sqlchars.IsNull || m_sqlchars.m_rgchBuf != null);
            }
        }

		public override long Length	{
			get	{
				CheckIfStreamClosed("get_Length");
				return m_sqlchars.Length;
            }
        }

		public override long Position {
			get	{
				CheckIfStreamClosed("get_Position");
				return m_lPosition;
            }
			set	{
				CheckIfStreamClosed("set_Position");
				if (value < 0 || value > m_sqlchars.Length)
					throw new ArgumentOutOfRangeException("value");
				else
					m_lPosition = value;
            }
        }

		// --------------------------------------------------------------
		//	  Public methods
		// --------------------------------------------------------------

		public override long Seek(long offset, SeekOrigin origin) {
			CheckIfStreamClosed("Seek");

			long lPosition = 0;

			switch(origin) {
				case SeekOrigin.Begin:
					if (offset < 0 || offset > m_sqlchars.Length)
						throw ADP.ArgumentOutOfRange("offset");
					m_lPosition = offset;
					break;
					
				case SeekOrigin.Current:
					lPosition = m_lPosition + offset;
					if (lPosition < 0 || lPosition > m_sqlchars.Length)
						throw ADP.ArgumentOutOfRange("offset");
					m_lPosition = lPosition;
					break;
					
				case SeekOrigin.End:
					lPosition = m_sqlchars.Length + offset;
					if (lPosition < 0 || lPosition > m_sqlchars.Length)
						throw ADP.ArgumentOutOfRange("offset");
					m_lPosition = lPosition;
					break;
					
				default:
                    throw ADP.ArgumentOutOfRange("offset");;
            }

			return m_lPosition;
        }

		// The Read/Write/Readchar/Writechar simply delegates to SqlChars
		public override int Read(char[] buffer, int offset, int count) {
			CheckIfStreamClosed("Read");

			if (buffer==null)
				throw new ArgumentNullException("buffer");
			if (offset < 0 || offset > buffer.Length)
				throw new ArgumentOutOfRangeException("offset");
			if (count < 0 || count > buffer.Length - offset)
				throw new ArgumentOutOfRangeException("count");

			int icharsRead = (int)m_sqlchars.Read(m_lPosition, buffer, offset, count);
			m_lPosition += icharsRead;

			return icharsRead;
        }

		public override void Write(char[] buffer, int offset, int count) {
			CheckIfStreamClosed("Write");

			if (buffer==null)
				throw new ArgumentNullException("buffer");
			if (offset < 0 || offset > buffer.Length)
				throw new ArgumentOutOfRangeException("offset");
			if (count < 0 || count > buffer.Length - offset)
				throw new ArgumentOutOfRangeException("count");

			m_sqlchars.Write(m_lPosition, buffer, offset, count);
			m_lPosition += count;
        }

		public override int ReadChar() {
    		CheckIfStreamClosed("ReadChar");

			// If at the end of stream, return -1, rather than call SqlChars.Readchar,
			// which will throw exception. This is the behavior for Stream.
			//
			if (m_lPosition >= m_sqlchars.Length)
				return -1;

			int ret = m_sqlchars[m_lPosition];
			m_lPosition ++;
			return ret;
        }

		public override void WriteChar(char value) {
			CheckIfStreamClosed("WriteChar");

			m_sqlchars[m_lPosition] = value;
			m_lPosition ++;
        }

		public override void SetLength(long value) {
			CheckIfStreamClosed("SetLength");

			m_sqlchars.SetLength(value);
			if (m_lPosition > value)
				m_lPosition = value;
        }

		// Flush is a no-op if underlying SqlChars is not a stream on SqlChars
		public override void Flush() {
			if (m_sqlchars.FStream())
				m_sqlchars.m_stream.Flush();
        }

        protected override void Dispose(bool disposing) {           
			// When m_sqlchars is null, it means the stream has been closed, and
			// any opearation in the future should fail.
			// This is the only case that m_sqlchars is null.
			m_sqlchars = null;
        }

		// --------------------------------------------------------------
		//	  Private utility functions
		// --------------------------------------------------------------

		private bool FClosed() {
			return m_sqlchars == null;
        }

        private void CheckIfStreamClosed(string methodname)	{
			if (FClosed())
                throw ADP.StreamClosed(methodname);
        }
    } // class StreamOnSqlChars
} // namespace System.Data.SqlTypes
