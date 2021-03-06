// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// EmptyEnumerable.cs
//
// <OWNER>Microsoft</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel
{
    /// <summary>
    /// We occ----ionally need a no-op enumerator to stand-in when we don't have data left
    /// within a partition's data stream. These are simple enumerable and enumerator
    /// implementations that always and consistently yield no elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class EmptyEnumerable<T> : ParallelQuery<T>
    {
        private EmptyEnumerable()
            : base(QuerySettings.Empty)
        {
        }

        // A singleton cached and shared among callers.
        private static volatile EmptyEnumerable<T> s_instance;
        private static volatile EmptyEnumerator<T> s_enumeratorInstance;

        internal static EmptyEnumerable<T> Instance
        {
            get
            {
                if (s_instance == null)
                {
                    // There is no need for thread safety here.
                    s_instance = new EmptyEnumerable<T>();
                }

                return s_instance;
            }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            if (s_enumeratorInstance == null)
            {
                // There is no need for thread safety here.
                s_enumeratorInstance = new EmptyEnumerator<T>();
            }

            return s_enumeratorInstance;
        }
    }

    internal class EmptyEnumerator<T> : QueryOperatorEnumerator<T, int>, IEnumerator<T>
    {
        internal override bool MoveNext(ref T currentElement, ref int currentKey)
        {
            return false;
        }

        // IEnumerator<T> methods.
        public T Current { get { return default(T); } }
        object IEnumerator.Current { get { return null; } }
        public bool MoveNext() { return false; }
        void Collections.IEnumerator.Reset() { }
    }
}
