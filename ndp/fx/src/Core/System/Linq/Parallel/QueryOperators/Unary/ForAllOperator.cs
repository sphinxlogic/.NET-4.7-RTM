// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ForAllQueryOperator.cs
//
// <OWNER>Microsoft</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;
using System.Diagnostics.Contracts;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A forall operator just enables an action to be placed at the "top" of a query tree
    /// instead of yielding an enumerator that some consumer can walk. We execute the
    /// query for effect instead of yielding a data result. 
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    internal sealed class ForAllOperator<TInput> : UnaryQueryOperator<TInput, TInput>
    {

        // The per-element action to be invoked.
        private readonly Action<TInput> m_elementAction;

        //---------------------------------------------------------------------------------------
        // Constructs a new forall operator.
        //

        internal ForAllOperator(IEnumerable<TInput> child, Action<TInput> elementAction)
            :base(child)
        {
            Contract.Assert(child != null, "child data source cannot be null");
            Contract.Assert(elementAction != null, "need a function");

            m_elementAction = elementAction;
        }

        //---------------------------------------------------------------------------------------
        // This invokes the entire query tree, invoking the per-element action for each result.
        //

        internal void RunSynchronously()
        {
            Contract.Assert(m_elementAction != null);
            
            // Get the enumerator w/out using pipelining. By the time this returns, the query
            // has been executed and we are done. We expect the return to be null.
            Shared<bool> dummyTopLevelDisposeFlag = new Shared<bool>(false);

            CancellationTokenSource dummyInternalCancellationTokenSource = new CancellationTokenSource();

            // stuff in appropriate defaults for unspecified options.
            QuerySettings settingsWithDefaults = SpecifiedQuerySettings
                .WithPerExecutionSettings(dummyInternalCancellationTokenSource, dummyTopLevelDisposeFlag)
                .WithDefaults();

            QueryLifecycle.LogicalQueryExecutionBegin(settingsWithDefaults.QueryId);

            IEnumerator<TInput> enumerator = GetOpenedEnumerator(ParallelMergeOptions.FullyBuffered, true, true,
                settingsWithDefaults);
            settingsWithDefaults.CleanStateAtQueryEnd();
            Contract.Assert(enumerator == null);

            QueryLifecycle.LogicalQueryExecutionEnd(settingsWithDefaults.QueryId);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TInput> Open(
            QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TInput> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void  WrapPartitionedStream<TKey>(
            PartitionedStream<TInput,TKey> inputStream, IPartitionedStreamRecipient<TInput> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            PartitionedStream<TInput, int> outputStream = new PartitionedStream<TInput, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new ForAllEnumerator<TKey>(
                    inputStream[i], m_elementAction, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInput> AsSequentialQuery(CancellationToken token)
        {
            Contract.Assert(false, "AsSequentialQuery is not supported on ForAllOperator");
            throw new InvalidOperationException();
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
        // The executable form of a forall operator. When it is enumerated, the entire underlying
        // partition is walked, invoking the per-element action for each item.
        //

        private class ForAllEnumerator<TKey> : QueryOperatorEnumerator<TInput, int>
        {
            private readonly QueryOperatorEnumerator<TInput, TKey> m_source; // The data source.
            private readonly Action<TInput> m_elementAction; // Forall operator being executed.
            private CancellationToken m_cancellationToken; // Token used to cancel this operator.

            //---------------------------------------------------------------------------------------
            // Constructs a new forall enumerator object.
            //

            internal ForAllEnumerator(QueryOperatorEnumerator<TInput, TKey> source, Action<TInput> elementAction, CancellationToken cancellationToken)
            {
                Contract.Assert(source != null);
                Contract.Assert(elementAction != null);

                m_source = source;
                m_elementAction = elementAction;
                m_cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Just walks the entire data source upon its first invocation, performing the per-
            // element action for each element.
            //

            internal override bool MoveNext(ref TInput currentElement, ref int currentKey)
            {
                Contract.Assert(m_elementAction != null, "expected a compiled operator");

                // We just scroll through the enumerator and execute the action. Because we execute
                // "in place", we actually never even produce a single value.
                
                // Cancellation testing must be performed here as full enumeration occurs within this method.
                // We only need to throw a simple exception here.. marshalling logic handled via QueryTaskGroupState.QueryEnd (called by ForAllSpoolingTask)
                TInput element = default(TInput);
                TKey keyUnused = default(TKey);
                int i = 0;
                while (m_source.MoveNext(ref element, ref keyUnused))
                {
                    if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    m_elementAction(element);
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
