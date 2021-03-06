//------------------------------------------------------------------------------
// <copyright file="Query.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace MS.Internal.Xml.XPath {
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Xml.Xsl;
    using System.Diagnostics;
    using System.Collections.Generic;

    // See comments to QueryBuilder.Props
    // Not all of them are used currently
    internal enum QueryProps {
        None     = 0x00,
        Position = 0x01,
        Count    = 0x02,
        Cached   = 0x04,
        Reverse  = 0x08,
        Merge    = 0x10,
    };


    // Turn off DebuggerDisplayAttribute. in subclasses of Query.
    // Calls to Current in the XPathNavigator.DebuggerDisplayProxy may change state or throw
    [DebuggerDisplay("{ToString()}")] 
    internal abstract class Query : ResetableIterator {
        public Query() { }
        protected Query(Query other) : base(other) { }

        // -- XPathNodeIterator --
        //public abstract XPathNodeIterator Clone();
        //public abstract XPathNavigator Current { get; }
        //public abstract int CurrentPosition { get; }
        public override bool MoveNext() { return Advance() != null; }
        public override int Count {
            get { 
                // Query can be ordered in reverse order. So we can't assume like base.Count that last node has greatest position.
                if (count == -1) {
                    Query clone = (Query)this.Clone();
                    clone.Reset();
                    count = 0;
                    while (clone.MoveNext()) count ++;
                }
                return count;
            } 
        }

        // ------------- ResetableIterator -----------
        // It's importent that Query is resetable. This fact is used in several plases: 
        // 1. In LogicalExpr: "foo = bar"
        // 2. In SelectionOperator.Reset().
        // In all this cases Reset() means restart iterate through the same nodes. 
        // So reset shouldn't clean bufferes in query. This should be done in set context.
        //public abstract void Reset();

        // -------------------- Query ------------------
        public virtual void SetXsltContext(XsltContext context) { }

        public abstract object Evaluate(XPathNodeIterator nodeIterator);
        public abstract XPathNavigator Advance();

        public virtual  XPathNavigator MatchNode(XPathNavigator current) {
            throw XPathException.Create(Res.Xp_InvalidPattern);
        }

        public virtual double XsltDefaultPriority { get { return 0.5; } }
        public abstract XPathResultType StaticType { get; }
        public virtual QueryProps Properties { get { return QueryProps.Merge; } }

        // ----------------- Helper methods -------------
        public static Query Clone(Query input) {
            if (input != null) {
                return (Query)input.Clone();
            }
            return null;
        }

        protected static XPathNodeIterator Clone(XPathNodeIterator input) {
            if (input != null) {
                return input.Clone();
            }
            return null;
        }

        protected static XPathNavigator Clone(XPathNavigator input) {
            if (input != null) {
                return input.Clone();
            }
            return null;
        }

        // -----------------------------------------------------
        // Set of methods to support insertion to sorted buffer.
        // buffer is always sorted here

        public bool Insert(List<XPathNavigator> buffer, XPathNavigator nav) {
            int l = 0;
            int r = buffer.Count;

            // In most cases nodes are already sorted. 
            // This means that nav often will be equal or after then last item in the buffer
            // So let's check this first.
            if (r != 0) {
                switch (CompareNodes(buffer[r - 1], nav)) {
                case XmlNodeOrder.Same:
                    return false;
                case XmlNodeOrder.Before:
                    buffer.Add(nav.Clone());
                    return true;
                default:
                    r --;
                    break;
                }
            }

            while (l < r) {
                int m = GetMedian(l, r);
                switch (CompareNodes(buffer[m], nav)) {
                case XmlNodeOrder.Same:
                    return false;
                case XmlNodeOrder.Before: 
                    l = m + 1;
                    break;
                default:
                    r = m;
                    break;
                }
            }
            AssertDOD(buffer, nav, l);
            buffer.Insert(l, nav.Clone());
            return true;
        }

        private static int GetMedian(int l, int r) {
            Debug.Assert(0 <= l && l < r);
            return (int) (((uint) l + (uint) r) >> 1);
        }

        public static XmlNodeOrder CompareNodes(XPathNavigator l, XPathNavigator r) {
            XmlNodeOrder cmp = l.ComparePosition(r);
            if (cmp == XmlNodeOrder.Unknown) {
                XPathNavigator copy = l.Clone();
                copy.MoveToRoot();
                string baseUriL = copy.BaseURI;
                if (! copy.MoveTo(r)) {
                    copy = r.Clone();
                }
                copy.MoveToRoot();
                string baseUriR = copy.BaseURI;
                int cmpBase = string.CompareOrdinal(baseUriL, baseUriR);
                cmp = (
                    cmpBase < 0 ? XmlNodeOrder.Before :
                    cmpBase > 0 ? XmlNodeOrder.After  :
                    /*default*/   XmlNodeOrder.Unknown
                );
            }
            return cmp;
        }

        [Conditional("DEBUG")]
        private void AssertDOD(List<XPathNavigator> buffer, XPathNavigator nav, int pos) {
            if (nav.GetType().ToString() == "Microsoft.VisualStudio.Modeling.StoreNavigator") return;
            if (nav.GetType().ToString() == "System.Xml.DataDocumentXPathNavigator") return;
            Debug.Assert(0 <= pos && pos <= buffer.Count, "Algorithm error: Insert()");
            XmlNodeOrder cmp;
            if (0 < pos) {
                cmp = CompareNodes(buffer[pos - 1], nav);
                Debug.Assert(cmp == XmlNodeOrder.Before, "Algorithm error: Insert()");
            }
            if (pos < buffer.Count) {
                cmp = CompareNodes(nav, buffer[pos]);
                Debug.Assert(cmp == XmlNodeOrder.Before, "Algorithm error: Insert()");
            }
        }

        [Conditional("DEBUG")]
        public static void AssertQuery(Query query) {
            Debug.Assert(query != null, "AssertQuery(): query == null");
            if (query is FunctionQuery) return; // Temp Fix. Functions (as document()) return now unordered sequences
            query = Clone(query);
            XPathNavigator last = null;
            XPathNavigator curr;
            int querySize = query.Clone().Count;
            int actualSize = 0;
            while ((curr = query.Advance()) != null) {
                if (curr.GetType().ToString() == "Microsoft.VisualStudio.Modeling.StoreNavigator") return;
                if (curr.GetType().ToString() == "System.Xml.DataDocumentXPathNavigator") return;
                Debug.Assert(curr == query.Current, "AssertQuery(): query.Advance() != query.Current");
                if (last != null) {
                    if (last.NodeType == XPathNodeType.Namespace && curr.NodeType == XPathNodeType.Namespace) {
                        // NamespaceQuery reports namsespaces in mixed order.
                        // Ignore this for now. 
                        // It seams that this doesn't breake other queries becasue NS can't have children
                    } else {
                        XmlNodeOrder cmp = CompareNodes(last, curr);
                        Debug.Assert(cmp == XmlNodeOrder.Before, "AssertQuery(): Wrong node order");
                    }
                }
                last = curr.Clone();
                actualSize++;
            }
            Debug.Assert(actualSize == querySize, "AssertQuery(): actualSize != querySize");
        }

        // =================== XPathResultType_Navigator ======================
        // In v.1.0 and v.1.1 XPathResultType.Navigator is defined == to XPathResultType.String
        // This is source for multiple bugs or additional type casts.
        // To fix all of them in one change in v.2 we internaly use one more value:
        public const XPathResultType XPathResultType_Navigator = (XPathResultType) 4;
        // The biggest challenge in this change is preserve backward compatibility with v.1.1
        // To achive this in all places where we accept from or report to user XPathResultType.
        // On my best knowledge this happens only in XsltContext.ResolveFunction() / IXsltContextFunction.ReturnType


        protected XPathResultType GetXPathType(object value) {
            if (value is XPathNodeIterator) return XPathResultType.NodeSet;
            if (value is string           ) return XPathResultType.String;
            if (value is double           ) return XPathResultType.Number;
            if (value is bool             ) return XPathResultType.Boolean;
            Debug.Assert(value is XPathNavigator, "Unknown value type");
            return XPathResultType_Navigator;
        }

        // =================== Serialization ======================
        public virtual void PrintQuery(XmlWriter w) {
            w.WriteElementString(this.GetType().Name, string.Empty);
        }
    }
}
