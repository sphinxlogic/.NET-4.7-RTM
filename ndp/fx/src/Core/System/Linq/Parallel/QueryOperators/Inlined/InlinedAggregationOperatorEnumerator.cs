// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// InlinedAggregationOperatorEnumerator.cs
//
// <OWNER>Microsoft</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Threading;

namespace System.Linq.Parallel
{
    //---------------------------------------------------------------------------------------
    // Inlined aggregate operators for finding the min/max values for primitives (int, long, float,
    // double, decimal).  Versions are also offered for the nullable primitives (int?, long?, float?,
    // double?, decimal?), which differ slightly in behavior: they return a null value for empty
    // streams, whereas the ordinary primitive versions throw.
    //

    //---------------------------------------------------------------------------------------
    // Inlined average operators for primitives (int, long, float, double, decimal), and the
    // nullable variants.  The difference between the nromal and nullable variety is that
    // nulls are skipped in tallying the count and sum for the average.
    //

    /// <summary>
    /// A class with some shared implementation between all aggregation enumerators. 
    /// </summary>
    /// <typeparam name="TIntermediate"></typeparam>
    internal abstract class InlinedAggregationOperatorEnumerator<TIntermediate> : QueryOperatorEnumerator<TIntermediate, int>
    {
        private int m_partitionIndex; // This partition's unique index.
        private bool m_done = false;
        protected CancellationToken m_cancellationToken;

        //---------------------------------------------------------------------------------------
        // Instantiates a new aggregation operator.
        //

        internal InlinedAggregationOperatorEnumerator(int partitionIndex, CancellationToken cancellationToken)
        {
            m_partitionIndex = partitionIndex;
            m_cancellationToken = cancellationToken;
        }

        //---------------------------------------------------------------------------------------
        // Tallies up the sum of the underlying data source, walking the entire thing the first
        // time MoveNext is called on this object. There is a boilerplate variant used by callers,
        // and then one that is used for extensibility by subclasses.
        //

        internal sealed override bool MoveNext(ref TIntermediate currentElement, ref int currentKey)
        {
            if (!m_done && MoveNextCore(ref currentElement))
            {
                // A reduction's "index" is the same as its partition number.
                currentKey = m_partitionIndex;
                m_done = true;
                return true;
            }

            return false;
        }

        protected abstract bool MoveNextCore(ref TIntermediate currentElement);

    }
}
