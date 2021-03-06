//------------------------------------------------------------------------------
// <copyright file="ResetableIterator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace MS.Internal.Xml.XPath {
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;

    internal abstract class ResetableIterator : XPathNodeIterator {
        // the best place for this constructors to be is XPathNodeIterator, to avoid DCR at this time let's ground them here
        public ResetableIterator() {
            base.count = -1;
        }
        protected ResetableIterator(ResetableIterator other) {
            base.count = other.count;
        }
        protected void ResetCount() { 
            base.count = -1; 
        }

        public abstract void Reset();
        public virtual bool MoveToPosition(int pos) {
            Reset();
            for(int i = CurrentPosition; i < pos ; i ++) {
                if(!MoveNext()) {
                    return false;
                }
            }
            return true;
        }

        // Contruct extension: CurrentPosition should return 0 if MoveNext() wasn't called after Reset()
        // (behavior is not defined for XPathNodeIterator)
        public abstract override int CurrentPosition { get; }
    }
}
