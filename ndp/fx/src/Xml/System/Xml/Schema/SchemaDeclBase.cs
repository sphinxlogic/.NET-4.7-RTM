//------------------------------------------------------------------------------
// <copyright file="SchemaDeclBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
// <owner current="true" primary="true">Microsoft</owner>                                                               
//------------------------------------------------------------------------------

namespace System.Xml.Schema {

    using System.Collections.Generic;
    using System.Diagnostics;

    internal abstract class SchemaDeclBase {
        internal enum Use {
            Default,
            Required,
            Implied,
            Fixed,
            RequiredFixed
        };

        protected XmlQualifiedName  name = XmlQualifiedName.Empty;
        protected string            prefix;
        protected bool isDeclaredInExternal = false;
        protected Use               presence;     // the presence, such as fixed, implied, etc

#if !SILVERLIGHT
        protected XmlSchemaType     schemaType;
        protected XmlSchemaDatatype datatype;

        protected string            defaultValueRaw;       // default value in its original form
        protected object            defaultValueTyped;       

        protected long               maxLength; // dt:maxLength
        protected long               minLength; // dt:minLength

        protected List<string> values;    // array of values for enumerated and notation types
#endif

        protected SchemaDeclBase(XmlQualifiedName name, string prefix) {
            this.name = name;
            this.prefix = prefix;
#if !SILVERLIGHT
            maxLength = -1;
            minLength = -1;
#endif
        }

#if !SILVERLIGHT
        protected SchemaDeclBase() {
        }
#endif

        internal XmlQualifiedName Name {
            get { return name;}
            set { name = value;}
        }

        internal string Prefix {
            get { return(prefix == null) ? string.Empty : prefix;}
            set { prefix = value;}
        }

        internal bool IsDeclaredInExternal {
            get { return isDeclaredInExternal;}
            set { isDeclaredInExternal = value;}
        }

        internal Use Presence {
            get { return presence; }
            set { presence = value; }
        }

#if !SILVERLIGHT
        internal long MaxLength {
            get { return maxLength;}
            set { maxLength = value;}
        }

        internal long MinLength {
            get { return minLength;}
            set { minLength = value;}
        }

        internal XmlSchemaType SchemaType {
            get { return schemaType;}
            set { schemaType = value;}
        }

        internal XmlSchemaDatatype Datatype {
            get { return datatype;}
            set { datatype = value;}
        }

        internal void AddValue(string value) {
            if (values == null) {
                values = new List<string>();
            }
            values.Add(value);
        }

        internal List<string> Values {
            get { return values; }
            set { values = value; }
        }

        internal string DefaultValueRaw {
            get { return(defaultValueRaw != null) ? defaultValueRaw : string.Empty;}
            set { defaultValueRaw = value;}
        }

        internal object DefaultValueTyped {
            get { return defaultValueTyped;}
            set { defaultValueTyped = value;}
        }

        internal bool CheckEnumeration(object pVal) {
            return (datatype.TokenizedType != XmlTokenizedType.NOTATION && datatype.TokenizedType != XmlTokenizedType.ENUMERATION) || values.Contains(pVal.ToString());
        }

        internal bool CheckValue(Object pVal) {
            return (presence != Use.Fixed && presence != Use.RequiredFixed) || (defaultValueTyped != null && datatype.IsEqual(pVal, defaultValueTyped));
        }
#endif
    };

}
