//-----------------------------------------------------------------------
//
//  Microsoft Windows Client Platform
//  Copyright (C) Microsoft Corporation, 2003
//
//  File:      CharacterBufferReference.cs
//
//  Contents:  Text Character buffer reference
//
//  Spec:      http://team/sites/Avalon/Specs/Text%20Formatting%20API.doc
//
//  Created:   2-5-2004 Worachai Chaoweeraprasit (wchao)
//
//------------------------------------------------------------------------


using System;
using System.Diagnostics;
using MS.Internal;
using System.Security;
using System.Security.Permissions;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Text character buffer reference
    /// </summary>
    public struct CharacterBufferReference : IEquatable<CharacterBufferReference>
    {
        private CharacterBuffer     _charBuffer;
        private int                 _offsetToFirstChar;

        #region Constructor

        /// <summary>
        /// Construct character buffer reference from character array
        /// </summary>
        /// <param name="characterArray">character array</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        public CharacterBufferReference(
            char[]      characterArray,
            int         offsetToFirstChar
            )
            : this(
                new CharArrayCharacterBuffer(characterArray),
                offsetToFirstChar
                )
        {}


        /// <summary>
        /// Construct character buffer reference from string
        /// </summary>
        /// <param name="characterString">character string</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        public CharacterBufferReference(
            string      characterString,
            int         offsetToFirstChar
            )
            : this(
                new StringCharacterBuffer(characterString),
                offsetToFirstChar
                )
        {}


        /// <summary>
        /// Construct character buffer reference from unsafe character string
        /// </summary>
        /// <param name="unsafeCharacterString">pointer to character string</param>
        /// <param name="characterLength">character length of unsafe string</param>
        /// <SecurityNote>
        /// Critical: This manipulates unsafe pointers and calls into the critical UnsafeStringCharacterBuffer ctor.
        /// PublicOK: The caller needs unmanaged code permission in order to pass unsafe pointers to us.
        /// </SecurityNote>
        [SecurityCritical]
        [CLSCompliant(false)]
        public unsafe CharacterBufferReference(
            char*       unsafeCharacterString,
            int         characterLength
            )
            : this(new UnsafeStringCharacterBuffer(unsafeCharacterString, characterLength), 0)
        {}


        /// <summary>
        /// Construct character buffer reference from memory buffer
        /// </summary>
        internal CharacterBufferReference(
            CharacterBuffer     charBuffer,
            int                 offsetToFirstChar
            )
        {
            if (offsetToFirstChar < 0)
            {
                throw new ArgumentOutOfRangeException("offsetToFirstChar", SR.Get(SRID.ParameterCannotBeNegative));
            }

            // maximum offset is one less than CharacterBuffer.Count, except that zero is always a valid offset
            // even in the case of an empty or null character buffer
            int maxOffset = (charBuffer == null) ? 0 : Math.Max(0, charBuffer.Count - 1);
            if (offsetToFirstChar > maxOffset)
            {
                throw new ArgumentOutOfRangeException("offsetToFirstChar", SR.Get(SRID.ParameterCannotBeGreaterThan, maxOffset));
            }

            _charBuffer = charBuffer;
            _offsetToFirstChar = offsetToFirstChar;
        }

        #endregion


        /// <summary>
        /// Compute hash code
        /// </summary>
        public override int GetHashCode()
        {
            return _charBuffer != null ? _charBuffer.GetHashCode() ^ _offsetToFirstChar : 0;
        }


        /// <summary>
        /// Test equality with the input object 
        /// </summary>
        /// <param name="obj"> The object to test. </param>
        public override bool Equals(object obj)
        {
            if (obj is CharacterBufferReference)
            {
                return Equals((CharacterBufferReference)obj);
            }
            return false;
        }


        /// <summary>
        /// Test equality with the input CharacterBufferReference
        /// </summary>
        /// <param name="value"> The characterBufferReference value to test </param>
        public bool Equals(CharacterBufferReference value)
        {
            return  _charBuffer == value._charBuffer
                &&  _offsetToFirstChar == value._offsetToFirstChar;
        }

        /// <summary>
        /// Compare two CharacterBufferReference for equality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator == (
            CharacterBufferReference  left,
            CharacterBufferReference  right
            )
        {
            return left.Equals(right);
        }

        
        /// <summary>
        /// Compare two CharacterBufferReference for inequality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator != (
            CharacterBufferReference  left,
            CharacterBufferReference  right
            )
        {
            return !(left == right);
        }


        internal CharacterBuffer CharacterBuffer
        {
            get { return _charBuffer; }
        }

        internal int OffsetToFirstChar
        {
            get { return _offsetToFirstChar; }
        }
    }
}

