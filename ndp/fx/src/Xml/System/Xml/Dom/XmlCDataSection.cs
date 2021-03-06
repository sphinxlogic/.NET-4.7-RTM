//------------------------------------------------------------------------------
// <copyright file="XmlCDATASection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml {
    using System;
    using System.Text;
    using System.Diagnostics;
    using System.Xml.XPath;

    // Used to quote or escape blocks of text to keep that text from being
    // interpreted as markup language.
    public class XmlCDataSection : XmlCharacterData {
        protected internal XmlCDataSection( string data, XmlDocument doc ): base( data, doc ) {
        }

        // Gets the name of the node.
        public override String Name { 
            get {
                return OwnerDocument.strCDataSectionName;
            }
        }

        // Gets the name of the node without the namespace prefix.
        public override String LocalName { 
            get {
                return OwnerDocument.strCDataSectionName;
            }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType {
            get { 
                return XmlNodeType.CDATA;
            }
        }

        public override XmlNode ParentNode {
            get {
                switch (parentNode.NodeType) {
                    case XmlNodeType.Document:
                        return null;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        XmlNode parent = parentNode.parentNode;
                        while (parent.IsText) {
                            parent = parent.parentNode;
                        }
                        return parent; 
                    default:
                        return parentNode;
                }
            }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep) {
            Debug.Assert( OwnerDocument != null );
            return OwnerDocument.CreateCDataSection( Data );
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w) {
            w.WriteCData( Data );
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w) {
            // Intentionally do nothing
        }

        internal override XPathNodeType XPNodeType { 
            get {
                return XPathNodeType.Text;
            }
        }

        internal override bool IsText {
            get {
                return true;
            }
        }

        public override XmlNode PreviousText {
            get {
                if (parentNode.IsText) {
                    return parentNode;
                }
                return null;
            }
        }
    }
}
