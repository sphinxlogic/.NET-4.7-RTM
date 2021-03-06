//------------------------------------------------------------------------------
// <copyright file="XPathAncestorQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace MS.Internal.Xml.XPath {
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Collections.Generic;

    internal sealed class XPathAncestorQuery : CacheAxisQuery {
        private bool matchSelf;

        public XPathAncestorQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest, bool matchSelf) : base(qyInput, name, prefix, typeTest)  {
            this.matchSelf = matchSelf;
        }
        private XPathAncestorQuery(XPathAncestorQuery other) : base(other) {
            this.matchSelf = other.matchSelf;
        }

        public override object Evaluate(XPathNodeIterator context) {
            base.Evaluate(context);

            XPathNavigator ancestor = null;            
            XPathNavigator input;
            while ((input = qyInput.Advance()) != null) {
                if (matchSelf) {
                    if (matches(input)) {
                        if (!Insert(outputBuffer, input)) {
                            // If input is already in output buffer all its ancestors are in a buffer as well.
                            continue; 
                        }
                    }
                }
                if (ancestor == null || ! ancestor.MoveTo(input)) {
                    ancestor = input.Clone();
                }
                while (ancestor.MoveToParent()) {
                    if (matches(ancestor)) {
                        if (!Insert(outputBuffer, ancestor)) {
                            // If input is already in output buffer all its ancestors are in a buffer as well.
                            break;
                        }
                    }
                }
            }
            return this;
        }

        public override XPathNodeIterator Clone() { return new XPathAncestorQuery(this); }
        public override int CurrentPosition { get { return outputBuffer.Count - count + 1; } }
        public override QueryProps Properties { get { return base.Properties | QueryProps.Reverse; } }

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
