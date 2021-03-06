//------------------------------------------------------------------------------
// <copyright file="BinHexDecoder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace System.Xml
{
    internal class BinHexDecoder : IncrementalReadDecoder {
//
// Fields
//
        byte[]  buffer;
        int     startIndex;
        int     curIndex;
        int     endIndex;
        bool    hasHalfByteCached;
        byte    cachedHalfByte;

//
// IncrementalReadDecoder interface
//
        internal override int DecodedCount { 
            get { 
                return curIndex - startIndex; 
            } 
        }

        internal override bool IsFull { 
            get { 
                return curIndex == endIndex; 
            }
        }

#if SILVERLIGHT && !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecuritySafeCritical]
#endif
        internal override unsafe int Decode(char[] chars, int startPos, int len) {
            if ( chars == null ) {
                throw new ArgumentNullException( "chars" );
            }
            if ( len < 0 ) {
                throw new ArgumentOutOfRangeException( "len" );
            }
            if ( startPos < 0 ) {
                throw new ArgumentOutOfRangeException( "startPos" );
            }
            if ( chars.Length - startPos < len ) {
                throw new ArgumentOutOfRangeException( "len" );
            }

            if ( len == 0 ) {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed ( char* pChars = &chars[startPos] ) {
                fixed ( byte* pBytes = &buffer[curIndex] ) {
                    Decode( pChars, pChars + len, pBytes, pBytes + ( endIndex - curIndex ),  
                            ref this.hasHalfByteCached, ref this.cachedHalfByte, out charsDecoded, out bytesDecoded );
                }
            }
            curIndex += bytesDecoded;
            return charsDecoded;
        }

#if SILVERLIGHT && !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecuritySafeCritical]
#endif
        internal override unsafe int Decode(string str, int startPos, int len) {
            if ( str == null ) {
                throw new ArgumentNullException( "str" );
            }
            if ( len < 0 ) {
                throw new ArgumentOutOfRangeException( "len" );
            }
            if ( startPos < 0 ) {
                throw new ArgumentOutOfRangeException( "startPos" );
            }
            if ( str.Length - startPos < len ) {
                throw new ArgumentOutOfRangeException( "len" );
            }

            if ( len == 0 ) {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed ( char* pChars = str ) {
                fixed ( byte* pBytes = &buffer[curIndex] ) {
                    Decode( pChars + startPos, pChars + startPos + len, pBytes, pBytes + ( endIndex - curIndex ),  
                            ref this.hasHalfByteCached, ref this.cachedHalfByte, out charsDecoded, out bytesDecoded );
                }
            }
            curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override void Reset() {
            this.hasHalfByteCached = false;
            this.cachedHalfByte = 0;
        }

        internal override void SetNextOutputBuffer( Array buffer, int index, int count ) {
            Debug.Assert( buffer != null );
            Debug.Assert( count >= 0 );
            Debug.Assert( index >= 0 );
            Debug.Assert( buffer.Length - index >= count );
            Debug.Assert( ( buffer as byte[] ) != null );

            this.buffer = (byte[])buffer;
            this.startIndex = index;
            this.curIndex = index;
            this.endIndex = index + count;
        }

//
// Static methods
//
#if SILVERLIGHT && !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecuritySafeCritical]
#endif
        public static unsafe byte[] Decode(char[] chars, bool allowOddChars) {
            if ( chars == null ) {
                throw new ArgumentNullException( "chars" );
            }
            
            int len = chars.Length;
            if ( len == 0 ) {
                return new byte[0];
            }

            byte[] bytes = new byte[ ( len + 1 ) / 2 ];
            int bytesDecoded, charsDecoded;
            bool hasHalfByteCached = false;
            byte cachedHalfByte = 0;
            
            fixed ( char* pChars = &chars[0] ) {
                fixed ( byte* pBytes = &bytes[0] ) {
                    Decode( pChars, pChars + len, pBytes, pBytes + bytes.Length, ref hasHalfByteCached, ref cachedHalfByte, out charsDecoded, out bytesDecoded );
                }
            }

            if ( hasHalfByteCached && !allowOddChars ) {
                throw new XmlException( Res.Xml_InvalidBinHexValueOddCount, new string( chars ) );
            }

            if ( bytesDecoded < bytes.Length ) {
                byte[] tmp = new byte[ bytesDecoded ];
                Array.Copy( bytes, 0, tmp, 0, bytesDecoded );
                bytes = tmp;
            }

            return bytes;
        }

//
// Private methods
//

#if SILVERLIGHT && !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecurityCritical]
#endif
        private static unsafe void Decode(char* pChars, char* pCharsEndPos, 
                                    byte* pBytes, byte* pBytesEndPos, 
                                    ref bool hasHalfByteCached, ref byte cachedHalfByte,
								    out int charsDecoded, out int bytesDecoded ) {
#if DEBUG
            Debug.Assert( pCharsEndPos - pChars >= 0 );
            Debug.Assert( pBytesEndPos - pBytes >= 0 );
#endif

            char* pChar = pChars;
            byte* pByte = pBytes;
            XmlCharType xmlCharType = XmlCharType.Instance;
            while ( pChar < pCharsEndPos && pByte < pBytesEndPos ) {
                byte halfByte;
                char ch = *pChar++;

                if ( ch >= 'a' && ch <= 'f' ) {
                    halfByte = (byte)(ch - 'a' + 10);
                }
                else if ( ch >= 'A' && ch <= 'F' ) {
                    halfByte = (byte)(ch - 'A' + 10);
                }
                else if ( ch >= '0' && ch <= '9' ) {
                    halfByte = (byte)(ch - '0');
                }
                else if ( ( xmlCharType.charProperties[ch] & XmlCharType.fWhitespace ) != 0 ) { // else if ( xmlCharType.IsWhiteSpace( ch ) ) {
                    continue; 
                }
                else {
                    throw new XmlException( Res.Xml_InvalidBinHexValue, new string( pChars, 0, (int)( pCharsEndPos - pChars ) ) );
                }

                if ( hasHalfByteCached ) {
                    *pByte++ = (byte)( ( cachedHalfByte << 4 ) + halfByte );
                    hasHalfByteCached = false;
                }
                else {
                    cachedHalfByte = halfByte;
                    hasHalfByteCached = true;
                }
            }

            bytesDecoded = (int)(pByte - pBytes);
            charsDecoded = (int)(pChar - pChars);
        }
    }
}
