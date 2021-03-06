//------------------------------------------------------------------------------
// <copyright file="MarvinHash.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace System
{
    internal static class MarvinHash
    {
        /// <summary>
        /// Convenience method to compute a Marvin hash and collapse it into a 32-bit hash from a string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int ComputeHash32(string key, ulong seed)
        {
            Debug.Assert(key != null);
            Debug.Assert(key.Length >= 0);

            int hash;
            fixed (char* data = key)
                hash = ComputeHash32((byte*)data, 2 * key.Length, seed);
            return hash;
        }

        /// <summary>
        /// Convenience method to compute a Marvin hash and collapse it into a 32-bit hash from a string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int ComputeHash32(char[] key, int start, int len, ulong seed)
        {
            Debug.Assert(key != null);
            Debug.Assert(start >= 0);
            Debug.Assert(len >= 0);
            Debug.Assert((long)start + len <= key.Length);

            int hash;
            fixed (char* data = &key[start])
                hash = ComputeHash32((byte*)data, 2 * len, seed);
            return hash;
        }

        /// <summary>
        /// Convenience method to compute a Marvin hash and collapse it into a 32-bit hash.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static int ComputeHash32(byte* data, int count, ulong seed)
        {
            long hash64 = ComputeHash(data, count, seed);
            return ((int)(hash64 >> 32)) ^ (int)hash64;
        }

        /// <summary>
        /// Computes a 64-hash using the Marvin algorithm.
        /// </summary>
        private unsafe static long ComputeHash(byte* data, int count, ulong seed)
        {
            uint ucount = (uint)count;
            uint p0 = (uint)seed;
            uint p1 = (uint)(seed >> 32);

            int byteOffset = 0;

            while (ucount >= 8)
            {
                p0 += *(uint*)(data + byteOffset);
                Block(ref p0, ref p1);

                p0 += *(uint*)(data + byteOffset + 4);
                Block(ref p0, ref p1);

                byteOffset += 8;
                ucount -= 8;
            }

            switch (ucount)
            {
                case 4:
                    p0 += *(uint*)(data + byteOffset);
                    Block(ref p0, ref p1);
                    goto case 0;

                case 0:
                    p0 += 0x80u;
                    break;

                case 5:
                    p0 += *(uint*)(data + byteOffset);
                    byteOffset += 4;
                    Block(ref p0, ref p1);
                    goto case 1;

                case 1:
                    p0 += 0x8000u | *(data + byteOffset);
                    break;

                case 6:
                    p0 += *(uint*)(data + byteOffset);
                    byteOffset += 4;
                    Block(ref p0, ref p1);
                    goto case 2;

                case 2:
                    p0 += 0x800000u | *(ushort*)(data + byteOffset);
                    break;

                case 7:
                    p0 += *(uint*)(data + byteOffset);
                    byteOffset += 4;
                    Block(ref p0, ref p1);
                    goto case 3;

                case 3:
                    p0 += 0x80000000u | (uint)*(data + byteOffset + 2) << 16 | *(ushort*)(data + byteOffset);
                    break;

                default:
                    Debug.Fail("Should not get here.");
                    break;
            }

            Block(ref p0, ref p1);
            Block(ref p0, ref p1);

            return (((long)p1) << 32) | p0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Block(ref uint rp0, ref uint rp1)
        {
            uint p0 = rp0;
            uint p1 = rp1;

            p1 ^= p0;
            p0 = _rotl(p0, 20);

            p0 += p1;
            p1 = _rotl(p1, 9);

            p1 ^= p0;
            p0 = _rotl(p0, 27);

            p0 += p1;
            p1 = _rotl(p1, 19);

            rp0 = p0;
            rp1 = p1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint _rotl(uint value, int shift)
        {
            // This is expected to be optimized into a single rol (or ror with negated shift value) instruction
            return (value << shift) | (value >> (32 - shift));
        }

        public static readonly ulong DefaultSeed = GenerateSeed();

        public static unsafe ulong GenerateSeed()
        {
            ulong seed;
            byte[] seedBytes = new byte[sizeof(ulong)];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
                fixed (byte* b = seedBytes)
                {
                    seed = *(ulong*)b;
                }
            }

            return seed;
        }
    }
}
