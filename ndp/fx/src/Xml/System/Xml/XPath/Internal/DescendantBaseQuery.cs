//------------------------------------------------------------------------------
// <copyright file="DescendantQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace MS.Internal.Xml.XPath {
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;

    internal abstract class DescendantBaseQuery : BaseAxisQuery {
        protected bool matchSelf;
        protected bool abbrAxis;

        public DescendantBaseQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type, bool matchSelf, bool abbrAxis) : base(qyParent, Name, Prefix, Type)  {
            this.matchSelf = matchSelf;
            this.abbrAxis  = abbrAxis;
        }
        public DescendantBaseQuery(DescendantBaseQuery other) : base(other) {
            this.matchSelf = other.matchSelf;
            this.abbrAxis  = other.abbrAxis;
        }

        public override XPathNavigator MatchNode(XPathNavigator context) {
            if (context != null) {
                if (!abbrAxis) {
                    throw XPathException.Create(Res.Xp_InvalidPattern);
                }
                XPathNavigator result = null;
                if (matches(context)) {
                    if (matchSelf) {
                        if ((result = qyInput.MatchNode(context)) != null) {
                            return result;
                        }
                    }

                    XPathNavigator anc = context.Clone();
                    while (anc.MoveToParent()) {
                        if ((result = qyInput.MatchNode(anc)) != null) {
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        public override void PrintQuery(XmlWriter w) {
            w.WriteStartElement(this.GetType().Name);
            if (matchSelf) {
                w.WriteAttributeString("self", "yes");
            }
            if (NameTest) {
                w.WriteAttributeString("name", Prefix.Length != 0 ? Prefix + ':' + Name : Name);
            }
            if (TypeTest != XPathNodeType.Element) {
                w.WriteAttributeString("nodeType", TypeTest.ToString());
            }
            qyInput.PrintQuery(w);
            w.WriteEndElement();
        }
    }
}
