//------------------------------------------------------------------------------
// <copyright file="Preprocessor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>   
// <owner current="true" primary="true">Microsoft</owner>                                                             
//------------------------------------------------------------------------------

namespace System.Xml.Schema {

    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Diagnostics;
    using System.Runtime.Versioning;

#pragma warning disable 618
    internal sealed class SchemaCollectionPreprocessor  : BaseProcessor {
        enum Compositor {
            Root,
            Include,
            Import
        };

        XmlSchema schema;
        string targetNamespace;
        bool buildinIncluded = false;
        XmlSchemaForm elementFormDefault;
        XmlSchemaForm attributeFormDefault;
        XmlSchemaDerivationMethod blockDefault;
        XmlSchemaDerivationMethod finalDefault;
        //Dictionary<Uri, Uri> schemaLocations;
        Hashtable schemaLocations;
        Hashtable referenceNamespaces;
        
        string Xmlns;
        const XmlSchemaDerivationMethod schemaBlockDefaultAllowed   = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Substitution;
        const XmlSchemaDerivationMethod schemaFinalDefaultAllowed   = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union;
        const XmlSchemaDerivationMethod elementBlockAllowed         = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Substitution;
        const XmlSchemaDerivationMethod elementFinalAllowed         = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension;
        const XmlSchemaDerivationMethod simpleTypeFinalAllowed      = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union;
        const XmlSchemaDerivationMethod complexTypeBlockAllowed     = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension;
        const XmlSchemaDerivationMethod complexTypeFinalAllowed     = XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.Extension;

        private XmlResolver xmlResolver = null; 

        public SchemaCollectionPreprocessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventHandler) 
            : base(nameTable, schemaNames, eventHandler) {
        }

        public bool Execute(XmlSchema schema, string targetNamespace, bool loadExternals, XmlSchemaCollection xsc) {
            this.schema = schema;
            Xmlns = NameTable.Add("xmlns");

            Cleanup(schema);
            if (loadExternals && xmlResolver != null) {
                schemaLocations = new Hashtable(); //new Dictionary<Uri, Uri>();
                if (schema.BaseUri != null) {
                    schemaLocations.Add(schema.BaseUri, schema.BaseUri);
                }
                LoadExternals(schema, xsc);
            }
            ValidateIdAttribute(schema);
            Preprocess(schema, targetNamespace, Compositor.Root);
            if (!HasErrors) {
                schema.IsPreprocessed = true;
                for (int i = 0; i < schema.Includes.Count; ++i) {
                    XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                    if (include.Schema != null) {
                        include.Schema.IsPreprocessed = true;
                    }
                }
            }
            return !HasErrors;
        }

        private void Cleanup(XmlSchema schema) {
            if (schema.IsProcessing) {
                return;
            }
            schema.IsProcessing = true;

            for (int i = 0; i < schema.Includes.Count; ++i) {
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];

                if (include.Schema != null) {
                    Cleanup(include.Schema);
                }

                if (include is XmlSchemaRedefine) {
                    XmlSchemaRedefine rdef = include as XmlSchemaRedefine;
                    rdef.AttributeGroups.Clear();
                    rdef.Groups.Clear();
                    rdef.SchemaTypes.Clear();
                }
                
            }

            schema.Attributes.Clear();
            schema.AttributeGroups.Clear();
            schema.SchemaTypes.Clear();
            schema.Elements.Clear();
            schema.Groups.Clear();
            schema.Notations.Clear();
            schema.Ids.Clear();
            schema.IdentityConstraints.Clear();

