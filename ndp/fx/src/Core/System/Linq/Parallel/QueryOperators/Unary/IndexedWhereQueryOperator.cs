// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IndexedWhereQueryOperator.cs
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
    /// A variant of the Where operator that supplies element index while performing the
    /// filtering operation. This requires cooperation with partitioning and merging to
    /// guarantee ordering is preserved.
    ///
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class IndexedWhereQueryOperator<TInputOutput> : UnaryQueryOperator<TInputOutput, TInputOutput>
    {

        // Predicate function. Used to filter out non-matching elements during execution.
        private Func<TInputOutput, int, bool> m_predicate;
        private bool m_prematureMerge = false; // Whether to prematurely merge the input of this operator.
        private bool m_limitsParallelism = false; // Whether this operator limits parallelism

        //---------------------------------------------------------------------------------------
        // Initializes a new where operator.
        //
        // Arguments:
        //    child         - the child operator or data source from which to pull data
        //    predicate     - a delegate representing the predicate function
        //
        // Assumptions:
        //    predicate must be non null.
        //

        internal IndexedWhereQueryOperator(IEnumerable<TInputOutput> child,
                                           Func<TInputOutput, int, bool> predicate)
            :base(child)
        {
            Contract.Assert(child != null, "child data source cannot be null");
            Contract.Assert(predicate != null, "need a filter function");

            m_predicate = predicate;

            // In an indexed Select, elements must be returned in the order in which
            // indices were assigned.
            m_outputOrdered = true;

            InitOrdinalIndexState();
        }

        private void InitOrdinalIndexState()
        {
            OrdinalIndexState childIndexState = Child.OrdinalIndexState;
            if (ExchangeUtilities.IsWorseThan(childIndexState, OrdinalIndexState.Correct))
            {
                m_prematureMerge = true;
                m_limitsParallelism = childIndexState != OrdinalIndexState.Shuffled;
            }

            SetOrdinalIndexState(OrdinalIndexState.Increasing);
        }


        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TInputOutput> Open(
            QuerySettings settings, bool preferStriping)
        {
            QueryResults<TInputOutput> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInputOutput, TKey> inputStream, IPartitionedStreamRecipient<TInputOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;

            // If the index is not correct, we need to reindex.
            PartitionedStream<TInputOutput, int> inputStreamInt;
            if (m_prematureMerge)
            {
                ListQueryResults<TInputOutput> listResults = ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings);
                inputStreamInt = listResults.GetPartitionedStream();
            }
            else
            {
                Contract.Assert(typeof(TKey) == typeof(int));
                inputStreamInt = (PartitionedStream<TInputOutput, int>)(object)inputStream;
            }

            // Since the index is correct, the type of the index must be int
            PartitionedStream<TInputOutput, int> outputStream =
                new PartitionedStream<TInputOutput, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new IndexedWhereQueryOperatorEnumerator(inputStreamInt[i], m_predicate, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TInputOutput> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.Where(m_predicate);
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return m_limitsParallelism; }
        }

        //-----------------------------------------------------------------------------------
        // An enumerator that implements the filtering logic.
        //

        private class IndexedWhereQueryOperatorEnumerator : QueryOperatorEnumerator<TInputOutput, int>
        {

            private readonly QueryOperatorEnumerator<TInputOutput, int> m_source; // The data source to enumerate.
            private readonly Func<TInputOutput, int, bool> m_predicate; // The predicate used for filtering.
            private CancellationToken m_cancellationToken;
            private Shared<int> m_outputLoopCount;
            //-----------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal IndexedWhereQueryOperatorEnumerator(QueryOperatorEnumerator<TInputOutput, int> source, Func<TInputOutput, int, bool> predicate,
                CancellationToken cancellationToken)
            {
                Contract.Assert(source != null);
                Contract.Assert(predicate != null);
                m_source = source;
                m_predicate = predicate;
                m_cancellationToken = cancellationToken;
            }

            //-----------------------------------------------------------------------------------
            // Moves to the next matching element in the underlying data stream.
            //

            internal override bool MoveNext(ref TInputOutput currentElement, ref int currentKey)
            {
                Contract.Assert(m_predicate != null, "expected a compiled operator");

                // Iterate through the input until we reach the end of the sequence or find
                // an element matching the predicate.
                
                if (m_outputLoopCount == null)
                    m_outputLoopCount = new Shared<int>(0);
                
                while (m_source.MoveNext(ref currentElement, ref currentKey))
                {
                    if ((m_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                        CancellationState.ThrowIfCanceled(m_cancellationToken);

                    if (m_predicate(currentElement, currentKey))
                    {
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
