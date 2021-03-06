//------------------------------------------------------------------------------
// <copyright file="XmlEntity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml {
    using System.Diagnostics;

    // Represents a parsed or unparsed entity in the XML document.
    public class XmlEntity : XmlNode {
        string publicId;
        string systemId;
        String notationName;
        String name;
        String unparsedReplacementStr;
        String baseURI;
        XmlLinkedNode lastChild;
        private bool childrenFoliating;

        internal XmlEntity( String name, String strdata, string publicId, string systemId, String notationName, XmlDocument doc ) : base( doc ) {
            this.name = doc.NameTable.Add(name);
            this.publicId = publicId;
            this.systemId = systemId;
            this.notationName = notationName;
            this.unparsedReplacementStr = strdata;
            this.childrenFoliating = false;
        }

        // Throws an excption since an entity can not be cloned.
        public override XmlNode CloneNode(bool deep) {

              throw new InvalidOperationException(Res.GetString(Res.Xdom_Node_Cloning));
        }

        //
        // Microsoft extensions
        //

        // Gets a value indicating whether the node is read-only.
        public override bool IsReadOnly {
            get {
                return true;        // Make entities readonly
            }
        }


        // Gets the name of the node.
        public override string Name {
            get { return name;}
        }

        // Gets the name of the node without the namespace prefix.
        public override string LocalName {
            get { return name;}
        }

        // Gets the concatenated values of the entity node and all its children.
        // The property is read-only and when tried to be set, exception will be thrown.
        public override string InnerText {
            get { return base.InnerText; }
            set {
                throw new InvalidOperationException(Res.GetString(Res.Xdom_Ent_Innertext));
            }
        }

        internal override bool IsContainer {
            get { return true;}
        }

        internal override XmlLinkedNode LastNode {
            get {
                if (lastChild == null && !childrenFoliating)
                { //expand the unparsedreplacementstring
                    childrenFoliating = true;
                    //wrap the replacement string with an element
                    XmlLoader loader = new XmlLoader();
                    loader.ExpandEntity(this);
                }
                return lastChild;
            }
            set { lastChild = value;}
        }

        internal override bool IsValidChildType( XmlNodeType type ) {
            return(type == XmlNodeType.Text ||
                   type == XmlNodeType.Element ||
                   type == XmlNodeType.ProcessingInstruction ||
                   type == XmlNodeType.Comment ||
                   type == XmlNodeType.CDATA ||
                   type == XmlNodeType.Whitespace ||
                   type == XmlNodeType.SignificantWhitespace ||
                   type == XmlNodeType.EntityReference);
        }

        // Gets the type of the node.
        public override XmlNodeType NodeType {
            get { return XmlNodeType.Entity;}
        }

        // Gets the value of the public identifier on the entity declaration.
        public String PublicId {
            get { return publicId;}
        }

        // Gets the value of the system identifier on the entity declaration.
        public String SystemId {
            get { return systemId;}
        }

        // Gets the name of the optional NDATA attribute on the
        // entity declaration.
        public String NotationName {
            get { return notationName;}
        }

        //Without override these two functions, we can't guarantee that WriteTo()/WriteContent() functions will never be called
        public override String OuterXml {
            get { return String.Empty; }
        }

        public override String InnerXml {
            get { return String.Empty; }
            set { throw new InvalidOperationException( Res.GetString(Res.Xdom_Set_InnerXml ) ); }
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w) {
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w) {
        }

        public override String BaseURI {
            get { return baseURI; }
        }

        internal void SetBaseURI( String inBaseURI ) {
            baseURI = inBaseURI;
        }
    }
}
