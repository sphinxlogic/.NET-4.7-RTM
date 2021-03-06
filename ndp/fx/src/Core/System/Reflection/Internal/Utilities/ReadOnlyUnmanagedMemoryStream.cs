// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection.Internal
{
    internal unsafe sealed class ReadOnlyUnmanagedMemoryStream : Stream
    {
        [SecurityCritical]
        private readonly byte* _data;

        private readonly int _length;
        private int _position;

        [SecurityCritical]
        public ReadOnlyUnmanagedMemoryStream(byte* data, int length)
        {
            _data = data;
            _length = length;
        }

        [SecuritySafeCritical]
        public unsafe override int ReadByte()
        {
            if (_position >= _length)
            {
                return -1;
            }

            return _data[_position++];
        }

        [SecuritySafeCritical]
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = Math.Min(count, _length - _position);
            Marshal.Copy((IntPtr)(_data + _position), buffer, offset, bytesRead);
            _position += bytesRead;
            return bytesRead;
        }

        public override void Flush()
        {
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get
            {
                return _position;
            }

            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long target;
            try
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        target = offset;
                        break;

                    case SeekOrigin.Current:
                        target = checked(offset + _position);
                        break;

                    case SeekOrigin.End:
                        target = checked(offset + _length);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin));
                }
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (target < 0 || target > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _position = (int)target;
            return target;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
