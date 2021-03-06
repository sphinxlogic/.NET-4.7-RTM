// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Class representing raw memory but not owning the memory.
    /// </summary>
    internal unsafe sealed class ExternalMemoryBlock : AbstractMemoryBlock
    {
        // keeps the owner of the memory alive as long as the block is alive:
        private readonly object _memoryOwner;

        [SecurityCritical]
        private byte* _buffer;

        private int _size;

        [SecurityCritical]
        public ExternalMemoryBlock(object memoryOwner, byte* buffer, int size)
        {
            _memoryOwner = memoryOwner;
            _buffer = buffer;
            _size = size;
        }

        [SecuritySafeCritical]
        public override void Dispose()
        {
            _buffer = null;
            _size = 0;
        }

        public override byte* Pointer
        {
            [SecurityCritical]
            get { return _buffer; }
        }

        public override int Size => _size;
    }
}
