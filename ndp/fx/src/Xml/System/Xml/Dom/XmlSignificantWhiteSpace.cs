//------------------------------------------------------------------------------
// <copyright file="XmlSignificantWhiteSpace.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml
{
    using System;
    using System.Xml.XPath;
    using System.Text;
    using System.Diagnostics;

    // Represents the text content of an element or attribute.
    public class XmlSignificantWhitespace : XmlCharacterData {
        protected internal XmlSignificantWhitespace( string strData, XmlDocument doc ) : base( strData, doc ) {
            if ( !doc.IsLoading && !base.CheckOnData( strData ) )
                throw new ArgumentException(Res.GetString(Res.Xdom_WS_Char));
        }

        // Gets the name of the node.
        public override String Name {
            get { 
                return OwnerDocument.strSignificantWhitespaceName;
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public override String LocalName {
            get { 
                return OwnerDocument.strSignificantWhitespaceName;
            }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType {
            get { 
                return XmlNodeType.SignificantWhitespace;
            }
        }

        public override XmlNode ParentNode {
            get {
                switch (parentNode.NodeType) {
                    case XmlNodeType.Document:
                        return base.ParentNode;
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
            return OwnerDocument.CreateSignificantWhitespace( Data );
        }

        public override String Value {
            get { 
                return Data;
            }

            set {
                if ( CheckOnData( value ) )
                    Data = value;
                else
                    throw new ArgumentException(Res.GetString(Res.Xdom_WS_Char));
            }
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w) {
            w.WriteString(Data);
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w) {
            // Intentionally do nothing
        }

        internal override XPathNodeType XPNodeType {
            get {
                XPathNodeType xnt = XPathNodeType.SignificantWhitespace;
                DecideXPNodeTypeForTextNodes(this, ref xnt);
                return xnt;
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
