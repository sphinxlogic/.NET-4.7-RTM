//------------------------------------------------------------------------------
// <copyright file="DocumentOrderComparer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;

namespace System.Xml.Xsl.Runtime {

    /// <summary>
    /// IComparer implementation that orders navigators based on ComparePosition.  When ComparePosition returns
    /// XmlNodeOrder.Unknown, a stable order between documents is maintained by an ordered list mapping each root node
    /// to an ordering index.
    /// </summary>
    internal class DocumentOrderComparer : IComparer<XPathNavigator> {
        private List<XPathNavigator> roots;

        /// <summary>
        /// Return:
        ///     -1 if navThis is positioned before navThat
        ///      0 if navThis has the same position as navThat
        ///      1 if navThis is positioned after navThat
        /// </summary>
        public int Compare(XPathNavigator navThis, XPathNavigator navThat) {
            switch (navThis.ComparePosition(navThat)) {
                case XmlNodeOrder.Before: return -1;
                case XmlNodeOrder.Same: return 0;
                case XmlNodeOrder.After: return 1;
            }

            // Use this.roots to impose stable ordering
            if (this.roots == null)
                this.roots = new List<XPathNavigator>();

            Debug.Assert(GetDocumentIndex(navThis) != GetDocumentIndex(navThat));
            return GetDocumentIndex(navThis) < GetDocumentIndex(navThat) ? -1 : 1;
        }

        /// <summary>
        /// Map navigator's document to a unique index.
        /// When consecutive calls are made to GetIndexOfNavigator for navThis and navThat, it is not possible
        /// for them to return the same index.  navThis compared to navThat is always XmlNodeOrder.Unknown.
        /// Therefore, no matter where navThis is inserted in the list, navThat will never be inserted just
        /// before navThis, and therefore will never have the same index.
        /// </summary>
        public int GetDocumentIndex(XPathNavigator nav) {
            XPathNavigator navRoot;

            // Use this.roots to impose stable ordering
            if (this.roots == null)
                this.roots = new List<XPathNavigator>();

            // Position navigator to root
            navRoot = nav.Clone();
            navRoot.MoveToRoot();

            for (int idx = 0; idx < this.roots.Count; idx++) {
                if (navRoot.IsSamePosition(this.roots[idx])) {
                    // navigator's document was previously mapped to a unique index
                    return idx;
                }
            }

            // Add navigator to this.roots mapping
            this.roots.Add(navRoot);

            return this.roots.Count - 1;
        }
    }
}
