// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// GroupJoinQueryOperator.cs
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
    /// A group join operator takes a left query tree and a right query tree, and then yields
    /// the matching elements between the two. This can be used for outer joins, i.e. those
    /// where an outer element has no matching inner elements -- the result is just an empty
    /// list. As with the join algorithm above, we currently use a hash join algorithm.
    /// </summary>
    /// <typeparam name="TLeftInput"></typeparam>
    /// <typeparam name="TRightInput"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class GroupJoinQueryOperator<TLeftInput, TRightInput, TKey, TOutput> :  BinaryQueryOperator<TLeftInput, TRightInput, TOutput>
    {

        private readonly Func<TLeftInput, TKey> m_leftKeySelector; // The key selection routine for the outer (left) data source.
        private readonly Func<TRightInput, TKey> m_rightKeySelector; // The key selection routine for the inner (right) data source.
        private readonly Func<TLeftInput, IEnumerable<TRightInput>, TOutput> m_resultSelector; // The result selection routine.
        private readonly IEqualityComparer<TKey> m_keyComparer; // An optional key comparison object.

        //---------------------------------------------------------------------------------------
        // Constructs a new join operator.
        //

        internal GroupJoinQueryOperator(ParallelQuery<TLeftInput> left, ParallelQuery<TRightInput> right,
                                        Func<TLeftInput, TKey> leftKeySelector,
                                        Func<TRightInput, TKey> rightKeySelector,
                                        Func<TLeftInput, IEnumerable<TRightInput>, TOutput> resultSelector,
                                        IEqualityComparer<TKey> keyComparer)
            :base(left, right)
        {
            Contract.Assert(left != null && right != null, "child data sources cannot be null");
            Contract.Assert(leftKeySelector != null, "left key selector must not be null");
            Contract.Assert(rightKeySelector != null, "right key selector must not be null");
            Contract.Assert(resultSelector != null, "need a result selector function");

            m_leftKeySelector = leftKeySelector;
            m_rightKeySelector = rightKeySelector;
            m_resultSelector = resultSelector;
            m_keyComparer = keyComparer;
            m_outputOrdered = LeftChild.OutputOrdered;

            SetOrdinalIndex(OrdinalIndexState.Shuffled);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TLeftInput> leftResults = LeftChild.Open(settings, false);
            QueryResults<TRightInput> rightResults = RightChild.Open(settings, false);

            return new BinaryQueryOperatorResults(leftResults, rightResults, this, settings, false);
        }

        public override void WrapPartitionedStream<TLeftKey, TRightKey>(
            PartitionedStream<TLeftInput, TLeftKey> leftStream, PartitionedStream<TRightInput, TRightKey> rightStream,
            IPartitionedStreamRecipient<TOutput> outputRecipient, bool preferStriping, QuerySettings settings)
        {
            Contract.Assert(rightStream.PartitionCount == leftStream.PartitionCount);
            int partitionCount = leftStream.PartitionCount;

            if (LeftChild.OutputOrdered)
            {
                WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
                    ExchangeUtilities.HashRepartitionOrdered(leftStream, m_leftKeySelector, m_keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
            }
            else
            {
                WrapPartitionedStreamHelper<int, TRightKey>(
                    ExchangeUtilities.HashRepartition(leftStream, m_leftKeySelector, m_keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TLeftKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, PartitionedStream<TRightInput, TRightKey> rightPartitionedStream, 
            IPartitionedStreamRecipient<TOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
        {
            PartitionedStream<Pair<TRightInput, TKey>, int> rightHashStream = ExchangeUtilities.HashRepartition(
                rightPartitionedStream, m_rightKeySelector, m_keyComparer, null, cancellationToken);

            PartitionedStream<TOutput, TLeftKey> outputStream = new PartitionedStream<TOutput, TLeftKey>(
                partitionCount, leftHashStream.KeyComparer, OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, TKey, TOutput>(
                    leftHashStream[i], rightHashStream[i], null, m_resultSelector, m_keyComparer, cancellationToken);
            }

            outputRecipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TLeftInput> wrappedLeftChild = CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token);
            IEnumerable<TRightInput> wrappedRightChild = CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token);

            return wrappedLeftChild
                .GroupJoin(
                wrappedRightChild, m_leftKeySelector, m_rightKeySelector, m_resultSelector, m_keyComparer);
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }
    }
}
