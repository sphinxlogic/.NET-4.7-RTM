// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Scheduling.cs
//
// <OWNER>Microsoft</OWNER>
//
// Infrastructure for setting up concurrent work, marshaling exceptions, determining
// the recommended degree-of-parallelism, and so forth.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using System.Security;
using System.Security.Permissions;

namespace System.Linq.Parallel
{

    //-----------------------------------------------------------------------------------
    // A simple helper class that offers common task scheduling functionality.
    //

    internal static class Scheduling
    {
        // Whether to preserve order by default, when neither AsOrdered nor AsUnordered is used.
        internal const bool DefaultPreserveOrder = false;

        // The default degree of parallelism, or -1 if unspecified. Dev unit tests set this value
        // to change the default DOP.
        internal static int DefaultDegreeOfParallelism = Math.Min(Environment.ProcessorCount, MAX_SUPPORTED_DOP);

        // The size to use for bounded buffers. 
        internal const int DEFAULT_BOUNDED_BUFFER_CAPACITY = 512;

        // The number of bytes we want "chunks" to be, when partitioning, etc. We choose 4 cache
        // lines worth, assuming 128b cache line.  Most (popular) architectures use 64b cache lines,
        // but choosing 128b works for 64b too whereas a multiple of 64b isn't necessarily sufficient
        // for 128b cache systems.  So 128b it is.
        internal const int DEFAULT_BYTES_PER_CHUNK = 128 * 4; 

        // The number of milliseconds before we assume a producer has been zombied.
        internal const int ZOMBIED_PRODUCER_TIMEOUT = Timeout.Infinite;

        // The largest number of partitions that PLINQ supports.
        internal const int MAX_SUPPORTED_DOP = 512;


        //-----------------------------------------------------------------------------------
        // Calculates the proper amount of DOP.  This takes into consideration dynamic nesting.
        //

        internal static int GetDefaultDegreeOfParallelism()
        {
            return DefaultDegreeOfParallelism;
        }

        //-----------------------------------------------------------------------------------
        // Gets the recommended "chunk size" for a particular CLR type.
        //
        // Notes:
        //     We try to recommend some reasonable "chunk size" for the data, but this is
        //     clearly a tradeoff, and requires a bit of experimentation. A larger chunk size
        //     can help locality, but if it's too big we may end up either stalling another
        //     partition (if enumerators are calculating data on demand) or skewing the
        //     distribution of data among the partitions.
        //

        internal static int GetDefaultChunkSize<T>()
        {
            int chunkSize;

            if (typeof(T).IsValueType)
            {
                // @

#if !SILVERLIGHT //StructLayoutAttribute is not supported in CoreCLR/CoreSys
                if (typeof(T).StructLayoutAttribute.Value == LayoutKind.Explicit)
                {
                    chunkSize = Math.Max(1, DEFAULT_BYTES_PER_CHUNK / Marshal.SizeOf(typeof(T)));
                }
                else
#endif
                {
                    // We choose '128' because this ensures, no matter the actual size of the value type,
                    // the total bytes used will be a multiple of 128. This ensures it's cache aligned.
                    chunkSize = 128;
                }
            }
            else
            {
                Contract.Assert((DEFAULT_BYTES_PER_CHUNK % IntPtr.Size) == 0, "bytes per chunk should be a multiple of pointer size");
                chunkSize = (DEFAULT_BYTES_PER_CHUNK / IntPtr.Size);
            }

            TraceHelpers.TraceInfo("Scheduling::GetDefaultChunkSize({0}) -- returning {1}", typeof(T), chunkSize);

            return chunkSize;
        }

    }

}
