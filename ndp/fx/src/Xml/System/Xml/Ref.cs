//------------------------------------------------------------------------------
// <copyright file="Ref.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

using System.Diagnostics;

namespace System.Xml {

    /// <summary>
    /// Ref class is used to verify string atomization in debug mode.
    /// </summary>
    internal static class Ref {
        public static bool Equal(string strA, string strB) {
        #if DEBUG
            if (((object) strA != (object) strB) && string.Equals(strA, strB))
                Debug.Assert(false, "Ref.Equal: Object comparison used for non-atomized string '" + strA + "'");
        #endif
            return (object) strA == (object) strB;
        }

        // Prevent typos. If someone uses Ref.Equals instead of Ref.Equal,
        // the program would not compile.
        public static new void Equals(object objA, object objB) {
        }
    }
}
