//------------------------------------------------------------------------------
// <copyright file="XmlUnspecifiedAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml
{
    using System;

    internal class XmlUnspecifiedAttribute: XmlAttribute {
        bool fSpecified = false;


        protected internal XmlUnspecifiedAttribute( string prefix, string localName, string namespaceURI, XmlDocument doc )
        : base( prefix, localName, namespaceURI, doc ) {
        }

        public override bool Specified
        {
            get { return fSpecified;}
        }


        public override XmlNode CloneNode(bool deep) {
            //CloneNode is deep for attributes irrespective of parameter
            XmlDocument doc = OwnerDocument;
            XmlUnspecifiedAttribute attr = (XmlUnspecifiedAttribute)doc.CreateDefaultAttribute(Prefix, LocalName, NamespaceURI);
            attr.CopyChildren( doc, this, true );
            attr.fSpecified = true; //When clone, should return the specifed attribute as default
            return attr;
        }

        public override string InnerText {
            set {
                base.InnerText = value;
                fSpecified = true;
            }
        }

        public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild) {
            XmlNode node = base.InsertBefore( newChild, refChild );
            fSpecified = true;
            return node;
        }

        public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild) {
            XmlNode node = base.InsertAfter( newChild, refChild );
            fSpecified = true;
            return node;
        }

        public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild) {
            XmlNode node = base.ReplaceChild( newChild, oldChild );
            fSpecified = true;
            return node;
        }

        public override XmlNode RemoveChild(XmlNode oldChild) {
            XmlNode node = base.RemoveChild(oldChild);
            fSpecified = true;
            return node;
        }

        public override XmlNode AppendChild(XmlNode newChild) {
            XmlNode node = base.AppendChild(newChild);
            fSpecified = true;
            return node;
        }

        public override void WriteTo(XmlWriter w) {
            if (fSpecified)
                base.WriteTo( w );
        }

        internal void SetSpecified(bool f) {
            fSpecified = f;
        }
    }
}
