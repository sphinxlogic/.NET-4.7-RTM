//------------------------------------------------------------------------------
// <copyright file="XmlElement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

using System;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml {
    // Represents an element.
    public class XmlElement : XmlLinkedNode {
        XmlName name;
        XmlAttributeCollection attributes;
        XmlLinkedNode lastChild; // == this for empty elements otherwise it is the last child

        internal XmlElement( XmlName name, bool empty, XmlDocument doc ): base( doc ) {
            Debug.Assert(name!=null);
            this.parentNode = null;
            if ( !doc.IsLoading ) {
                XmlDocument.CheckName( name.Prefix );
                XmlDocument.CheckName( name.LocalName );
            }
            if (name.LocalName.Length == 0) 
                throw new ArgumentException(Res.GetString(Res.Xdom_Empty_LocalName));
            this.name = name;
            if (empty) {
                this.lastChild = this; 
            }
        }

        protected internal XmlElement( string prefix, string localName, string namespaceURI, XmlDocument doc ) 
        : this( doc.AddXmlName( prefix, localName, namespaceURI, null ), true, doc ) {
        }

        internal XmlName XmlName {
            get { return name; }
            set { name = value; }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep) {
            Debug.Assert( OwnerDocument != null );
            XmlDocument doc = OwnerDocument;
            bool OrigLoadingStatus = doc.IsLoading;
            doc.IsLoading = true;
            XmlElement element = doc.CreateElement( Prefix, LocalName, NamespaceURI );
            doc.IsLoading = OrigLoadingStatus;
            if ( element.IsEmpty != this.IsEmpty )
                element.IsEmpty = this.IsEmpty;

            if (HasAttributes) {
                foreach( XmlAttribute attr in Attributes ) {
                    XmlAttribute newAttr = (XmlAttribute)(attr.CloneNode(true));
                    if (attr is XmlUnspecifiedAttribute && attr.Specified == false)
                        ( ( XmlUnspecifiedAttribute )newAttr).SetSpecified(false);
                    element.Attributes.InternalAppendAttribute( newAttr );
                }
            }
            if (deep)
                element.CopyChildren( doc, this, deep );

            return element;
        }

        // Gets the name of the node.
        public override string Name { 
            get { return name.Name;}
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName { 
            get { return name.LocalName;}
        }

        // Gets the namespace URI of this node.
        public override string NamespaceURI { 
            get { return name.NamespaceURI;} 
        }

        // Gets or sets the namespace prefix of this node.
        public override string Prefix { 
            get { return name.Prefix;}
            set { name = name.OwnerDocument.AddXmlName( value, LocalName, NamespaceURI, SchemaInfo ); }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType {
            get { return XmlNodeType.Element;}
        }

        public override XmlNode ParentNode {
            get {
                return this.parentNode;
            }
        }

        // Gets the XmlDocument that contains this node.
        public override XmlDocument OwnerDocument { 
            get { 
                return name.OwnerDocument;
            }
        }

        internal override bool IsContainer {
            get { return true;}
        }

        //the function is provided only at Load time to speed up Load process
        internal override XmlNode AppendChildForLoad(XmlNode newChild, XmlDocument doc) {
            XmlNodeChangedEventArgs args = doc.GetInsertEventArgsForLoad( newChild, this );

            if (args != null)
                doc.BeforeEvent( args );

            XmlLinkedNode newNode = (XmlLinkedNode)newChild;

            if (lastChild == null 
                || lastChild == this) { // if LastNode == null 
                newNode.next = newNode;
                lastChild = newNode; // LastNode = newNode;
                newNode.SetParentForLoad(this);
            }
            else {
                XmlLinkedNode refNode = lastChild; // refNode = LastNode;
                newNode.next = refNode.next;
                refNode.next = newNode;
                lastChild = newNode; // LastNode = newNode;
                if (refNode.IsText
                    && newNode.IsText) {
                    NestTextNodes(refNode, newNode);
                }
                else {
                    newNode.SetParentForLoad(this);
                }
            }

            if (args != null)
                doc.AfterEvent( args );

            return newNode;
        }

        // Gets or sets whether the element does not have any children.
        public bool IsEmpty {
            get { 
                return lastChild == this;
            }

            set {
                if (value) {
                    if (lastChild != this) {
                        RemoveAllChildren();
                        lastChild = this;
                    }
                }
                else {
                    if (lastChild == this) {
                        lastChild = null;
                    }
                }

            }
        }

        internal override XmlLinkedNode LastNode {
            get { 
                return lastChild == this ? null : lastChild; 
            }

            set { 
                lastChild = value;
            }
        }

        internal override bool IsValidChildType( XmlNodeType type ) {
            switch (type) {
                case XmlNodeType.Element:
                case XmlNodeType.Text:
                case XmlNodeType.EntityReference:
                case XmlNodeType.Comment:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.CDATA:
                    return true;

                default:
                    return false;
            }
        }


        // Gets a XmlAttributeCollection containing the list of attributes for this node.
        public override XmlAttributeCollection Attributes { 
            get { 
                if (attributes == null) {
                    lock ( OwnerDocument.objLock ) {
                        if ( attributes == null ) {
                            attributes = new XmlAttributeCollection(this);
                        }
                    }
                }

                return attributes; 
            }
        }

        // Gets a value indicating whether the current node
        // has any attributes.
        public virtual bool HasAttributes {
            get {
                if ( this.attributes == null )
                    return false;
                else
                    return this.attributes.Count > 0;
            }
        }

        // Returns the value for the attribute with the specified name.
        public virtual string GetAttribute(string name) {
            XmlAttribute attr = GetAttributeNode(name);
            if (attr != null)
                return attr.Value;
            return String.Empty;
        }

        // Sets the value of the attribute
        // with the specified name.
        public virtual void SetAttribute(string name, string value) {
            XmlAttribute attr = GetAttributeNode(name);
            if (attr == null) {
                attr = OwnerDocument.CreateAttribute(name);
                attr.Value = value;
                Attributes.InternalAppendAttribute( attr );
            }
            else {
                attr.Value = value;
            }
        }

        // Removes an attribute by name.
        public virtual void RemoveAttribute(string name) {
            if (HasAttributes)
                Attributes.RemoveNamedItem(name);
        }

        // Returns the XmlAttribute with the specified name.
        public virtual XmlAttribute GetAttributeNode(string name) {
            if (HasAttributes)
                return Attributes[name];
            return null;
        }

        // Adds the specified XmlAttribute.
        public virtual XmlAttribute SetAttributeNode(XmlAttribute newAttr) {
            if ( newAttr.OwnerElement != null )
                throw new InvalidOperationException( Res.GetString(Res.Xdom_Attr_InUse) );
            return(XmlAttribute) Attributes.SetNamedItem(newAttr);
        }

        // Removes the specified XmlAttribute.
        public virtual XmlAttribute RemoveAttributeNode(XmlAttribute oldAttr) {
            if (HasAttributes)
                return(XmlAttribute) Attributes.Remove(oldAttr);
            return null;
        }

        // Returns a XmlNodeList containing
        // a list of all descendant elements that match the specified name.
        public virtual XmlNodeList GetElementsByTagName(string name) {
            return new XmlElementList( this, name );
        }

        //
        // DOM Level 2
        //

        // Returns the value for the attribute with the specified LocalName and NamespaceURI.
        public virtual string GetAttribute(string localName, string namespaceURI) {
            XmlAttribute attr = GetAttributeNode( localName, namespaceURI );
            if (attr != null)
                return attr.Value;
            return String.Empty;
        }

        // Sets the value of the attribute with the specified name
        // and namespace.
        public virtual string SetAttribute(string localName, string namespaceURI, string value) {
            XmlAttribute attr = GetAttributeNode( localName, namespaceURI );
            if (attr == null) {
                attr = OwnerDocument.CreateAttribute( string.Empty, localName, namespaceURI );
                attr.Value = value;
                Attributes.InternalAppendAttribute( attr );
            }
            else {
                attr.Value = value;
            }

            return value;
        }

        // Removes an attribute specified by LocalName and NamespaceURI.
        public virtual void RemoveAttribute(string localName, string namespaceURI) {
            //Debug.Assert(namespaceURI != null);
            RemoveAttributeNode( localName, namespaceURI );
        }

        // Returns the XmlAttribute with the specified LocalName and NamespaceURI.
        public virtual XmlAttribute GetAttributeNode(string localName, string namespaceURI) {
            //Debug.Assert(namespaceURI != null);
            if (HasAttributes)
                return Attributes[ localName, namespaceURI ];
            return null;
        }

        // Adds the specified XmlAttribute.
        public virtual XmlAttribute SetAttributeNode(string localName, string namespaceURI) {
            XmlAttribute attr = GetAttributeNode( localName, namespaceURI );
            if (attr == null) {
                attr = OwnerDocument.CreateAttribute( string.Empty, localName, namespaceURI );
                Attributes.InternalAppendAttribute( attr );
            }
            return attr;
        }

        // Removes the XmlAttribute specified by LocalName and NamespaceURI.
        public virtual XmlAttribute RemoveAttributeNode(string localName, string namespaceURI) {
            //Debug.Assert(namespaceURI != null);
            if (HasAttributes) {
                XmlAttribute attr = GetAttributeNode( localName, namespaceURI );
                Attributes.Remove( attr );
                return attr;
            }
            return null;
        }

        // Returns a XmlNodeList containing 
        // a list of all descendant elements that match the specified name.
        public virtual XmlNodeList GetElementsByTagName(string localName, string namespaceURI) {
            //Debug.Assert(namespaceURI != null);
            return new XmlElementList( this, localName, namespaceURI );
        }

        // Determines whether the current node has the specified attribute.
        public virtual bool HasAttribute(string name) {
            return GetAttributeNode(name) != null;
        }

        // Determines whether the current node has the specified
        // attribute from the specified namespace.
        public virtual bool HasAttribute(string localName, string namespaceURI) {
            return GetAttributeNode(localName, namespaceURI) != null;
        }

        // Saves the current node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w) {

            if (GetType() == typeof(XmlElement)) {
                // Use the non-recursive version (for XmlElement only)
                WriteElementTo(w, this);
            }
            else {
                // Use the (potentially) recursive version
                WriteStartElement(w);

                if (IsEmpty) {
                    w.WriteEndElement();
                }
                else {
                    WriteContentTo(w);
                    w.WriteFullEndElement();
                }
            }
        }

        // This method is copied from System.Xml.Linq.ElementWriter.WriteElement but adapted to DOM
        private static void WriteElementTo(XmlWriter writer, XmlElement e) {
            XmlNode root = e;
            XmlNode n = e;
            while (true) {
                e = n as XmlElement;
                // Only use the inlined write logic for XmlElement, not for derived classes
                if (e != null && e.GetType() == typeof(XmlElement)) {
                    // Write the element
                    e.WriteStartElement(writer);
                    // Write the element's content
                    if (e.IsEmpty) {
                        // No content; use a short end element <a />
                        writer.WriteEndElement();
                    }
                    else if (e.lastChild == null) {
                        // No actual content; use a full end element <a></a>
                        writer.WriteFullEndElement();
                    }
                    else {
                        // There are child node(s); move to first child
                        n = e.FirstChild;
                        Debug.Assert(n != null);
                        continue;
                    }
                }
                else {
                    // Use virtual dispatch (might recurse)
                    n.WriteTo(writer);
                }
                // Go back to the parent after writing the last child
                while (n != root && n == n.ParentNode.LastChild) {
                    n = n.ParentNode;
                    Debug.Assert(n != null);
                    writer.WriteFullEndElement();
                }
                if (n == root)
                    break;
                n = n.NextSibling;
                Debug.Assert(n != null);
            }
        }

        // Writes the start of the element (and its attributes) to the specified writer
        private void WriteStartElement(XmlWriter w) {
            w.WriteStartElement(Prefix, LocalName, NamespaceURI);

            if (HasAttributes) {
                XmlAttributeCollection attrs = Attributes;
                for (int i = 0; i < attrs.Count; i += 1) {
                    XmlAttribute attr = attrs[i];
                    attr.WriteTo(w);
                }
            }
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w) {
            for (XmlNode node = FirstChild; node != null; node = node.NextSibling) {
                node.WriteTo(w);
            }
        }

        // Removes the attribute node with the specified index from the attribute collection.
        public virtual XmlNode RemoveAttributeAt(int i) {
            if (HasAttributes)
                return attributes.RemoveAt( i );
            return null;
        }

        // Removes all attributes from the element.
        public virtual void RemoveAllAttributes() {
            if (HasAttributes) {
                attributes.RemoveAll();
            }
        }

        // Removes all the children and/or attributes
        // of the current node.
        public override void RemoveAll() {
            //remove all the children
            base.RemoveAll(); 
            //remove all the attributes
            RemoveAllAttributes();
        }
        
        internal void RemoveAllChildren() {
            base.RemoveAll();
        }

        public override IXmlSchemaInfo SchemaInfo {
            get {
                return name;
            }
        }

        // Gets or sets the markup representing just
        // the children of this node.
        public override string InnerXml {
            get {
                return base.InnerXml;
            }
            set {
                RemoveAllChildren();
                XmlLoader loader = new XmlLoader();
                loader.LoadInnerXmlElement( this, value );
            }
        }

        // Gets or sets the concatenated values of the
        // node and all its children.
        public override string InnerText { 
            get {
                return base.InnerText;
            }
            set {
                XmlLinkedNode linkedNode = LastNode;
                if (linkedNode != null && //there is one child
                    linkedNode.NodeType == XmlNodeType.Text && //which is text node
                    linkedNode.next == linkedNode ) // and it is the only child 
                {
                    //this branch is for perf reason, event fired when TextNode.Value is changed.
                    linkedNode.Value = value;
                } 
                else {
                    RemoveAllChildren();
                    AppendChild( OwnerDocument.CreateTextNode( value ) );
                }
            }
        }

        public override XmlNode NextSibling { 
            get { 
                if (this.parentNode != null
                    && this.parentNode.LastNode != this)
                    return next;
                return null; 
            } 
        }

        internal override void SetParent(XmlNode node) {
            this.parentNode = node;
        }

        internal override XPathNodeType XPNodeType { get { return XPathNodeType.Element; } }

        internal override string XPLocalName { get { return LocalName; } }

        internal override string GetXPAttribute( string localName, string ns ) {
            if ( ns == OwnerDocument.strReservedXmlns )
                return null;
            XmlAttribute attr = GetAttributeNode( localName, ns );
            if ( attr != null )
                return attr.Value;
            return string.Empty;
        }
    }
}
