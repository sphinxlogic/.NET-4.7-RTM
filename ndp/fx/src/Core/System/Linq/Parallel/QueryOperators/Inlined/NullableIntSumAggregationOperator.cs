// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// NullableIntSumAggregationOperator.cs
//
// <OWNER>Microsoft</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// An inlined sum aggregation and its enumerator, for Nullable ints. 
    /// </summary>
    internal sealed class NullableIntSumAggregationOperator : InlinedAggregationOperator<int?, int?, int?>
    {

        //---------------------------------------------------------------------------------------
        // Constructs a new instance of a sum associative operator.
        //

        internal NullableIntSumAggregationOperator(IEnumerable<int?> child) : base(child)
        {
        }

        //---------------------------------------------------------------------------------------
        // Executes the entire query tree, and aggregates the intermediate results into the
        // final result based on the binary operators and final reduction.
        //
        // Return Value:
        //     The single result of aggregation.
        //

        protected override int? InternalAggregate(ref Exception singularExceptionToThrow)
        {
            // Because the final reduction is typically much cheaper than the intermediate 
            // reductions over the individual partitions, and because each parallel partition
            // will do a lot of work to produce a single output element, we prefer to turn off
            // pipelining, and process the final reductions serially.
            using (IEnumerator<int?> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
            {
                // We just reduce the elements in each output partition.
                int sum = 0;
                while (enumerator.MoveNext())
                {
                    checked
                    {
                        sum += enumerator.Current.GetValueOrDefault();
                    }
                }

                return sum;
            }
        }

        //---------------------------------------------------------------------------------------
        // Creates an enumerator that is used internally for the final aggregation step.
        //

        protected override QueryOperatorEnumerator<int?,int> CreateEnumerator<TKey>(
            int index, int count, QueryOperatorEnumerator<int?, TKey> source, object sharedData, CancellationToken cancellationToken)
        {
            return new NullableIntSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
        }

        //---------------------------------------------------------------------------------------
        // This enumerator type encapsulates the intermediary aggregation over the underlying
        // (possibly partitioned) data source.
        //

        private class NullableIntSumAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<int?>
        {
            private QueryOperatorEnumerator<int?, TKey> m_source; // The source data.

            //---------------------------------------------------------------------------------------
            // Instantiates a new aggregation operator.
            //

            internal NullableIntSumAggregationOperatorEnumerator(QueryOperatorEnumerator<int?, TKey> source, int partitionIndex,
                CancellationToken cancellationToken) :
                base(partitionIndex, cancellationToken)
            {
                Contract.Assert(source != null);
                m_source = source;
            }

            //---------------------------------------------------------------------------------------
            // Tallies up the sum of the underlying data source, walking the entire thing the first
            // time MoveNext is called on this object.
            //

            protected override bool MoveNextCore(ref int? currentElement)
            {
                int? element = default(int?);
                TKey keyUnused = default(TKey);

                QueryOperatorEnumerator<int?, TKey> source = m_source;
                if (source.MoveNext(ref element, ref keyUnused))
                {
                    // We just scroll through the enumerator and accumulate the sum.
                    int tempSum = 0;
                    int i = 0;
                    do
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(m_cancellationToken);
                        checked
                        {
                            tempSum += element.GetValueOrDefault();
                        }
                    }
                    while (source.MoveNext(ref element, ref keyUnused));

                    // The sum has been calculated. Now just return.
                    currentElement = new int?(tempSum);
                    return true;
                }

                return false;
            }

            //---------------------------------------------------------------------------------------
            // Dispose of resources associated with the underlying enumerator.
            //

            protected override void Dispose(bool disposing)
            {
                Contract.Assert(m_source != null);
                m_source.Dispose();
            }
        }
    }
}
