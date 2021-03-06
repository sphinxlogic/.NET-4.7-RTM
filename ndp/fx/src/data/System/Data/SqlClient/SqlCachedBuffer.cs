//------------------------------------------------------------------------------
// <copyright file="SqlCachedBuffer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Data.SqlClient {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.Data.SqlTypes;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    // Caches the bytes returned from partial length prefixed datatypes, like XML
    sealed internal class SqlCachedBuffer : System.Data.SqlTypes.INullable{
        public static readonly SqlCachedBuffer Null = new SqlCachedBuffer();
        private const int _maxChunkSize = 2048;	// Arbitrary value for chunk size. Revisit this later for better perf
        
        private List<byte[]> _cachedBytes;

        private SqlCachedBuffer() {
            // For constructing Null
        }
        
        private SqlCachedBuffer(List<byte[]> cachedBytes) {
            _cachedBytes = cachedBytes;
        }

        internal List<byte[]> CachedBytes {
            get { return _cachedBytes;  }
        }

        // Reads off from the network buffer and caches bytes. Only reads one column value in the current row.
        static internal bool TryCreate(SqlMetaDataPriv metadata, TdsParser parser, TdsParserStateObject stateObj, out SqlCachedBuffer buffer) {
            int cb = 0;
            ulong  plplength;
            byte[] byteArr;
            
            List<byte[]> cachedBytes = new List<byte[]>();
            buffer = null;

            // the very first length is already read.
            if (!parser.TryPlpBytesLeft(stateObj, out plplength)) {
                return false;
            }
            // For now we  only handle Plp data from the parser directly.
            Debug.Assert(metadata.metaType.IsPlp, "SqlCachedBuffer call on a non-plp data");
            do {
                if (plplength == 0) 
                    break;
                do {
                    cb = (plplength > (ulong) _maxChunkSize) ?  _maxChunkSize : (int)plplength ;
                    byteArr = new byte[cb];
                    if (!stateObj.TryReadPlpBytes(ref byteArr, 0, cb, out cb)) {
                        return false;
                    }
                    Debug.Assert(cb == byteArr.Length);
                    if (cachedBytes.Count == 0) {
                        // Add the Byte order mark if needed if we read the first array
                        AddByteOrderMark(byteArr, cachedBytes);
                    }
                    cachedBytes.Add(byteArr);
                    plplength -= (ulong)cb;
                } while (plplength > 0);
                if (!parser.TryPlpBytesLeft(stateObj, out plplength)) {
                    return false;
                }
            } while (plplength > 0);                    
            Debug.Assert(stateObj._longlen == 0 && stateObj._longlenleft == 0);

            buffer = new SqlCachedBuffer(cachedBytes);
            return true;
        }

        private static void AddByteOrderMark(byte[] byteArr, List<byte[]> cachedBytes) {
            // Need to find out if we should add byte order mark or not. 
            // We need to add this if we are getting ntext xml, not if we are getting binary xml
            // Binary Xml always begins with the bytes 0xDF and 0xFF
            // If we aren't getting these, then we are getting unicode xml
            if ((byteArr.Length < 2 ) || (byteArr[0] != 0xDF) || (byteArr[1] != 0xFF)){
                Debug.Assert(cachedBytes.Count == 0);
                cachedBytes.Add(TdsEnums.XMLUNICODEBOMBYTES);
            }
        }
        
        internal Stream ToStream() {
            return new SqlCachedStream(this);
        }
        
        override public string ToString() {
            if (IsNull)
                throw new SqlNullValueException();

            if (_cachedBytes.Count == 0) {
                return String.Empty;
            }
            SqlXml   sxml = new SqlXml(ToStream());
            return sxml.Value;
        }

        internal SqlString ToSqlString() {
            if (IsNull)
                return SqlString.Null;
            string str = ToString();
            return new SqlString(str);
        }

        internal SqlXml ToSqlXml() {
            SqlXml  sx = new SqlXml(ToStream());
            return sx;
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal XmlReader ToXmlReader() {
            //XmlTextReader xr = new XmlTextReader(fragment, XmlNodeType.Element, null);
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;

            // Call internal XmlReader.CreateSqlReader from System.Xml.
            // Signature: internal static XmlReader CreateSqlReader(Stream input, XmlReaderSettings settings, XmlParserContext inputContext);
            MethodInfo createSqlReaderMethodInfo = typeof(System.Xml.XmlReader).GetMethod("CreateSqlReader", BindingFlags.Static | BindingFlags.NonPublic);
            object[] args = new object[3] { ToStream(), readerSettings, null };
            XmlReader xr;

            new System.Security.Permissions.ReflectionPermission(System.Security.Permissions.ReflectionPermissionFlag.MemberAccess).Assert();
            try {
                xr = (XmlReader)createSqlReaderMethodInfo.Invoke(null, args);
            }
            finally {
                System.Security.Permissions.ReflectionPermission.RevertAssert();
            }
            return xr;
        }

        public bool IsNull {
            get {
                return (_cachedBytes == null) ? true : false ;
            }
        }

    }
    
}
