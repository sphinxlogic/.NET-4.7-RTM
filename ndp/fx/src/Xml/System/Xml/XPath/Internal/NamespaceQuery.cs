//------------------------------------------------------------------------------
// <copyright file="NamespaceQuery.cs" company="Microsoft">
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

    internal sealed class NamespaceQuery : BaseAxisQuery {
        private bool onNamespace;
        
        public NamespaceQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type) : base(qyParent, Name, Prefix, Type) {}
        private NamespaceQuery(NamespaceQuery other) : base(other) {
            this.onNamespace = other.onNamespace;
        }

        public override void Reset() {
            onNamespace = false;
            base.Reset();
        }

        public override XPathNavigator Advance() {
            while (true) {
                if (!onNamespace) {
                    currentNode = qyInput.Advance();
                    if (currentNode == null) {
                        return null;
                    }
                    position = 0;
                    currentNode = currentNode.Clone();
                    onNamespace = currentNode.MoveToFirstNamespace();
                } else {
                    onNamespace = currentNode.MoveToNextNamespace();
                }

                if (onNamespace) {
                    if (matches(currentNode)) {
                        position++;
                        return currentNode;
                    }
                }
            } // while
        } // Advance

        public override bool matches(XPathNavigator e) {
            Debug.Assert(e.NodeType == XPathNodeType.Namespace);
            if (e.Value.Length == 0) {
                Debug.Assert(e.LocalName.Length == 0, "Only xmlns='' can have empty string as a value");
                // Namespace axes never returns xmlns='', 
                // because it's not a NS declaration but rather undeclaration.
                return false;               
            }
            if (NameTest) {
                return Name.Equals(e.LocalName);
            } else {
                return true;
            }
        }
                    
        public override XPathNodeIterator Clone() { return new NamespaceQuery(this); }
    }
}
