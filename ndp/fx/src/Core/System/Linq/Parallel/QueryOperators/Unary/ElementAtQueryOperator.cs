// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ElementAtQueryOperator.cs
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
    /// ElementAt just retrieves an element at a specific index.  There is some cross-partition
    /// coordination to force partitions to stop looking once a partition has found the
    /// sought-after element.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class ElementAtQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {

        private readonly int m_index; // The index that we're looking for.
        private readonly bool m_prematureMerge = false; // Whether to prematurely merge the input of this operator.
        private readonly bool m_limitsParallelism = false; // Whether this operator limits parallelism

        //---------------------------------------------------------------------------------------
        // Constructs a new instance of the contains search operator.
        //
        // Arguments:
        //     child       - the child tree to enumerate.
        //     index       - index we are searching for.
        //

        internal ElementAtQueryOperator(IEnumerable<TSource> child, int index)
            :base(child)
        {
            Contract.Assert(child != null, "child data source cannot be null");
            Contract.Assert(index >= 0, "index can't be less than 0");
            m_index = index;

            OrdinalIndexState childIndexState = Child.OrdinalIndexState;
            if (ExchangeUtilities.IsWorseThan(childIndexState, OrdinalIndexState.Correct))
            {
                m_prematureMerge = true;
                m_limitsParallelism = childIndexState != OrdinalIndexState.Shuffled;
            }
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(
            QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TSource> childQueryResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void  WrapPartitionedStream<TKey>(
            PartitionedStream<TSource,TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            // If the child OOP index is not correct, reindex.
            int partitionCount = inputStream.PartitionCount;

            PartitionedStream<TSource, int> intKeyStream;
            if (m_prematureMerge)
            {
                intKeyStream = ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings).GetPartitionedStream();
                Contract.Assert(intKeyStream.OrdinalIndexState == OrdinalIndexState.Indexible);
            }
            else
            {
                intKeyStream = (PartitionedStream<TSource, int>)(object)inputStream;
            }

            // Create a shared cancelation variable and then return a possibly wrapped new enumerator.
            Shared<bool> resultFoundFlag = new Shared<bool>(false);

            PartitionedStream<TSource, int> outputStream = new PartitionedStream<TSource, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new ElementAtQueryOperatorEnumerator(intKeyStream[i], m_index, resultFoundFlag, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            Contract.Assert(false, "This method should never be called as fallback to sequential is handled in Aggregate().");
            throw new NotSupportedException();
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return m_limitsParallelism; }
        }

        
        /// <summary>
        /// Executes the query, either sequentially or in parallel, depending on the query execution mode and
        /// whether a premature merge was inserted by this ElementAt operator.
        /// </summary>
        /// <param name="result">result</param>
        /// <param name="withDefaultValue">withDefaultValue</param>
        /// <returns>whether an element with this index exists</returns>
        internal bool Aggregate(out TSource result, bool withDefaultValue)
        {
            // If we were to insert a premature merge before this ElementAt, and we are executing in conservative mode, run the whole query
            // sequentially.
            if (LimitsParallelism && SpecifiedQuerySettings.WithDefaults().ExecutionMode.Value != ParallelExecutionMode.ForceParallelism)
            {
                CancellationState cancelState = SpecifiedQuerySettings.CancellationState;
                if (withDefaultValue)
                {
                    IEnumerable<TSource> childAsSequential = Child.AsSequentialQuery(cancelState.ExternalCancellationToken);
                    IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, cancelState.ExternalCancellationToken);
                    result = ExceptionAggregator.WrapEnumerable(childWithCancelChecks, cancelState).ElementAtOrDefault(m_index);
                }
                else
                {
                    IEnumerable<TSource> childAsSequential = Child.AsSequentialQuery(cancelState.ExternalCancellationToken);
                    IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, cancelState.ExternalCancellationToken);
                    result = ExceptionAggregator.WrapEnumerable(childWithCancelChecks, cancelState).ElementAt(m_index);
                }
                return true;
            }

            using (IEnumerator<TSource> e = GetEnumerator(ParallelMergeOptions.FullyBuffered))
            {
                if (e.MoveNext())
                {
                    TSource current = e.Current;
                    Contract.Assert(!e.MoveNext(), "expected enumerator to be empty");
                    result = current;
                    return true;
                }
            }

            result = default(TSource);
            return false;
        }


        //---------------------------------------------------------------------------------------
        // This enumerator performs the search for the element at the specified index.
        //

        class ElementAtQueryOperatorEnumerator : QueryOperatorEnumerator<TSource, int>
        {
            private QueryOperatorEnumerator<TSource, int> m_source; // The source data.
            private int m_index; // The index of the element to seek.
            private Shared<bool> m_resultFoundFlag; // Whether to cancel the operation.
            private CancellationToken m_cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new any/all search operator.
            //

            internal ElementAtQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, int> source,
                                                      int index, Shared<bool> resultFoundFlag,
                CancellationToken cancellationToken)
            {
                Contract.Assert(source != null);
                Contract.Assert(index >= 0);
                Contract.Assert(resultFoundFlag != null);

                m_source = source;
                m_index = index;
                m_resultFoundFlag = resultFoundFlag;
                m_cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Enumerates the entire input until the element with the specified is found or another
            // partition has signaled that it found the element.
            //

            internal override bool MoveNext(ref TSource currentElement, ref int currentKey)
            {
                // Just walk the enumerator until we've found the element.
                int i = 0;
                while (m_source.MoveNext(ref currentElement, ref currentKey))
                {
                    if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                        CancellationState.ThrowIfCanceled(m_cancellationToken);

                    if (m_resultFoundFlag.Value)
                    {
                        // Another partition found the element.
                        break;
                    }

                    if (currentKey == m_index)
                    {
                        // We have found the element. Cancel other searches and return true.
                        m_resultFoundFlag.Value = true;
                        return true;
                    }
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                Contract.Assert(m_source != null);
                m_source.Dispose();
            }
        }
    }
}