            schema.IsProcessing = false;
        }

        internal XmlResolver XmlResolver {
            set {
                xmlResolver = value;
            }
        }

        // SxS: This method reads resource names from the source documents and does not return any resources to the caller 
        // It's fine to suppress the SxS warning
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.None)]
        private void LoadExternals(XmlSchema schema, XmlSchemaCollection xsc) {
            if (schema.IsProcessing) {
                return;
            }
            schema.IsProcessing = true;
            for (int i = 0; i < schema.Includes.Count; ++i) {
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                Uri includeLocation = null;
                //CASE 1: If the Schema object of the include has been set 
                if (include.Schema != null) {
                    // already loaded
                    if (include is XmlSchemaImport && ((XmlSchemaImport)include).Namespace == XmlReservedNs.NsXml) {
                        buildinIncluded = true;
                    }
                    else {
                        includeLocation = include.BaseUri;
                        if (includeLocation != null && schemaLocations[includeLocation] == null) {
                            schemaLocations.Add(includeLocation, includeLocation);
                        }
                        LoadExternals(include.Schema, xsc);
                    }
                    continue;
                }

                //CASE 2: If the include has been already added to the schema collection directly
                if (xsc != null && include is XmlSchemaImport) { //Added for SchemaCollection compatibility
                    XmlSchemaImport import = (XmlSchemaImport)include;
                    string importNS =  import.Namespace != null ? import.Namespace : string.Empty;
                    include.Schema = xsc[importNS]; //Fetch it from the collection
                    if (include.Schema != null) {
                        include.Schema   = include.Schema.Clone();
                        if (include.Schema.BaseUri != null && schemaLocations[include.Schema.BaseUri] == null) {
                            schemaLocations.Add(include.Schema.BaseUri, include.Schema.BaseUri);
                        }
                        //To avoid re-including components that were already included through a different path
                        Uri subUri = null;
                        for (int j = 0; j < include.Schema.Includes.Count; ++j) {
                            XmlSchemaExternal subInc = (XmlSchemaExternal)include.Schema.Includes[j];
                            if (subInc is XmlSchemaImport) {
                                XmlSchemaImport subImp = (XmlSchemaImport)subInc;
                                subUri = subImp.BaseUri != null ? subImp.BaseUri : (subImp.Schema != null && subImp.Schema.BaseUri != null ? subImp.Schema.BaseUri : null);
                                if (subUri != null) {
                                    if(schemaLocations[subUri] != null) {
                                        subImp.Schema = null; //So that the components are not included again
                                    }
                                    else { //if its not there already, add it
                                        schemaLocations.Add(subUri, subUri); //The schema for that location is available
                                    }
                                }
                            }
                        }
                        continue;
                    }
                }

                 //CASE 3: If the imported namespace is the XML namespace, load built-in schema
                if (include is XmlSchemaImport && ((XmlSchemaImport)include).Namespace == XmlReservedNs.NsXml) {
                    if (!buildinIncluded) {
                        buildinIncluded = true;
                        include.Schema = Preprocessor.GetBuildInSchema();
                    }
                    continue;
                }
                
                //CASE4: Parse schema from the provided location
                string schemaLocation = include.SchemaLocation;
                if (schemaLocation == null) {
                    continue;
                }

                Uri ruri = ResolveSchemaLocationUri(schema, schemaLocation);

                if (ruri != null && schemaLocations[ruri] == null) {
                    Stream stream = GetSchemaEntity(ruri);
                    if (stream != null) {
                        include.BaseUri = ruri;
                        schemaLocations.Add(ruri, ruri);
                        XmlTextReader reader = new XmlTextReader(ruri.ToString(), stream, NameTable);
                        reader.XmlResolver = xmlResolver;
                        try {
                            Parser parser = new Parser(SchemaType.XSD, NameTable, SchemaNames, EventHandler);
                            parser.Parse(reader, null);
                            while(reader.Read());// wellformness check
                            include.Schema = parser.XmlSchema;
                            LoadExternals(include.Schema, xsc);
                        }
                        catch(XmlSchemaException e) {
                            SendValidationEventNoThrow(new XmlSchemaException(Res.Sch_CannotLoadSchema, new string[] {schemaLocation, e.Message}, e.SourceUri, e.LineNumber, e.LinePosition), XmlSeverityType.Error);
                        }
                        catch(Exception) {
                            SendValidationEvent(Res.Sch_InvalidIncludeLocation, include, XmlSeverityType.Warning);
                        }
                        finally {
                            reader.Close();
                        }
                        
                    }
                    else {
                        SendValidationEvent(Res.Sch_InvalidIncludeLocation, include, XmlSeverityType.Warning);
                    }
                }
                
            }
            schema.IsProcessing = false;
        }


        private void BuildRefNamespaces(XmlSchema schema) {
            referenceNamespaces = new Hashtable();
            XmlSchemaImport import;
            string ns;

            //Add XSD namespace
            referenceNamespaces.Add(XmlReservedNs.NsXs,XmlReservedNs.NsXs);
            referenceNamespaces.Add(string.Empty, string.Empty);            

            for (int i = 0; i < schema.Includes.Count; ++i) {
                import = schema.Includes[i] as XmlSchemaImport;
                if (import != null) {
                    ns = import.Namespace;
                    if(ns != null && referenceNamespaces[ns] == null) 
                      referenceNamespaces.Add(ns,ns);
                }
            }
            
            //Add the schema's targetnamespace 
            if(schema.TargetNamespace != null && referenceNamespaces[schema.TargetNamespace] == null)
                referenceNamespaces.Add(schema.TargetNamespace,schema.TargetNamespace);
           
        }

        private void Preprocess(XmlSchema schema, string targetNamespace, Compositor compositor) {
            if (schema.IsProcessing) {
                return;
            }
            schema.IsProcessing = true;
            string tns = schema.TargetNamespace;
            if (tns != null) {
                schema.TargetNamespace = tns = NameTable.Add(tns);
                if (tns.Length == 0) {
                    SendValidationEvent(Res.Sch_InvalidTargetNamespaceAttribute, schema);
                }
                else {
                    try {
                        XmlConvert.ToUri(tns);  // can throw
                    }
                    catch {
                        SendValidationEvent(Res.Sch_InvalidNamespace, schema.TargetNamespace, schema);
                    }
                }
            }

            if (schema.Version != null) {
                try {
                    XmlConvert.VerifyTOKEN(schema.Version); // can throw
                } 
                catch (Exception) {
                    SendValidationEvent(Res.Sch_AttributeValueDataType, "version", schema);
                }
            }
            switch (compositor) {
            case Compositor.Root:
                if (targetNamespace == null && schema.TargetNamespace != null) { // not specified
                    targetNamespace = schema.TargetNamespace;
                }
                else if (schema.TargetNamespace == null && targetNamespace != null && targetNamespace.Length == 0) { // no namespace schema
                    targetNamespace = null;
                }
                if (targetNamespace != schema.TargetNamespace) {
                    SendValidationEvent(Res.Sch_MismatchTargetNamespaceEx, targetNamespace, schema.TargetNamespace, schema);
                }
                break;
            case Compositor.Import:
                if (targetNamespace != schema.TargetNamespace) {
                    SendValidationEvent(Res.Sch_MismatchTargetNamespaceImport, targetNamespace, schema.TargetNamespace, schema);
                }
                break;
            case Compositor.Include:
                if (schema.TargetNamespace != null) {
                    if (targetNamespace != schema.TargetNamespace) {
                        SendValidationEvent(Res.Sch_MismatchTargetNamespaceInclude, targetNamespace, schema.TargetNamespace, schema);
                    }
                }
                break;
            }

            for (int i = 0; i < schema.Includes.Count; ++i) {
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                SetParent(include, schema);
                PreprocessAnnotation(include);

                string loc = include.SchemaLocation;
                if (loc != null) {
                    try {
                        XmlConvert.ToUri(loc); // can throw
                    } 
                    catch {
                        SendValidationEvent(Res.Sch_InvalidSchemaLocation, loc, include);
                    }
                }
                else if((include is XmlSchemaRedefine || include is XmlSchemaInclude) && include.Schema == null) {
                    SendValidationEvent(Res.Sch_MissRequiredAttribute, "schemaLocation", include);
                }   
                if (include.Schema != null) {
                    if (include is XmlSchemaRedefine) {
                        Preprocess(include.Schema, schema.TargetNamespace, Compositor.Include);
                    }
                    else if (include is XmlSchemaImport) {
                        if (((XmlSchemaImport)include).Namespace == null && schema.TargetNamespace == null) {
                            SendValidationEvent(Res.Sch_ImportTargetNamespaceNull, include);
                        }
                        else if (((XmlSchemaImport)include).Namespace == schema.TargetNamespace) {
                            SendValidationEvent(Res.Sch_ImportTargetNamespace, include);
                        }
                        Preprocess(include.Schema, ((XmlSchemaImport)include).Namespace, Compositor.Import);
                    }
                    else {
                        Preprocess(include.Schema, schema.TargetNamespace, Compositor.Include);
                    }
                }
                else if (include is XmlSchemaImport) {
                    string ns = ((XmlSchemaImport)include).Namespace;
                    if (ns != null) {
                        if (ns.Length == 0) {
                            SendValidationEvent(Res.Sch_InvalidNamespaceAttribute, ns, include);                                        
                        }
                        else {
                            try {
                                XmlConvert.ToUri(ns); //can throw
                            }
                            catch(FormatException) {
                                SendValidationEvent(Res.Sch_InvalidNamespace, ns, include);
                            }
                        }
                    }
                }
            }

            //Begin processing the current schema passed to preprocess
            //Build the namespaces that can be referenced in the current schema
            BuildRefNamespaces(schema);

            this.targetNamespace = targetNamespace == null ? string.Empty : targetNamespace;

            if (schema.BlockDefault == XmlSchemaDerivationMethod.All) {
                this.blockDefault = XmlSchemaDerivationMethod.All;
            }
            else if (schema.BlockDefault == XmlSchemaDerivationMethod.None) {
                this.blockDefault = XmlSchemaDerivationMethod.Empty;
            }
            else {
                if ((schema.BlockDefault & ~schemaBlockDefaultAllowed) != 0) {
                    SendValidationEvent(Res.Sch_InvalidBlockDefaultValue, schema);
                }
                this.blockDefault = schema.BlockDefault & schemaBlockDefaultAllowed;
            }
            if (schema.FinalDefault == XmlSchemaDerivationMethod.All) {
                this.finalDefault = XmlSchemaDerivationMethod.All;
            }
            else if (schema.FinalDefault == XmlSchemaDerivationMethod.None) {
                this.finalDefault = XmlSchemaDerivationMethod.Empty;
            }
            else {
                if ((schema.FinalDefault & ~schemaFinalDefaultAllowed) != 0) {
                    SendValidationEvent(Res.Sch_InvalidFinalDefaultValue, schema);
                }
                this.finalDefault = schema.FinalDefault & schemaFinalDefaultAllowed;
            }
            this.elementFormDefault = schema.ElementFormDefault;
            if (this.elementFormDefault == XmlSchemaForm.None) {
                this.elementFormDefault = XmlSchemaForm.Unqualified;
            }
            this.attributeFormDefault = schema.AttributeFormDefault;
            if (this.attributeFormDefault == XmlSchemaForm.None) {
                this.attributeFormDefault = XmlSchemaForm.Unqualified;
            }

            for (int i = 0; i < schema.Includes.Count; ++i) {
                XmlSchemaExternal include = (XmlSchemaExternal)schema.Includes[i];
                if (include is XmlSchemaRedefine) {
                    XmlSchemaRedefine redefine = (XmlSchemaRedefine)include;
                    if (include.Schema != null) {
                        PreprocessRedefine(redefine);
                    }
                    else {
                        for (int j = 0; j < redefine.Items.Count; ++j) {
                            if (!(redefine.Items[j] is XmlSchemaAnnotation)) {
                                SendValidationEvent(Res.Sch_RedefineNoSchema, redefine);
                                break;
                            }
                        }

                    }
                }
                XmlSchema includedSchema = include.Schema;
                if (includedSchema != null) {
                    foreach (XmlSchemaElement element in includedSchema.Elements.Values) {
                        AddToTable(schema.Elements, element.QualifiedName, element);
                    }
                    foreach (XmlSchemaAttribute attribute in includedSchema.Attributes.Values) {
                        AddToTable(schema.Attributes, attribute.QualifiedName, attribute);
                    }
                    foreach (XmlSchemaGroup group in includedSchema.Groups.Values) {
                        AddToTable(schema.Groups, group.QualifiedName, group);
                    }
                    foreach (XmlSchemaAttributeGroup attributeGroup in includedSchema.AttributeGroups.Values) {
                        AddToTable(schema.AttributeGroups, attributeGroup.QualifiedName, attributeGroup);
                    }
                    foreach (XmlSchemaType type in includedSchema.SchemaTypes.Values) {
                        AddToTable(schema.SchemaTypes, type.QualifiedName, type);
                    }
                    foreach (XmlSchemaNotation notation in includedSchema.Notations.Values) {
                        AddToTable(schema.Notations, notation.QualifiedName, notation);
                    }
                } 
                ValidateIdAttribute(include);
            }
           
            List<XmlSchemaObject> removeItemsList = new List<XmlSchemaObject>();
            for (int i = 0; i < schema.Items.Count; ++i) {
                SetParent(schema.Items[i], schema);
                XmlSchemaAttribute attribute = schema.Items[i] as XmlSchemaAttribute;
                if (attribute != null) {
                    PreprocessAttribute(attribute);
                    AddToTable(schema.Attributes, attribute.QualifiedName, attribute);
                } 
                else if (schema.Items[i] is XmlSchemaAttributeGroup) {
                    XmlSchemaAttributeGroup attributeGroup = (XmlSchemaAttributeGroup)schema.Items[i];
                    PreprocessAttributeGroup(attributeGroup);
                    AddToTable(schema.AttributeGroups, attributeGroup.QualifiedName, attributeGroup);
                } 
                else if (schema.Items[i] is XmlSchemaComplexType) {
                    XmlSchemaComplexType complexType = (XmlSchemaComplexType)schema.Items[i];
                    PreprocessComplexType(complexType, false);
                    AddToTable(schema.SchemaTypes, complexType.QualifiedName, complexType);
                } 
                else if (schema.Items[i] is XmlSchemaSimpleType) {
                    XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)schema.Items[i];
                    PreprocessSimpleType(simpleType, false);
                    AddToTable(schema.SchemaTypes, simpleType.QualifiedName, simpleType);
                } 
                else if (schema.Items[i] is XmlSchemaElement) {
                    XmlSchemaElement element = (XmlSchemaElement)schema.Items[i];
                    PreprocessElement(element);
                    AddToTable(schema.Elements, element.QualifiedName, element);
                } 
                else if (schema.Items[i] is XmlSchemaGroup) {
                    XmlSchemaGroup group = (XmlSchemaGroup)schema.Items[i];
                    PreprocessGroup(group);
                    AddToTable(schema.Groups, group.QualifiedName, group);
                } 
                else if (schema.Items[i] is XmlSchemaNotation) {
                    XmlSchemaNotation notation = (XmlSchemaNotation)schema.Items[i];
                    PreprocessNotation(notation);
                    AddToTable(schema.Notations, notation.QualifiedName, notation);
                }
                else if(!(schema.Items[i] is XmlSchemaAnnotation)) {
                    SendValidationEvent(Res.Sch_InvalidCollection, schema.Items[i]);
                    removeItemsList.Add(schema.Items[i]);
                }
            }
            for (int i = 0; i < removeItemsList.Count; ++i) {
                schema.Items.Remove(removeItemsList[i]);          
            }

            schema.IsProcessing = false;
        }

        private void PreprocessRedefine(XmlSchemaRedefine redefine) {
            for (int i = 0; i < redefine.Items.Count; ++i) {
                SetParent(redefine.Items[i], redefine);
                XmlSchemaGroup group = redefine.Items[i] as XmlSchemaGroup;
                if (group != null) {
                    PreprocessGroup(group);
                    if (redefine.Groups[group.QualifiedName] != null) {
                        SendValidationEvent(Res.Sch_GroupDoubleRedefine, group);
                    }
                    else {
                        AddToTable(redefine.Groups, group.QualifiedName, group);
                        group.Redefined = (XmlSchemaGroup)redefine.Schema.Groups[group.QualifiedName];
                        if (group.Redefined != null) {
                            CheckRefinedGroup(group);
                        }
                        else {
                            SendValidationEvent(Res.Sch_GroupRedefineNotFound, group);
                        }
                    }
                } 
                else if (redefine.Items[i] is XmlSchemaAttributeGroup) {
                    XmlSchemaAttributeGroup attributeGroup = (XmlSchemaAttributeGroup)redefine.Items[i];
                    PreprocessAttributeGroup(attributeGroup);
                    if (redefine.AttributeGroups[attributeGroup.QualifiedName] != null) {
                        SendValidationEvent(Res.Sch_AttrGroupDoubleRedefine, attributeGroup);
                    }
                    else {
                        AddToTable(redefine.AttributeGroups, attributeGroup.QualifiedName, attributeGroup);
                        attributeGroup.Redefined = (XmlSchemaAttributeGroup)redefine.Schema.AttributeGroups[attributeGroup.QualifiedName];
                        if (attributeGroup.Redefined != null) {
                            CheckRefinedAttributeGroup(attributeGroup);
                        }
                        else  {
                            SendValidationEvent(Res.Sch_AttrGroupRedefineNotFound, attributeGroup);
                        }
                    }
                } 
                else if (redefine.Items[i] is XmlSchemaComplexType) {
                    XmlSchemaComplexType complexType = (XmlSchemaComplexType)redefine.Items[i];
                    PreprocessComplexType(complexType, false);
                    if (redefine.SchemaTypes[complexType.QualifiedName] != null) {
                        SendValidationEvent(Res.Sch_ComplexTypeDoubleRedefine, complexType);
                    }
                    else {
                        AddToTable(redefine.SchemaTypes, complexType.QualifiedName, complexType);
                        XmlSchemaType type = (XmlSchemaType)redefine.Schema.SchemaTypes[complexType.QualifiedName];
                        if (type != null) {
                            if (type is XmlSchemaComplexType) {
                                complexType.Redefined = type;
                                CheckRefinedComplexType(complexType);
                            }
                            else {
                                SendValidationEvent(Res.Sch_SimpleToComplexTypeRedefine, complexType);
                            }
                        }
                        else {
                            SendValidationEvent(Res.Sch_ComplexTypeRedefineNotFound, complexType);
                        }
                    }
                } 
                else if (redefine.Items[i] is XmlSchemaSimpleType) {
                    XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)redefine.Items[i];
                    PreprocessSimpleType(simpleType, false);
                    if (redefine.SchemaTypes[simpleType.QualifiedName] != null) {
                        SendValidationEvent(Res.Sch_SimpleTypeDoubleRedefine, simpleType);
                    }
                    else {
                        AddToTable(redefine.SchemaTypes, simpleType.QualifiedName, simpleType);
                        XmlSchemaType type = (XmlSchemaType)redefine.Schema.SchemaTypes[simpleType.QualifiedName];
                        if (type != null) {
                            if (type is XmlSchemaSimpleType) {
                                simpleType.Redefined = type;
                                CheckRefinedSimpleType(simpleType);
                            }
                            else {
                                SendValidationEvent(Res.Sch_ComplexToSimpleTypeRedefine, simpleType);
                            }
                        }
                        else {
                            SendValidationEvent(Res.Sch_SimpleTypeRedefineNotFound, simpleType);
                        }
                    }
                }
            }

            foreach (DictionaryEntry entry in redefine.Groups) {
                redefine.Schema.Groups.Insert((XmlQualifiedName)entry.Key, (XmlSchemaObject)entry.Value);
            }
            foreach (DictionaryEntry entry in redefine.AttributeGroups) {
                redefine.Schema.AttributeGroups.Insert((XmlQualifiedName)entry.Key, (XmlSchemaObject)entry.Value);
            }
            foreach (DictionaryEntry entry in redefine.SchemaTypes) {
                redefine.Schema.SchemaTypes.Insert((XmlQualifiedName)entry.Key, (XmlSchemaObject)entry.Value);
            }
        }

        private int CountGroupSelfReference(XmlSchemaObjectCollection items, XmlQualifiedName name) {
            int count = 0;
            for (int i = 0; i < items.Count; ++i) {
                XmlSchemaGroupRef groupRef = items[i] as XmlSchemaGroupRef;
                if (groupRef != null) {
                    if (groupRef.RefName == name) {
                        if (groupRef.MinOccurs != decimal.One || groupRef.MaxOccurs != decimal.One) {
                            SendValidationEvent(Res.Sch_MinMaxGroupRedefine, groupRef);
                        }
                        count ++;
                    }
                }
                else if (items[i] is XmlSchemaGroupBase) {
                    count += CountGroupSelfReference(((XmlSchemaGroupBase) items[i]).Items, name);
                }
                if (count > 1) {
                    break;
                }
            }
            return count;

        }

        private void CheckRefinedGroup(XmlSchemaGroup group) {
            int count = 0;
            if (group.Particle != null) {
                count = CountGroupSelfReference(group.Particle.Items, group.QualifiedName);            
            }            
            if (count > 1) {
                SendValidationEvent(Res.Sch_MultipleGroupSelfRef, group);
            }
        }

        private void CheckRefinedAttributeGroup(XmlSchemaAttributeGroup attributeGroup) {
            int count = 0;
            for (int i = 0; i < attributeGroup.Attributes.Count; ++i) {
                XmlSchemaAttributeGroupRef groupRef = attributeGroup.Attributes[i] as XmlSchemaAttributeGroupRef;
                if (groupRef != null && groupRef.RefName == attributeGroup.QualifiedName) {
                    count++;
                }
            }           
            if (count > 1) {
                SendValidationEvent(Res.Sch_MultipleAttrGroupSelfRef, attributeGroup);
            }            
        }

        private void CheckRefinedSimpleType(XmlSchemaSimpleType stype) {
            if (stype.Content != null && stype.Content is XmlSchemaSimpleTypeRestriction) {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)stype.Content;
                if (restriction.BaseTypeName == stype.QualifiedName) {
                    return;
                }
            }
            SendValidationEvent(Res.Sch_InvalidTypeRedefine, stype);
        }

        private void CheckRefinedComplexType(XmlSchemaComplexType ctype) {
            if (ctype.ContentModel != null) {
                XmlQualifiedName baseName;
                if (ctype.ContentModel is XmlSchemaComplexContent) {
                    XmlSchemaComplexContent content = (XmlSchemaComplexContent)ctype.ContentModel;
                    if (content.Content is XmlSchemaComplexContentRestriction) {
                        baseName = ((XmlSchemaComplexContentRestriction)content.Content).BaseTypeName;
                    }
                    else {
                        baseName = ((XmlSchemaComplexContentExtension)content.Content).BaseTypeName;
                    }
                }
                else {
                    XmlSchemaSimpleContent content = (XmlSchemaSimpleContent)ctype.ContentModel;
                    if (content.Content is XmlSchemaSimpleContentRestriction) {
                        baseName = ((XmlSchemaSimpleContentRestriction)content.Content).BaseTypeName;
                    }
                    else {
                        baseName = ((XmlSchemaSimpleContentExtension)content.Content).BaseTypeName;
                    }
                }
                if (baseName == ctype.QualifiedName) {
                    return;
                }
            }
            SendValidationEvent(Res.Sch_InvalidTypeRedefine, ctype);            
        }

        private void PreprocessAttribute(XmlSchemaAttribute attribute) {
            if (attribute.Name != null) { 
                ValidateNameAttribute(attribute);
                attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, this.targetNamespace));
            } 
            else {
                SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", attribute);
            }
            if (attribute.Use != XmlSchemaUse.None) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "use", attribute);
            }
            if (attribute.Form != XmlSchemaForm.None) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "form", attribute);
            }
            PreprocessAttributeContent(attribute);
            ValidateIdAttribute(attribute);
        }

        private void PreprocessLocalAttribute(XmlSchemaAttribute attribute) {
            if (attribute.Name != null) { // name
                ValidateNameAttribute(attribute);
                PreprocessAttributeContent(attribute);
                attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, (attribute.Form == XmlSchemaForm.Qualified || (attribute.Form == XmlSchemaForm.None && this.attributeFormDefault == XmlSchemaForm.Qualified)) ? this.targetNamespace : null));
            } 
            else { // ref
                PreprocessAnnotation(attribute); //set parent of annotation child of ref
                if (attribute.RefName.IsEmpty) {
                    SendValidationEvent(Res.Sch_AttributeNameRef, "???", attribute);
                }
                else {
                    ValidateQNameAttribute(attribute, "ref", attribute.RefName);
                }
                if (!attribute.SchemaTypeName.IsEmpty || 
                    attribute.SchemaType != null || 
                    attribute.Form != XmlSchemaForm.None /*||
                    attribute.DefaultValue != null ||
                    attribute.FixedValue != null*/
                ) {
                    SendValidationEvent(Res.Sch_InvalidAttributeRef, attribute);
                }
                attribute.SetQualifiedName(attribute.RefName);
            }
            ValidateIdAttribute(attribute);
        }

        private void PreprocessAttributeContent(XmlSchemaAttribute attribute) {
            PreprocessAnnotation(attribute);
            if (schema.TargetNamespace == XmlReservedNs.NsXsi) {
                SendValidationEvent(Res.Sch_TargetNamespaceXsi, attribute);
            }
            if (!attribute.RefName.IsEmpty) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "ref", attribute);
            } 
            if (attribute.DefaultValue != null && attribute.FixedValue != null) {
                SendValidationEvent(Res.Sch_DefaultFixedAttributes, attribute);
            }
            if (attribute.DefaultValue != null && attribute.Use != XmlSchemaUse.Optional && attribute.Use != XmlSchemaUse.None) {
                SendValidationEvent(Res.Sch_OptionalDefaultAttribute, attribute);
            }
            if (attribute.Name == Xmlns) {
                SendValidationEvent(Res.Sch_XmlNsAttribute, attribute);
            }
            if (attribute.SchemaType != null) {
                SetParent(attribute.SchemaType, attribute);
                if (!attribute.SchemaTypeName.IsEmpty) {
                    SendValidationEvent(Res.Sch_TypeMutualExclusive, attribute);
                } 
                PreprocessSimpleType(attribute.SchemaType, true);
            }
            if (!attribute.SchemaTypeName.IsEmpty) {
                ValidateQNameAttribute(attribute, "type", attribute.SchemaTypeName);
            } 
        }

        private void PreprocessAttributeGroup(XmlSchemaAttributeGroup attributeGroup) {
            if (attributeGroup.Name != null) { 
                ValidateNameAttribute(attributeGroup);
                attributeGroup.SetQualifiedName(new XmlQualifiedName(attributeGroup.Name, this.targetNamespace));
            }
            else {
                SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", attributeGroup);
            }
            PreprocessAttributes(attributeGroup.Attributes, attributeGroup.AnyAttribute, attributeGroup);
            PreprocessAnnotation(attributeGroup);
            ValidateIdAttribute(attributeGroup);
        }

        private void PreprocessElement(XmlSchemaElement element) {
            if (element.Name != null) {
                ValidateNameAttribute(element);
                element.SetQualifiedName(new XmlQualifiedName(element.Name, this.targetNamespace));
            }
            else {
                SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", element);
            }
            PreprocessElementContent(element);

            if (element.Final == XmlSchemaDerivationMethod.All) {
                element.SetFinalResolved(XmlSchemaDerivationMethod.All);
            }
            else if (element.Final == XmlSchemaDerivationMethod.None) {
                if (this.finalDefault == XmlSchemaDerivationMethod.All) {
                    element.SetFinalResolved(XmlSchemaDerivationMethod.All);
                }
                else {
                    element.SetFinalResolved(this.finalDefault & elementFinalAllowed);
                }
            }
            else {
                if ((element.Final & ~elementFinalAllowed) != 0) {
                    SendValidationEvent(Res.Sch_InvalidElementFinalValue, element);
                }
                element.SetFinalResolved(element.Final & elementFinalAllowed);
            }
            if (element.Form != XmlSchemaForm.None) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "form", element);
            }
            if (element.MinOccursString != null) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "minOccurs", element);
            }
            if (element.MaxOccursString != null) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "maxOccurs", element);
            }
            if (!element.SubstitutionGroup.IsEmpty) {
                ValidateQNameAttribute(element, "type", element.SubstitutionGroup);
            }
            ValidateIdAttribute(element);
        }

        private void PreprocessLocalElement(XmlSchemaElement element) {
            if (element.Name != null) { // name
                ValidateNameAttribute(element);
                PreprocessElementContent(element);
                element.SetQualifiedName(new XmlQualifiedName(element.Name, (element.Form == XmlSchemaForm.Qualified || (element.Form == XmlSchemaForm.None && this.elementFormDefault == XmlSchemaForm.Qualified))? this.targetNamespace : null));
            } 
            else { // ref
                PreprocessAnnotation(element); //Check annotation child for ref and set parent 
                if (element.RefName.IsEmpty) {
                    SendValidationEvent(Res.Sch_ElementNameRef, element);
                }
                else {
                    ValidateQNameAttribute(element, "ref", element.RefName);
                }
                if (!element.SchemaTypeName.IsEmpty || 
                    element.IsAbstract ||
                    element.Block != XmlSchemaDerivationMethod.None ||
                    element.SchemaType != null ||
                    element.HasConstraints ||
                    element.DefaultValue != null ||
                    element.Form != XmlSchemaForm.None ||
                    element.FixedValue != null ||
                    element.HasNillableAttribute) {
                    SendValidationEvent(Res.Sch_InvalidElementRef, element);
                }
                if (element.DefaultValue != null && element.FixedValue != null) {     
                    SendValidationEvent(Res.Sch_DefaultFixedAttributes, element);
                }
                element.SetQualifiedName(element.RefName);
            }
            if (element.MinOccurs > element.MaxOccurs) {
                element.MinOccurs = decimal.Zero;
                SendValidationEvent(Res.Sch_MinGtMax, element);
            }
            if(element.IsAbstract) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "abstract", element);
            }
            if (element.Final != XmlSchemaDerivationMethod.None) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "final", element);
            }
            if (!element.SubstitutionGroup.IsEmpty) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "substitutionGroup", element);
            }
            ValidateIdAttribute(element);
        }

        private void PreprocessElementContent(XmlSchemaElement element) {
            PreprocessAnnotation(element); //Set parent for Annotation child of element
            if (!element.RefName.IsEmpty) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "ref", element);
            } 
            if (element.Block == XmlSchemaDerivationMethod.All) {
                element.SetBlockResolved(XmlSchemaDerivationMethod.All);
            }
            else if (element.Block == XmlSchemaDerivationMethod.None) {
                if (this.blockDefault == XmlSchemaDerivationMethod.All) {
                    element.SetBlockResolved(XmlSchemaDerivationMethod.All);
                }
                else {
                    element.SetBlockResolved(this.blockDefault & elementBlockAllowed);
                }
            }
            else {
                if ((element.Block & ~elementBlockAllowed) != 0) {
                    SendValidationEvent(Res.Sch_InvalidElementBlockValue, element);
                }
                element.SetBlockResolved(element.Block & elementBlockAllowed);
            }
            if (element.SchemaType != null) {
                SetParent(element.SchemaType, element); //Set parent for simple / complex type child of element
                if (!element.SchemaTypeName.IsEmpty) {
                    SendValidationEvent(Res.Sch_TypeMutualExclusive, element);
                } 
                if (element.SchemaType is XmlSchemaComplexType) {
                    PreprocessComplexType((XmlSchemaComplexType)element.SchemaType, true);
                } 
                else {
                    PreprocessSimpleType((XmlSchemaSimpleType)element.SchemaType, true);
                }
            }
            if (!element.SchemaTypeName.IsEmpty) {
                ValidateQNameAttribute(element, "type", element.SchemaTypeName);
            } 
            if (element.DefaultValue != null && element.FixedValue != null) {
                SendValidationEvent(Res.Sch_DefaultFixedAttributes, element);
            }
            for (int i = 0; i < element.Constraints.Count; ++i) {
                SetParent(element.Constraints[i], element);
                PreprocessIdentityConstraint((XmlSchemaIdentityConstraint)element.Constraints[i]);
            }
        }

        private void PreprocessIdentityConstraint(XmlSchemaIdentityConstraint constraint) {
            bool valid = true;
            PreprocessAnnotation(constraint); //Set parent of annotation child of key/keyref/unique
            if (constraint.Name != null) {
                ValidateNameAttribute(constraint);
                constraint.SetQualifiedName(new XmlQualifiedName(constraint.Name, this.targetNamespace));
            }
            else {
                SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", constraint);
                valid = false;
            }

            if (this.schema.IdentityConstraints[constraint.QualifiedName] != null) {
                SendValidationEvent(Res.Sch_DupIdentityConstraint, constraint.QualifiedName.ToString(), constraint);
                valid = false;
            }
            else {
                this.schema.IdentityConstraints.Add(constraint.QualifiedName, constraint);
            }

            if (constraint.Selector == null) {
                SendValidationEvent(Res.Sch_IdConstraintNoSelector, constraint);
                valid = false;
            }
            if (constraint.Fields.Count == 0) {
                SendValidationEvent(Res.Sch_IdConstraintNoFields, constraint);
                valid = false;
            }
            if (constraint is XmlSchemaKeyref) {
                XmlSchemaKeyref keyref = (XmlSchemaKeyref)constraint;
                if (keyref.Refer.IsEmpty) {
                    SendValidationEvent(Res.Sch_IdConstraintNoRefer, constraint);
                    valid = false;
                }
                else {
                    ValidateQNameAttribute(keyref, "refer", keyref.Refer);
                }
            }
            if (valid) {
                ValidateIdAttribute(constraint);
                ValidateIdAttribute(constraint.Selector);
                SetParent(constraint.Selector, constraint);
                for (int i = 0; i < constraint.Fields.Count; ++i) {
                    SetParent(constraint.Fields[i], constraint);
                    ValidateIdAttribute(constraint.Fields[i]);
                }
            }
        }

        private void PreprocessSimpleType(XmlSchemaSimpleType simpleType, bool local) {
            if (local) {
                if (simpleType.Name != null) {
                    SendValidationEvent(Res.Sch_ForbiddenAttribute, "name", simpleType);
                }
            }
            else {
                if (simpleType.Name != null) {
                    ValidateNameAttribute(simpleType);
                    simpleType.SetQualifiedName(new XmlQualifiedName(simpleType.Name, this.targetNamespace));
                }
                else {
                    SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", simpleType);
                }

                if (simpleType.Final == XmlSchemaDerivationMethod.All) {
                    simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                }
                else if (simpleType.Final == XmlSchemaDerivationMethod.None) {
                    if (this.finalDefault == XmlSchemaDerivationMethod.All) {
                        simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                    }
                    else {
                        simpleType.SetFinalResolved(this.finalDefault & simpleTypeFinalAllowed);
                    }
                }
                else {
                    if ((simpleType.Final & ~simpleTypeFinalAllowed) != 0) {
                        SendValidationEvent(Res.Sch_InvalidSimpleTypeFinalValue, simpleType);
                    }
                    simpleType.SetFinalResolved(simpleType.Final & simpleTypeFinalAllowed);
                }
            }

            if (simpleType.Content == null) {
                SendValidationEvent(Res.Sch_NoSimpleTypeContent, simpleType);
            } 
            else if (simpleType.Content is XmlSchemaSimpleTypeRestriction) {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
                //SetParent
                SetParent(restriction, simpleType);
                for (int i = 0; i < restriction.Facets.Count; ++i) {
                    SetParent(restriction.Facets[i], restriction);
                }

                if (restriction.BaseType != null) {
                    if (!restriction.BaseTypeName.IsEmpty) {
                        SendValidationEvent(Res.Sch_SimpleTypeRestRefBase, restriction);
                    }
                    PreprocessSimpleType(restriction.BaseType, true);
                } 
                else {
                    if (restriction.BaseTypeName.IsEmpty) {
                        SendValidationEvent(Res.Sch_SimpleTypeRestRefBaseNone, restriction);
                    }
                    else {
                        ValidateQNameAttribute(restriction, "base", restriction.BaseTypeName);
                    }
                }
                PreprocessAnnotation(restriction); //set parent of annotation child of simple type restriction
                ValidateIdAttribute(restriction);
            } 
            else if (simpleType.Content is XmlSchemaSimpleTypeList) {
                XmlSchemaSimpleTypeList list = (XmlSchemaSimpleTypeList)simpleType.Content;
                SetParent(list, simpleType);

                if (list.ItemType != null) {
                    if (!list.ItemTypeName.IsEmpty) {
                        SendValidationEvent(Res.Sch_SimpleTypeListRefBase, list);
                    }
                    SetParent(list.ItemType, list);
                    PreprocessSimpleType(list.ItemType, true);
                } 
                else {
                    if (list.ItemTypeName.IsEmpty) {
                        SendValidationEvent(Res.Sch_SimpleTypeListRefBaseNone, list);
                    }
                    else {
                        ValidateQNameAttribute(list, "itemType", list.ItemTypeName);
                    }
                }
                PreprocessAnnotation(list); //set parent of annotation child of simple type list
                ValidateIdAttribute(list);
            } 
            else { // union
                XmlSchemaSimpleTypeUnion union1 = (XmlSchemaSimpleTypeUnion)simpleType.Content;
                SetParent(union1, simpleType);

                int baseTypeCount = union1.BaseTypes.Count;
                if (union1.MemberTypes != null) {
                    baseTypeCount += union1.MemberTypes.Length;
                    for (int i = 0; i < union1.MemberTypes.Length; ++i) {
                        ValidateQNameAttribute(union1, "memberTypes", union1.MemberTypes[i]);
                    }
                }
                if (baseTypeCount == 0) {
                    SendValidationEvent(Res.Sch_SimpleTypeUnionNoBase, union1);
                }
                for (int i = 0; i < union1.BaseTypes.Count; ++i) {
                    SetParent(union1.BaseTypes[i], union1);
                    PreprocessSimpleType((XmlSchemaSimpleType)union1.BaseTypes[i], true);
                }
                PreprocessAnnotation(union1); //set parent of annotation child of simple type union
                ValidateIdAttribute(union1);
            }
            ValidateIdAttribute(simpleType);
        }

        private void PreprocessComplexType(XmlSchemaComplexType complexType, bool local) {
            if (local) {
                if (complexType.Name != null) {
                    SendValidationEvent(Res.Sch_ForbiddenAttribute, "name", complexType);
                }
            }
            else {
                if (complexType.Name != null) {
                    ValidateNameAttribute(complexType);
                    complexType.SetQualifiedName(new XmlQualifiedName(complexType.Name, this.targetNamespace));
                }
                else {
                    SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", complexType);
                }
                if (complexType.Block == XmlSchemaDerivationMethod.All) {
                    complexType.SetBlockResolved(XmlSchemaDerivationMethod.All);
                }
                else if (complexType.Block == XmlSchemaDerivationMethod.None) {
                    complexType.SetBlockResolved(this.blockDefault & complexTypeBlockAllowed);
                }
                else {
                    if ((complexType.Block & ~complexTypeBlockAllowed) != 0) {
                        SendValidationEvent(Res.Sch_InvalidComplexTypeBlockValue, complexType);
                    }
                    complexType.SetBlockResolved(complexType.Block & complexTypeBlockAllowed);
                }
                if (complexType.Final == XmlSchemaDerivationMethod.All) {
                    complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                }
                else if (complexType.Final == XmlSchemaDerivationMethod.None) {
                    if (this.finalDefault == XmlSchemaDerivationMethod.All) {
                        complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
                    }
                    else {
                        complexType.SetFinalResolved(this.finalDefault & complexTypeFinalAllowed);
                    }
                }
                else {
                    if ((complexType.Final & ~complexTypeFinalAllowed) != 0) {
                        SendValidationEvent(Res.Sch_InvalidComplexTypeFinalValue, complexType);
                    }
                    complexType.SetFinalResolved(complexType.Final & complexTypeFinalAllowed);
                }

            }

            if (complexType.ContentModel != null) {
                SetParent(complexType.ContentModel, complexType); //SimpleContent / complexCotent
                PreprocessAnnotation(complexType.ContentModel);

                if (complexType.Particle != null || complexType.Attributes != null) {
                    // this is illigal
                }
                if (complexType.ContentModel is XmlSchemaSimpleContent) {
                    XmlSchemaSimpleContent content = (XmlSchemaSimpleContent)complexType.ContentModel;
                    if (content.Content == null) {
                        if (complexType.QualifiedName == XmlQualifiedName.Empty) {
                            SendValidationEvent(Res.Sch_NoRestOrExt, complexType);
                        }
                        else {
                            SendValidationEvent(Res.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
                        }
                    } 
                    else {
                        SetParent(content.Content, content);   //simplecontent extension / restriction
                        PreprocessAnnotation(content.Content); //annotation child of simple extension / restriction

                        if (content.Content is XmlSchemaSimpleContentExtension) {
                            XmlSchemaSimpleContentExtension contentExtension = (XmlSchemaSimpleContentExtension)content.Content;
                            if (contentExtension.BaseTypeName.IsEmpty) {
                                SendValidationEvent(Res.Sch_MissAttribute, "base", contentExtension);
                            }
                            else {
                                ValidateQNameAttribute(contentExtension, "base", contentExtension.BaseTypeName);
                            }
                            PreprocessAttributes(contentExtension.Attributes, contentExtension.AnyAttribute, contentExtension);
                            ValidateIdAttribute(contentExtension);
                        } 
                        else { //XmlSchemaSimpleContentRestriction
                            XmlSchemaSimpleContentRestriction contentRestriction = (XmlSchemaSimpleContentRestriction)content.Content;
                            if (contentRestriction.BaseTypeName.IsEmpty) {
                                SendValidationEvent(Res.Sch_MissAttribute, "base", contentRestriction);
                            }
                            else {
                                ValidateQNameAttribute(contentRestriction, "base", contentRestriction.BaseTypeName);
                            }
                            if (contentRestriction.BaseType != null) {
                                SetParent(contentRestriction.BaseType, contentRestriction);
                                PreprocessSimpleType(contentRestriction.BaseType, true);
                            } 
                            PreprocessAttributes(contentRestriction.Attributes, contentRestriction.AnyAttribute, contentRestriction);
                            ValidateIdAttribute(contentRestriction);
                        }
                    }
                    ValidateIdAttribute(content);
                } 
                else { // XmlSchemaComplexContent
                    XmlSchemaComplexContent content = (XmlSchemaComplexContent)complexType.ContentModel;
                    if (content.Content == null) {
                        if (complexType.QualifiedName == XmlQualifiedName.Empty) {
                            SendValidationEvent(Res.Sch_NoRestOrExt, complexType);
                        }
                        else {
                            SendValidationEvent(Res.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
                        }
                    } 
                    else {
                        if ( !content.HasMixedAttribute && complexType.IsMixed) {
                            content.IsMixed = true; // fixup
                        }
                        SetParent(content.Content, content);   //complexcontent extension / restriction
                        PreprocessAnnotation(content.Content); //Annotation child of extension / restriction

                        if (content.Content is XmlSchemaComplexContentExtension) {
                            XmlSchemaComplexContentExtension contentExtension = (XmlSchemaComplexContentExtension)content.Content;
                            if (contentExtension.BaseTypeName.IsEmpty) {
                                SendValidationEvent(Res.Sch_MissAttribute, "base", contentExtension);
                            }
                            else {
                                ValidateQNameAttribute(contentExtension, "base", contentExtension.BaseTypeName);
                            }
                            if (contentExtension.Particle != null) {
                                SetParent(contentExtension.Particle, contentExtension); //Group / all / choice / sequence
                                PreprocessParticle(contentExtension.Particle);
                            }
                            PreprocessAttributes(contentExtension.Attributes, contentExtension.AnyAttribute, contentExtension);
                            ValidateIdAttribute(contentExtension);
                        } 
                        else { // XmlSchemaComplexContentRestriction
                            XmlSchemaComplexContentRestriction contentRestriction = (XmlSchemaComplexContentRestriction)content.Content;
                            if (contentRestriction.BaseTypeName.IsEmpty) {
                                SendValidationEvent(Res.Sch_MissAttribute, "base", contentRestriction);
                            }
                            else {
                                ValidateQNameAttribute(contentRestriction, "base", contentRestriction.BaseTypeName);
                            }
                            if (contentRestriction.Particle != null) {
                                SetParent(contentRestriction.Particle, contentRestriction); //Group / all / choice / sequence
                                PreprocessParticle(contentRestriction.Particle);
                            }
                            PreprocessAttributes(contentRestriction.Attributes, contentRestriction.AnyAttribute, contentRestriction);
                            ValidateIdAttribute(contentRestriction);
                        }
                        ValidateIdAttribute(content);
                    }
                }
            } 
            else {
                if (complexType.Particle != null) {
                    SetParent(complexType.Particle, complexType);
                    PreprocessParticle(complexType.Particle);
                }
                PreprocessAttributes(complexType.Attributes, complexType.AnyAttribute, complexType);
            }
            ValidateIdAttribute(complexType);
        }

        private void PreprocessGroup(XmlSchemaGroup group) {
            if (group.Name != null) { 
                ValidateNameAttribute(group);
                group.SetQualifiedName(new XmlQualifiedName(group.Name, this.targetNamespace));
            }
            else {
                SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", group);
            }
            if (group.Particle == null) {
                SendValidationEvent(Res.Sch_NoGroupParticle, group);
                return;
            }
            if (group.Particle.MinOccursString != null) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "minOccurs", group.Particle);
            }
            if (group.Particle.MaxOccursString != null) {
                SendValidationEvent(Res.Sch_ForbiddenAttribute, "maxOccurs", group.Particle);
            }

            PreprocessParticle(group.Particle);
            PreprocessAnnotation(group); //Set parent of annotation child of group
            ValidateIdAttribute(group);
        }

        private void PreprocessNotation(XmlSchemaNotation notation) {
            if (notation.Name != null) { 
                ValidateNameAttribute(notation);
                notation.QualifiedName = new XmlQualifiedName(notation.Name, this.targetNamespace);
            }
            else {
                SendValidationEvent(Res.Sch_MissRequiredAttribute, "name", notation);
            }
            if (notation.Public != null) { 
                try {
                    XmlConvert.ToUri(notation.Public); // can throw
                } 
                catch {
                    SendValidationEvent(Res.Sch_InvalidPublicAttribute, notation.Public, notation);
                }
            }
            else {
                SendValidationEvent(Res.Sch_MissRequiredAttribute, "public", notation);
            }
            if (notation.System != null) {
                try {
                    XmlConvert.ToUri(notation.System); // can throw
                } 
                catch {
                    SendValidationEvent(Res.Sch_InvalidSystemAttribute, notation.System, notation);
                }    
            }
            PreprocessAnnotation(notation); //Set parent of annotation child of notation
            ValidateIdAttribute(notation);
        }

        
        private void PreprocessParticle(XmlSchemaParticle particle) {
            XmlSchemaAll schemaAll = particle as XmlSchemaAll;
            if (schemaAll != null) {
                if (particle.MinOccurs != decimal.Zero && particle.MinOccurs != decimal.One) {
                    particle.MinOccurs = decimal.One;
                    SendValidationEvent(Res.Sch_InvalidAllMin, particle);
                }
                if (particle.MaxOccurs != decimal.One) {
                    particle.MaxOccurs = decimal.One;
                    SendValidationEvent(Res.Sch_InvalidAllMax, particle);
                }
                for (int i = 0; i < schemaAll.Items.Count; ++i) {
                    XmlSchemaElement element = (XmlSchemaElement)schemaAll.Items[i];
                    if (element.MaxOccurs != decimal.Zero && element.MaxOccurs != decimal.One) {
                        element.MaxOccurs = decimal.One;
                        SendValidationEvent(Res.Sch_InvalidAllElementMax, element);
                    }
                    SetParent(element, particle);
                    PreprocessLocalElement(element);
                }
            } 
            else {
                if (particle.MinOccurs > particle.MaxOccurs) {
                    particle.MinOccurs = particle.MaxOccurs;
                    SendValidationEvent(Res.Sch_MinGtMax, particle);
                }
                XmlSchemaChoice choice = particle as XmlSchemaChoice;
                if (choice != null) {
                    XmlSchemaObjectCollection choices = choice.Items;
                    for (int i = 0; i < choices.Count; ++i) {
                        SetParent(choices[i], particle);
                        XmlSchemaElement element = choices[i] as XmlSchemaElement;
                        if (element != null) {
                            PreprocessLocalElement(element);
                        } 
                        else {
                            PreprocessParticle((XmlSchemaParticle)choices[i]);
                        }
                    }
                } 
                else if (particle is XmlSchemaSequence) {
                    XmlSchemaObjectCollection sequences = ((XmlSchemaSequence) particle).Items;
                    for (int i = 0; i < sequences.Count; ++i) {
                        SetParent(sequences[i], particle);
                        XmlSchemaElement element = sequences[i] as XmlSchemaElement;
                        if (element != null) {
                            PreprocessLocalElement(element);
                        } 
                        else {
                            PreprocessParticle((XmlSchemaParticle)sequences[i]);
                        }
                    }
                } 
                else if (particle is XmlSchemaGroupRef) {
                    XmlSchemaGroupRef groupRef = (XmlSchemaGroupRef)particle;
                    if (groupRef.RefName.IsEmpty) {
                        SendValidationEvent(Res.Sch_MissAttribute, "ref", groupRef);
                    }
                    else {
                        ValidateQNameAttribute(groupRef, "ref", groupRef.RefName);
                    }
                } 
                else if (particle is XmlSchemaAny) {
                    try {
                        ((XmlSchemaAny)particle).BuildNamespaceListV1Compat(this.targetNamespace);
                    } 
                    catch {
                        SendValidationEvent(Res.Sch_InvalidAny, particle);
                    }
                }
            }
            PreprocessAnnotation(particle); //set parent of annotation child of group / all/ choice / sequence
            ValidateIdAttribute(particle);
        }

        private void PreprocessAttributes(XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute anyAttribute, XmlSchemaObject parent) {
            for (int i = 0; i < attributes.Count; ++i) {
                SetParent(attributes[i], parent);
                XmlSchemaAttribute attribute = attributes[i] as XmlSchemaAttribute;
                if (attribute != null) {
                    PreprocessLocalAttribute(attribute);
                } 
                else { // XmlSchemaAttributeGroupRef
                    XmlSchemaAttributeGroupRef attributeGroupRef = (XmlSchemaAttributeGroupRef)attributes[i];
                    if (attributeGroupRef.RefName.IsEmpty) {
                        SendValidationEvent(Res.Sch_MissAttribute, "ref", attributeGroupRef);
                    }
                    else {
                        ValidateQNameAttribute(attributeGroupRef, "ref", attributeGroupRef.RefName);
                    }
                    PreprocessAnnotation(attributes[i]); //set parent of annotation child of attributeGroupRef
                    ValidateIdAttribute(attributes[i]);
                }
            }
            if (anyAttribute != null) {
                try {
                    SetParent(anyAttribute, parent);
                    PreprocessAnnotation(anyAttribute); //set parent of annotation child of any attribute
                    anyAttribute.BuildNamespaceListV1Compat(this.targetNamespace);
                } 
                catch {
                    SendValidationEvent(Res.Sch_InvalidAnyAttribute, anyAttribute);
                }
                ValidateIdAttribute(anyAttribute);
            }
        }

        private void ValidateIdAttribute(XmlSchemaObject xso) {
            if (xso.IdAttribute != null) {
                try {
                    xso.IdAttribute = NameTable.Add(XmlConvert.VerifyNCName(xso.IdAttribute));
                    if (this.schema.Ids[xso.IdAttribute] != null) {
                        SendValidationEvent(Res.Sch_DupIdAttribute, xso);
                    }
                    else {
                        this.schema.Ids.Add(xso.IdAttribute, xso);
                    }
                } 
                catch (Exception ex){
                    SendValidationEvent(Res.Sch_InvalidIdAttribute, ex.Message, xso);
                }
            }
        }

        private void ValidateNameAttribute(XmlSchemaObject xso) {
            string name = xso.NameAttribute;
            if (name == null || name.Length == 0) {
                SendValidationEvent(Res.Sch_InvalidNameAttributeEx, null, Res.GetString(Res.Sch_NullValue), xso);
            }
            //Normalize whitespace since NCName has whitespace facet="collapse"
            name = XmlComplianceUtil.NonCDataNormalize(name);
            int len = ValidateNames.ParseNCName(name, 0);
            if (len != name.Length) { // If the string is not a valid NCName, then throw or return false
                string[] invCharArgs = XmlException.BuildCharExceptionArgs(name, len);
                string innerStr = Res.GetString(Res.Xml_BadNameCharWithPos, invCharArgs[0], invCharArgs[1], len);
                SendValidationEvent(Res.Sch_InvalidNameAttributeEx, name, innerStr, xso);
            }
            else {
                xso.NameAttribute = NameTable.Add(name);
            }
        }

        private void ValidateQNameAttribute(XmlSchemaObject xso, string attributeName, XmlQualifiedName value) {
            try {
                value.Verify();
                value.Atomize(NameTable);
                if(referenceNamespaces[value.Namespace] == null) {
                    SendValidationEvent(Res.Sch_UnrefNS,value.Namespace,xso, XmlSeverityType.Warning);
                }
            } 
            catch (Exception ex){
                SendValidationEvent(Res.Sch_InvalidAttribute, attributeName, ex.Message, xso);
            }
        }

        private void SetParent(XmlSchemaObject child, XmlSchemaObject parent) {
            child.Parent = parent;
        }

        private void PreprocessAnnotation(XmlSchemaObject schemaObject) {
            XmlSchemaAnnotated annotated = schemaObject as XmlSchemaAnnotated;
            if (annotated != null) {
                if (annotated.Annotation != null) {
                    annotated.Annotation.Parent = schemaObject;
                    for (int i = 0; i < annotated.Annotation.Items.Count; ++i) {
                        annotated.Annotation.Items[i].Parent = annotated.Annotation; //Can be documentation or appInfo
                    }
                }

            }
        }

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        private Uri ResolveSchemaLocationUri(XmlSchema enclosingSchema, string location) {
            try {
                return xmlResolver.ResolveUri(enclosingSchema.BaseUri, location);
            }
            catch {
                return null;
            }
        }

        private Stream GetSchemaEntity(Uri ruri) {
            try {
                return (Stream)xmlResolver.GetEntity(ruri, null, null);
            }
            catch {
                return null;
            }
        }

    };
#pragma warning restore 618

}  // namespace System.Xml
