// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// Internal helper functions for working with enumerables.
    /// </summary>
    internal static partial class EnumerableHelpers
    {
        /// <summary>
        /// Tries to get the count of the enumerable cheaply.
        /// </summary>
        /// <typeparam name="T">The element type of the source enumerable.</typeparam>
        /// <param name="source">The enumerable to count.</param>
        /// <param name="count">The count of the enumerable, if it could be obtained cheaply.</param>
        /// <returns><c>true</c> if the enumerable could be counted cheaply; otherwise, <c>false</c>.</returns>
        internal static bool TryGetCount<T>(IEnumerable<T> source, out int count)
        {
            Debug.Assert(source != null);

            var collection = source as ICollection<T>;
            if (collection != null)
            {
                count = collection.Count;
                return true;
            }

            var provider = source as IIListProvider<T>;
            if (provider != null)
            {
                count = provider.GetCount(onlyIfCheap: true);
                return count >= 0;
            }

            count = -1;
            return false;
        }
        
        /// <summary>
        /// Copies items from an enumerable to an array.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in the array to start copying to.</param>
        /// <param name="count">The number of items in the enumerable.</param>
        internal static void Copy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
        {
            Debug.Assert(source != null);
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(array?.Length - arrayIndex >= count);

            var collection = source as ICollection<T>;
            if (collection != null)
            {
                Debug.Assert(collection.Count == count);
                collection.CopyTo(array, arrayIndex);
                return;
            }

            IterativeCopy(source, array, arrayIndex, count);
        }

        /// <summary>
        /// Copies items from a non-collection enumerable to an array.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in the array to start copying to.</param>
        /// <param name="count">The number of items in the enumerable.</param>
        internal static void IterativeCopy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
        {
            Debug.Assert(source != null && !(source is ICollection<T>));
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(array?.Length - arrayIndex >= count);

            int endIndex = arrayIndex + count;
            foreach (T item in source)
            {
                array[arrayIndex++] = item;
            }

            Debug.Assert(arrayIndex == endIndex);
        }
    }
}
