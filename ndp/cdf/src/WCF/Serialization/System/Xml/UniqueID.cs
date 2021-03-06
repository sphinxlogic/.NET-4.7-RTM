//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace System.Xml
{
    using System.Runtime;
    using System.Runtime.Serialization; // for SR
    using System.Security;

    public class UniqueId
    {
        Int64 idLow;
        Int64 idHigh;

        [Fx.Tag.SecurityNote(Critical = "Some SecurityCritical unsafe code assumes that this field has been validated.")]
        [SecurityCritical]
        string s;
        const int guidLength = 16;
        const int uuidLength = 45;

        static short[] char2val = new short[256]
        {
                /*    0-15 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   16-31 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   32-47 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   48-63 */ 0x000, 0x010, 0x020, 0x030, 0x040, 0x050, 0x060, 0x070, 0x080, 0x090, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   64-79 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   80-95 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*  96-111 */ 0x100, 0x0A0, 0x0B0, 0x0C0, 0x0D0, 0x0E0, 0x0F0, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /* 112-127 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,

                /*    0-15 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   16-31 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   32-47 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   48-63 */ 0x000, 0x001, 0x002, 0x003, 0x004, 0x005, 0x006, 0x007, 0x008, 0x009, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   64-79 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*   80-95 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /*  96-111 */ 0x100, 0x00A, 0x00B, 0x00C, 0x00D, 0x00E, 0x00F, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
                /* 112-127 */ 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100, 0x100,
        };

        const string val2char = "0123456789abcdef";

        public UniqueId() : this(Guid.NewGuid())
        {
        }

        public UniqueId(Guid guid) : this(guid.ToByteArray())
        {
        }

        public UniqueId(byte[] guid) : this(guid, 0)
        {
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code.",
            Safe = "Unsafe code is effectively encapsulated, all inputs are validated.")]
        [SecuritySafeCritical]
        unsafe public UniqueId(byte[] guid, int offset)
        {
            if (guid == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("guid"));
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.ValueMustBeNonNegative)));
            if (offset > guid.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.OffsetExceedsBufferSize, guid.Length)));
            if (guidLength > guid.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.GetString(SR.XmlArrayTooSmallInput, guidLength), "guid"));
            fixed (byte* pb = &guid[offset])
            {
                this.idLow = UnsafeGetInt64(pb);
                this.idHigh = UnsafeGetInt64(&pb[8]);
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code.",
            Safe = "Unsafe code is effectively encapsulated, all inputs are validated.")]
        [SecuritySafeCritical]
        unsafe public UniqueId(string value)
        {
            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            if (value.Length == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SR.GetString(SR.XmlInvalidUniqueId)));
            fixed (char* pch = value)
            {
                UnsafeParse(pch, value.Length);
            }
            this.s = value;
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code.",
            Safe = "Unsafe code is effectively encapsulated, all inputs are validated.")]
        [SecuritySafeCritical]
        unsafe public UniqueId(char[] chars, int offset, int count)
        {
            if (chars == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.ValueMustBeNonNegative)));
            if (offset > chars.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.OffsetExceedsBufferSize, chars.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", SR.GetString(SR.ValueMustBeNonNegative)));
            if (count > chars.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", SR.GetString(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset)));
            if (count == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SR.GetString(SR.XmlInvalidUniqueId)));
            fixed (char* pch = &chars[offset])
            {
                UnsafeParse(pch, count);
            }
            if (!IsGuid)
            {
                this.s = new string(chars, offset, count);
            }
        }

        
        public int CharArrayLength
        {
            [Fx.Tag.SecurityNote(Critical = "Accesses critical field 's'.",
                Safe = "Doesn't leak any control or data.")]
            [SecuritySafeCritical]
            get
            {
                if (s != null)
                    return s.Length;

                return uuidLength;
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code. Caller needs to validate arguments.")]
        [SecurityCritical]
        unsafe int UnsafeDecode(short* char2val, char ch1, char ch2)
        {
            if ((ch1 | ch2) >= 0x80)
                return 0x100;

            return char2val[ch1] | char2val[0x80 + ch2];
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code. Caller needs to validate arguments.")]
        [SecurityCritical]
        unsafe void UnsafeEncode(char* val2char, byte b, char* pch)
        {
            pch[0] = val2char[b >> 4];
            pch[1] = val2char[b & 0x0F];
        }

        public bool IsGuid
        {
            get
            {
                return ((idLow | idHigh) != 0);
            }
        }

        // It must be the case that comparing UniqueId's as strings yields the same result as comparing UniqueId's as
        // their binary equivalent.  This means that there must be a 1-1 relationship between a string and its binary
        // equivalent.  Therefore, for example, we cannot accept both upper and lower case hex chars since there would
        // then be more than 1 string that mapped to a binary equivalent.
        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code. Caller needs to validate arguments.")]
        [SecurityCritical]
        unsafe void UnsafeParse(char* chars, int charCount)
        {
            //           1         2         3         4
            // 012345678901234567890123456789012345678901234
            // urn:uuid:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

            if (charCount != uuidLength ||
                chars[0] != 'u' || chars[1] != 'r' || chars[2] != 'n' || chars[3] != ':' ||
                chars[4] != 'u' || chars[5] != 'u' || chars[6] != 'i' || chars[7] != 'd' || chars[8] != ':' ||
                chars[17] != '-' || chars[22] != '-' || chars[27] != '-' || chars[32] != '-')
            {
                return;
            }

            byte* bytes = stackalloc byte[guidLength];

            int i = 0;
            int j = 0;
            fixed (short* ps = char2val)
            {
                short* _char2val = ps;

                //   0         1         2         3         4
                //   012345678901234567890123456789012345678901234
                //   urn:uuid:aabbccdd-eeff-gghh-0011-223344556677
                // 
                //   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
                //   ddccbbaaffeehhgg0011223344556677

                i = UnsafeDecode(_char2val, chars[15], chars[16]); bytes[0]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[13], chars[14]); bytes[1]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[11], chars[12]); bytes[2]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[9],  chars[10]); bytes[3]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[20], chars[21]); bytes[4]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[18], chars[19]); bytes[5]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[25], chars[26]); bytes[6]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[23], chars[24]); bytes[7]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[28], chars[29]); bytes[8]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[30], chars[31]); bytes[9]  = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[33], chars[34]); bytes[10] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[35], chars[36]); bytes[11] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[37], chars[38]); bytes[12] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[39], chars[40]); bytes[13] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[41], chars[42]); bytes[14] = (byte)i; j |= i;
                i = UnsafeDecode(_char2val, chars[43], chars[44]); bytes[15] = (byte)i; j |= i;

                if (j >= 0x100)
                    return;

                this.idLow = UnsafeGetInt64(bytes);
                this.idHigh = UnsafeGetInt64(&bytes[8]);
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code.",
            Safe = "Unsafe code is effectively encapsulated, all inputs are validated.")]
        [SecuritySafeCritical]
        unsafe public int ToCharArray(char[] chars, int offset)
        {
            int count = CharArrayLength;

            if (chars == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));

            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.ValueMustBeNonNegative)));
            if (offset > chars.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.OffsetExceedsBufferSize, chars.Length)));

            if (count > chars.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("chars", SR.GetString(SR.XmlArrayTooSmallOutput, count)));

            if (s != null)
            {
                s.CopyTo(0, chars, offset, count);
            }
            else
            {
                byte* bytes = stackalloc byte[guidLength];
                UnsafeSetInt64(this.idLow, bytes);
                UnsafeSetInt64(this.idHigh, &bytes[8]);

                fixed (char* _pch = &chars[offset])
                {
                    char* pch = _pch;
                    pch[0] = 'u';
                    pch[1] = 'r';
                    pch[2] = 'n';
                    pch[3] = ':';
                    pch[4] = 'u';
                    pch[5] = 'u';
                    pch[6] = 'i';
                    pch[7] = 'd';
                    pch[8] = ':';
                    pch[17] = '-';
                    pch[22] = '-';
                    pch[27] = '-';
                    pch[32] = '-';

                    fixed (char* ps = val2char)
                    {
                        char* _val2char = ps;
                        UnsafeEncode(_val2char, bytes[0], &pch[15]);
                        UnsafeEncode(_val2char, bytes[1], &pch[13]);
                        UnsafeEncode(_val2char, bytes[2], &pch[11]);
                        UnsafeEncode(_val2char, bytes[3], &pch[9]);
                        UnsafeEncode(_val2char, bytes[4], &pch[20]);
                        UnsafeEncode(_val2char, bytes[5], &pch[18]);
                        UnsafeEncode(_val2char, bytes[6], &pch[25]);
                        UnsafeEncode(_val2char, bytes[7], &pch[23]);
                        UnsafeEncode(_val2char, bytes[8], &pch[28]);
                        UnsafeEncode(_val2char, bytes[9], &pch[30]);
                        UnsafeEncode(_val2char, bytes[10], &pch[33]);
                        UnsafeEncode(_val2char, bytes[11], &pch[35]);
                        UnsafeEncode(_val2char, bytes[12], &pch[37]);
                        UnsafeEncode(_val2char, bytes[13], &pch[39]);
                        UnsafeEncode(_val2char, bytes[14], &pch[41]);
                        UnsafeEncode(_val2char, bytes[15], &pch[43]);
                    }
                }
            }

            return count;
        }

        public bool TryGetGuid(out Guid guid)
        {
            byte[] buffer = new byte[guidLength];
            if (!TryGetGuid(buffer, 0))
            {
                guid = Guid.Empty;
                return false;
            }

            guid = new Guid(buffer);
            return true;
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code.",
            Safe = "Unsafe code is effectively encapsulated, all inputs are validated.")]
        [SecuritySafeCritical]
        unsafe public bool TryGetGuid(byte[] buffer, int offset)
        {
            if (!IsGuid)
                return false;

            if (buffer == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("buffer"));

            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.ValueMustBeNonNegative)));
            if (offset > buffer.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", SR.GetString(SR.OffsetExceedsBufferSize, buffer.Length)));

            if (guidLength > buffer.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("buffer", SR.GetString(SR.XmlArrayTooSmallOutput, guidLength)));

            fixed (byte* pb = &buffer[offset])
            {
                UnsafeSetInt64(this.idLow, pb);
                UnsafeSetInt64(this.idHigh, &pb[8]);
            }

            return true;
        }

        [Fx.Tag.SecurityNote(Critical = "Accesses critical field 's'.",
            Safe = "Doesn't allow unchecked write access to the field.")]
        [SecuritySafeCritical]
        unsafe public override string ToString()
        {
            if (s == null)
            {
                int length = CharArrayLength;
                char[] chars = new char[length];
                ToCharArray(chars, 0);
                s = new string(chars, 0, length);
            }
            return s;
        }

        static public bool operator ==(UniqueId id1, UniqueId id2)
        {
            if (object.ReferenceEquals(id1, null) && object.ReferenceEquals(id2, null))
                return true;

            if (object.ReferenceEquals(id1, null) || object.ReferenceEquals(id2, null))
                return false;

#pragma warning suppress 56506 // Microsoft, checks for whether id1 and id2 are null done above.
            if (id1.IsGuid && id2.IsGuid)
            {
                return id1.idLow == id2.idLow && id1.idHigh == id2.idHigh;
            }

            return id1.ToString() == id2.ToString();
        }

        static public bool operator !=(UniqueId id1, UniqueId id2)
        {
            return !(id1 == id2);
        }

        public override bool Equals(object obj)
        {
            return this == (obj as UniqueId);
        }

        public override int GetHashCode()
        {
            if (IsGuid)
            {
                Int64 hash = (idLow ^ idHigh);
                return ((int)(hash >> 32)) ^ ((int)hash);
            }
            else
            {
                return ToString().GetHashCode();
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code. Caller needs to validate arguments.")]
        [SecurityCritical]
        unsafe Int64 UnsafeGetInt64(byte* pb)
        {
            Int32 idLow = UnsafeGetInt32(pb);
            Int32 idHigh = UnsafeGetInt32(&pb[4]);
            return (((Int64)idHigh) << 32) | ((UInt32)idLow);
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code. Caller needs to validate arguments.")]
        [SecurityCritical]
        unsafe Int32 UnsafeGetInt32(byte* pb)
        {
            int value = pb[3];
            value <<= 8;
            value |= pb[2];
            value <<= 8;
            value |= pb[1];
            value <<= 8;
            value |= pb[0];
            return value;
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code. Caller needs to validate arguments.")]
        [SecurityCritical]
        unsafe void UnsafeSetInt64(Int64 value, byte* pb)
        {
            UnsafeSetInt32((int)value, pb);
            UnsafeSetInt32((int)(value >> 32), &pb[4]);
        }

        [Fx.Tag.SecurityNote(Critical = "Contains unsafe code. Caller needs to validate arguments.")]
        [SecurityCritical]
        unsafe void UnsafeSetInt32(Int32 value, byte* pb)
        {
            pb[0] = (byte)value;
            value >>= 8;
            pb[1] = (byte)value;
            value >>= 8;
            pb[2] = (byte)value;
            value >>= 8;
            pb[3] = (byte)value;
        }
    }
}
