// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SelectQueryOperator.cs
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
    /// The operator type for Select statements. This operator transforms elements as it
    /// enumerates them through the use of a selector delegate. 
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class SelectQueryOperator<TInput, TOutput> : UnaryQueryOperator<TInput, TOutput>
    {

        // Selector function. Used to project elements to a transformed view during execution.
        private Func<TInput, TOutput> m_selector;

        //---------------------------------------------------------------------------------------
        // Initializes a new select operator.
        //
        // Arguments:
        //    child         - the child operator or data source from which to pull data
        //    selector      - a delegate representing the selector function
        //
        // Assumptions:
        //    selector must be non null.
        //

        internal SelectQueryOperator(IEnumerable<TInput> child, Func<TInput, TOutput> selector)
            :base(child)
        {
            Contract.Assert(child != null, "child data source cannot be null");
            Contract.Assert(selector != null, "need a selector function");

            m_selector = selector;
            SetOrdinalIndexState(Child.OrdinalIndexState);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            PartitionedStream<TOutput, TKey> outputStream =
                new PartitionedStream<TOutput, TKey>(inputStream.PartitionCount, inputStream.KeyComparer, OrdinalIndexState);
            for (int i = 0; i < inputStream.PartitionCount; i++)
            {
                outputStream[i] = new SelectQueryOperatorEnumerator<TKey>(inputStream[i], m_selector);
            }

            recipient.Receive(outputStream);
        }


        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TInput> childQueryResults = Child.Open(settings, preferStriping);
            return SelectQueryOperatorResults.NewResults(childQueryResults, this, settings, preferStriping);
        }

        internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
        {
            return Child.AsSequentialQuery(token).Select(m_selector);
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------
        // The enumerator type responsible for projecting elements as it is walked.
        //

        class SelectQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TOutput, TKey>
        {

            private readonly QueryOperatorEnumerator<TInput, TKey> m_source; // The data source to enumerate.
            private readonly Func<TInput, TOutput> m_selector;  // The actual select function.

            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal SelectQueryOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source, Func<TInput, TOutput> selector)
            {
                Contract.Assert(source != null);
                Contract.Assert(selector != null);
                m_source = source;
                m_selector = selector;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext(ref TOutput currentElement, ref TKey currentKey)
            {
                // So long as the source has a next element, we have an element.
                TInput element = default(TInput);
                if (m_source.MoveNext(ref element, ref currentKey))
                {
                    Contract.Assert(m_selector != null, "expected a compiled operator");
                    currentElement = m_selector(element);
                    return true;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                m_source.Dispose();
            }
        }

        //-----------------------------------------------------------------------------------
        // Query results for a Select operator. The results are indexible if the child
        // results were indexible.
        //

        class SelectQueryOperatorResults : UnaryQueryOperatorResults
        {
            private Func<TInput, TOutput> m_selector; // Selector function
            private int m_childCount; // The number of elements in child results

            public static QueryResults<TOutput> NewResults(
                QueryResults<TInput> childQueryResults, SelectQueryOperator<TInput, TOutput> op, 
                QuerySettings settings, bool preferStriping)
            {
                if (childQueryResults.IsIndexible)
                {
                    return new SelectQueryOperatorResults(childQueryResults, op, settings, preferStriping);
                }
                else
                {
                    return new UnaryQueryOperatorResults(childQueryResults, op, settings, preferStriping);
                }
            }

            private SelectQueryOperatorResults(
                QueryResults<TInput> childQueryResults, SelectQueryOperator<TInput, TOutput> op,
                QuerySettings settings, bool preferStriping)
                : base(childQueryResults, op, settings, preferStriping)
            {
                Contract.Assert(op.m_selector != null);
                m_selector = op.m_selector;
                Contract.Assert(m_childQueryResults.IsIndexible);
                m_childCount = m_childQueryResults.ElementsCount;
            }

            internal override bool IsIndexible
            {
                get { return true; }
            }

            internal override int ElementsCount
            {
                get { return m_childCount; }
            }

            internal override TOutput GetElement(int index)
            {
                Contract.Assert(index >= 0);
                Contract.Assert(index < ElementsCount);

                return m_selector(m_childQueryResults.GetElement(index));
            }
        }
    }
}
