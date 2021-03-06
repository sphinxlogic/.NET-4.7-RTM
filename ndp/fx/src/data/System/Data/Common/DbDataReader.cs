//------------------------------------------------------------------------------
// <copyright file="DbDataReader.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data.Common {

    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.IO;
    using System.Threading.Tasks;
    using System.Threading;
    
    public abstract class DbDataReader : MarshalByRefObject, IDataReader, IEnumerable { // V1.2.3300    
        protected DbDataReader() : base() {
        }
        
        abstract public int Depth {
            get;
        }
    
        abstract public int FieldCount { 
            get;
        }
        
        abstract public bool HasRows {
            get;
        }
        
        abstract public bool IsClosed {
            get;
        }
    
        abstract public int RecordsAffected {
            get;
        }
        
        virtual public int VisibleFieldCount { 
            // NOTE: This is virtual because not all providers may choose to support
            //       this property, since it was added in Whidbey
            get {
                return FieldCount;
            }
        }
        
        abstract public object this [ int ordinal ] {
            get;
        }
        
        abstract public object this [ string name ] {
            get;
        }

        virtual public void Close()
        {
        }

        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        public void Dispose() {
            Dispose(true);
        }
    
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                Close();
            }
        }

        abstract public string GetDataTypeName(int ordinal);

        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        abstract public IEnumerator GetEnumerator();
        
        abstract public Type GetFieldType(int ordinal);
          
        abstract public string GetName(int ordinal);
        
        abstract public int GetOrdinal(string name);

        virtual public DataTable GetSchemaTable()
        {
            throw new NotSupportedException();
        }
        
        abstract public bool GetBoolean(int ordinal);
        
        abstract public byte GetByte(int ordinal);
        
        abstract public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length);
        
        abstract public char GetChar(int ordinal);
        
        abstract public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);
        
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        public DbDataReader GetData(int ordinal) {
            return GetDbDataReader(ordinal);
        }
        
        IDataReader IDataRecord.GetData(int ordinal) {
            return GetDbDataReader(ordinal);
        }
        
        virtual protected DbDataReader GetDbDataReader(int ordinal) {
            // NOTE: This method is virtual because we're required to implement
            //       it however most providers won't support it. Only the OLE DB 
            //       provider supports it right now, and they can override it.
            throw ADP.NotSupported();
        }
        
        abstract public DateTime GetDateTime(int ordinal);
        
        abstract public Decimal GetDecimal(int ordinal);
        
        abstract public double GetDouble(int ordinal);
        
        abstract public float GetFloat(int ordinal);
        
        abstract public Guid GetGuid(int ordinal);
        
        abstract public Int16 GetInt16(int ordinal);
        
        abstract public Int32 GetInt32(int ordinal);
        
        abstract public Int64 GetInt64(int ordinal);
        
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        virtual public Type GetProviderSpecificFieldType(int ordinal) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey.
            return GetFieldType(ordinal);
        }
        
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        virtual public Object GetProviderSpecificValue(int ordinal) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey
            return GetValue(ordinal);
        }
        
        [
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        virtual public int GetProviderSpecificValues(object[] values) {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey
            return GetValues(values);
        }

        abstract public String GetString(int ordinal);

        virtual public Stream GetStream(int ordinal) {
            using (MemoryStream bufferStream = new MemoryStream())
            {
                long bytesRead = 0;
                long bytesReadTotal = 0;
                byte[] buffer = new byte[4096];
                do {
                    bytesRead = GetBytes(ordinal, bytesReadTotal, buffer, 0, buffer.Length);
                    bufferStream.Write(buffer, 0, (int)bytesRead);
                    bytesReadTotal += bytesRead;
                } while (bytesRead > 0);
                
                return new MemoryStream(bufferStream.ToArray(), false);
            }
        }

        virtual public TextReader GetTextReader(int ordinal) {
            if (IsDBNull(ordinal)) {
                return new StringReader(String.Empty);
            }
            else {
                return new StringReader(GetString(ordinal));
            }
        }
        
        abstract public Object GetValue(int ordinal);

        virtual public T GetFieldValue<T>(int ordinal) {
            return (T)GetValue(ordinal);
        }

        public Task<T> GetFieldValueAsync<T>(int ordinal) {
            return GetFieldValueAsync<T>(ordinal, CancellationToken.None);
        }

        virtual public Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return ADP.CreatedTaskWithCancellation<T>();
            }
            else {
                try {
                    return Task.FromResult<T>(GetFieldValue<T>(ordinal));
                }
                catch (Exception e) {
                    return ADP.CreatedTaskWithException<T>(e);
                }
            }
        }
        
        abstract public int GetValues(object[] values);
        
        abstract public bool IsDBNull(int ordinal);

        public Task<bool> IsDBNullAsync(int ordinal) {
            return IsDBNullAsync(ordinal, CancellationToken.None);
        }

        virtual public Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return ADP.CreatedTaskWithCancellation<bool>();
            }
            else {
                try {
                    return IsDBNull(ordinal) ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e) {
                    return ADP.CreatedTaskWithException<bool>(e);
                }
            }
        }
        
        abstract public bool NextResult();
    
        abstract public bool Read();

        public Task<bool> ReadAsync() {
            return ReadAsync(CancellationToken.None);
        }

        virtual public Task<bool> ReadAsync(CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return ADP.CreatedTaskWithCancellation<bool>();
            }
            else {
                try {
                    return Read() ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e) {
                    return ADP.CreatedTaskWithException<bool>(e);
                }
            }
        }
        
        public Task<bool> NextResultAsync() {
            return NextResultAsync(CancellationToken.None);
        }

        virtual public Task<bool> NextResultAsync(CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return ADP.CreatedTaskWithCancellation<bool>();
            }
            else {
                try {
                    return NextResult() ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e) {
                    return ADP.CreatedTaskWithException<bool>(e);
                }
            }
        }
    }

}
