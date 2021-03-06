//------------------------------------------------------------------------------
// <copyright file="FilterQuery.cs" company="Microsoft">
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
    using System.Xml.Xsl;

    internal sealed class FilterQuery : BaseAxisQuery {
        private Query cond;
        private bool noPosition;
        
        public FilterQuery(Query qyParent, Query cond, bool noPosition) : base(qyParent) {
            this.cond = cond;
            this.noPosition = noPosition;
        }
        private FilterQuery(FilterQuery other) : base(other) {
            this.cond       = Clone(other.cond);
            this.noPosition = other.noPosition;
        }        

        public override void Reset() {
            cond.Reset();
            base.Reset();
        }

        public Query Condition { get { return cond; } }

        public override void SetXsltContext(XsltContext input) {
            base.SetXsltContext(input);
            cond.SetXsltContext(input);
            if (cond.StaticType != XPathResultType.Number && cond.StaticType != XPathResultType.Any && noPosition) {
                // BugBug: We can do such trick at Evaluate time only.
                // But to do this FilterQuery should stop inherit from BaseAxisQuery
                ReversePositionQuery query = qyInput as ReversePositionQuery;
                if (query != null) {
                    qyInput = query.input;
                }
            }
        }

        public override XPathNavigator Advance() {
            while ((currentNode = qyInput.Advance()) != null) {
                if (EvaluatePredicate()) {
                    position++;
                    return currentNode;
                }
            }
            return null;
        }

        internal bool EvaluatePredicate() {
            object value = cond.Evaluate(qyInput);
            if (value is XPathNodeIterator) return cond.Advance() != null;
            if (value is string           ) return ((string)value).Length != 0;
            if (value is double           ) return (((double)value) == qyInput.CurrentPosition);
            if (value is bool             ) return (bool)value;
            Debug.Assert(value is XPathNavigator, "Unknown value type");
            return true;
        }

        public override XPathNavigator MatchNode(XPathNavigator current) {
            XPathNavigator context;
            if (current == null) {
                return null;
            }
            context = qyInput.MatchNode(current);

            if (context != null) {
                // In this switch we process some special case in wich we can calculate predicate faster then in generic case
                switch (cond.StaticType) {
                case XPathResultType.Number:
                    OperandQuery operand = cond as OperandQuery;
                    if (operand != null) {
                        double val = (double)operand.val;
                        ChildrenQuery childrenQuery = qyInput as ChildrenQuery;
                        if (childrenQuery != null) { // foo[2], but not foo[expr][2]
                            XPathNavigator result = current.Clone();
                            result.MoveToParent();
                            int i = 0;
                            result.MoveToFirstChild();
                            do {
                                if (childrenQuery.matches(result)) {
                                    i++;
                                    if (current.IsSamePosition(result)) {
                                        return val == i ? context : null;
                                    }
                                }
                            } while (result.MoveToNext());
                            return null;
                        }
                        AttributeQuery attributeQuery = qyInput as AttributeQuery;
                        if (attributeQuery != null) {// @foo[3], but not @foo[expr][2]
                            XPathNavigator result = current.Clone();
                            result.MoveToParent();
                            int i = 0;
                            result.MoveToFirstAttribute();
                            do {
                                if (attributeQuery.matches(result)) {
                                    i++;
                                    if (current.IsSamePosition(result)) {
                                        return val == i ? context : null;
                                    }
                                }
                            } while (result.MoveToNextAttribute());
                            return null;
                        }
                    }
                    break;
                case XPathResultType.NodeSet:
                    cond.Evaluate(new XPathSingletonIterator(current, /*moved:*/true));
                    return (cond.Advance() != null) ? context : null;
                case XPathResultType.Boolean:
                    if (noPosition) {
                        return ((bool)cond.Evaluate(new XPathSingletonIterator(current, /*moved:*/true))) ? context : null;
                    }
                    break;
                case XPathResultType.String:
                    if (noPosition) {
                        return (((string)cond.Evaluate(new XPathSingletonIterator(current, /*moved:*/true))).Length != 0) ? context : null;
                    }
                    break;
                case XPathResultType_Navigator:
                    return context;
                default:
                    return null;
                }
                /* Generic case */ {
                    Evaluate(new XPathSingletonIterator(context, /*moved:*/true));
                    XPathNavigator result;
                    while ((result = Advance()) != null) {
                        if (result.IsSamePosition(current)) {
                            return context;
                        }
                    }
                }
            }
            return null;
        }

        public override QueryProps Properties { 
            get { 
                return QueryProps.Position | (qyInput.Properties & (QueryProps.Merge | QueryProps.Reverse)); 
            } 
        }

        public override XPathNodeIterator Clone() { return new FilterQuery(this); }

        public override void PrintQuery(XmlWriter w) {
            w.WriteStartElement(this.GetType().Name);
            if (! noPosition
                ) {
                w.WriteAttributeString("position", "yes");
            }
            qyInput.PrintQuery(w);
            cond.PrintQuery(w);
            w.WriteEndElement();
        }
    }
}
