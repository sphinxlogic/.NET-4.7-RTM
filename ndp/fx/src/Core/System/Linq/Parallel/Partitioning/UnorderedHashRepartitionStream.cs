// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// UnorderedHashRepartitionStream.cs
//
// <OWNER>Microsoft</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel
{
    internal class UnorderedHashRepartitionStream<TInputOutput, THashKey, TIgnoreKey> : HashRepartitionStream<TInputOutput, THashKey, int>
    {

        //---------------------------------------------------------------------------------------
        // Creates a new partition exchange operator.
        //

        internal UnorderedHashRepartitionStream(
            PartitionedStream<TInputOutput, TIgnoreKey> inputStream,
            Func<TInputOutput, THashKey> keySelector, IEqualityComparer<THashKey> keyComparer, IEqualityComparer<TInputOutput> elementComparer,
            CancellationToken cancellationToken)
            : base(inputStream.PartitionCount, Util.GetDefaultComparer<int>(), keyComparer, elementComparer)
        {
            // Create our array of partitions.
            m_partitions = new HashRepartitionEnumerator<TInputOutput, THashKey, TIgnoreKey>[inputStream.PartitionCount];

            // Initialize state shared among the partitions. A latch and a matrix of buffers. Note that
            // the actual elements in the buffer array are lazily allocated if needed.
            CountdownEvent barrier = new CountdownEvent(inputStream.PartitionCount);
            ListChunk<Pair<TInputOutput, THashKey>>[,] valueExchangeMatrix =
                new ListChunk<Pair<TInputOutput, THashKey>>[inputStream.PartitionCount, inputStream.PartitionCount];

            // Now construct each partition object.
            for (int i = 0; i < inputStream.PartitionCount; i++)
            {
                m_partitions[i] = new HashRepartitionEnumerator<TInputOutput, THashKey, TIgnoreKey>(
                    inputStream[i], inputStream.PartitionCount, i, keySelector, this,
                    barrier, valueExchangeMatrix, cancellationToken);
            }
        }
    }
}
