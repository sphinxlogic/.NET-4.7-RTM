//------------------------------------------------------------------------------
// <copyright file="XsltCompileContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml.Xsl.XsltOld {
    using Res = System.Xml.Utils.Res;
    using System.Diagnostics;
    using System.IO;
    using System.Globalization;
    using System.Collections;
    using System.Xml.XPath;
    using System.Xml.Xsl.Runtime;
    using MS.Internal.Xml.XPath;
    using System.Reflection;
    using System.Security;
    using System.Runtime.Versioning;

    internal class XsltCompileContext : XsltContext {
        private InputScopeManager manager;
        private Processor         processor;

        // storage for the functions
        private static Hashtable            s_FunctionTable = CreateFunctionTable();
        private static IXsltContextFunction s_FuncNodeSet   = new FuncNodeSet();
        const          string               f_NodeSet       = "node-set";

        internal XsltCompileContext(InputScopeManager manager, Processor processor) : base(/*dummy*/false) {
            this.manager   = manager;
            this.processor = processor;
        }

        internal XsltCompileContext() : base(/*dummy*/ false ) {}

        internal void Recycle() {
            manager = null;
            processor = null;
        }

        internal void Reinitialize(InputScopeManager manager, Processor processor) {
            this.manager = manager;
            this.processor = processor;
        }

        public override int CompareDocument(string baseUri, string nextbaseUri) {
            return String.Compare(baseUri, nextbaseUri, StringComparison.Ordinal);
        }

        // Namespace support
        public override string DefaultNamespace {
            get { return string.Empty; }
        }

        public override string LookupNamespace(string prefix) {
            return this.manager.ResolveXPathNamespace(prefix);
        }

        // --------------------------- XsltContext -------------------
        //                Resolving variables and functions

        public override IXsltContextVariable ResolveVariable(string prefix, string name) {
            string namespaceURI = this.LookupNamespace(prefix);
            XmlQualifiedName qname = new XmlQualifiedName(name, namespaceURI);
            IXsltContextVariable variable = this.manager.VariableScope.ResolveVariable(qname);
            if (variable == null) {
                throw XsltException.Create(Res.Xslt_InvalidVariable, qname.ToString());
            }
            return variable;
        }

        internal object EvaluateVariable(VariableAction variable) {
            Object result = this.processor.GetVariableValue(variable);
            if (result == null && ! variable.IsGlobal) {
                // This was uninitialized local variable. May be we have sutable global var too?
                VariableAction global = this.manager.VariableScope.ResolveGlobalVariable(variable.Name);
                if (global != null) {
                    result = this.processor.GetVariableValue(global);
                }
            }
            if (result == null) {
                throw XsltException.Create(Res.Xslt_InvalidVariable, variable.Name.ToString());
            }
            return result;
        }

        // Whitespace stripping support
        public override bool Whitespace {
            get { return this.processor.Stylesheet.Whitespace; }
        }

        public override bool PreserveWhitespace(XPathNavigator node) {
            node = node.Clone();
            node.MoveToParent();
            return this.processor.Stylesheet.PreserveWhiteSpace(this.processor, node);
        }

        private MethodInfo FindBestMethod(MethodInfo[] methods, bool ignoreCase, bool publicOnly, string name, XPathResultType[] argTypes) {
            int length = methods.Length;
            int free   = 0;
            // restrict search to methods with the same name and requiested protection attribute
            for(int i = 0; i < length; i ++) {
                if(string.Compare(name, methods[i].Name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0) {
                    if(! publicOnly || methods[i].GetBaseDefinition().IsPublic) {
                        methods[free ++] = methods[i];
                    }
                }
            }
            length = free;
            if(length == 0) {
                // this is the only place we returning null in this function
                return null;
            }
            if(argTypes == null) {
                // without arg types we can't do more detailed search
                return methods[0];
            }
            // restrict search by number of parameters
            free = 0;
            for(int i = 0; i < length; i ++) {
                if(methods[i].GetParameters().Length == argTypes.Length) {
                    methods[free ++] = methods[i];
                }
            }
            length = free;
            if(length <= 1) {
                // 0 -- not method found. We have to return non-null and let it fail with corect exception on call.
                // 1 -- no reason to continue search anyway.
                return methods[0];
            }
            // restrict search by parameters type
            free = 0;
            for(int i = 0; i < length; i ++) {
                bool match = true;
                ParameterInfo[] parameters = methods[i].GetParameters();
                for(int par = 0; par < parameters.Length; par ++) {
                    XPathResultType required = argTypes[par];
                    if(required == XPathResultType.Any) {
                        continue;                        // Any means we don't know type and can't discriminate by it
                    }
                    XPathResultType actual = GetXPathType(parameters[par].ParameterType);
                    if(
                        actual != required &&
                        actual != XPathResultType.Any   // actual arg is object and we can pass everithing here.
                    ) {
                        match = false;
                        break;
                    }
                }
                if(match) {
                    methods[free ++] = methods[i];
                }
            }
            length = free;
            return methods[0];
        }

        private const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        private IXsltContextFunction GetExtentionMethod(string ns, string name, XPathResultType[] argTypes, out object extension) {
            FuncExtension result = null;
            extension = this.processor.GetScriptObject(ns);
            if (extension != null) {
                MethodInfo method = FindBestMethod(extension.GetType().GetMethods(bindingFlags), /*ignoreCase:*/true, /*publicOnly:*/false, name, argTypes);
                if (method != null) {
                    result = new FuncExtension(extension, method, /*permissions*/null);
                }
                return result;
            }

            extension = this.processor.GetExtensionObject(ns);
            if(extension != null) {
                MethodInfo method = FindBestMethod(extension.GetType().GetMethods(bindingFlags), /*ignoreCase:*/false, /*publicOnly:*/true, name, argTypes);
                if (method != null) {
                    result = new FuncExtension(extension, method, this.processor.permissions);
                }
                return result;
            }

            return null;
        }

        public override IXsltContextFunction ResolveFunction(string prefix, string name, XPathResultType[] argTypes) {
            IXsltContextFunction func = null;
            if(prefix.Length == 0) {
                func = s_FunctionTable[name] as IXsltContextFunction;
            } else {
                string ns = this.LookupNamespace(prefix);
                if (ns == XmlReservedNs.NsMsxsl && name == f_NodeSet) {
                    func = s_FuncNodeSet;
                } else {
                    object     extension;
                    func = GetExtentionMethod(ns, name, argTypes, out extension);
                    if(extension == null) {
                        throw XsltException.Create(Res.Xslt_ScriptInvalidPrefix, prefix);  // BugBug: It's better to say that method 'name' not found
                    }
                }
            }
            if (func == null) {
                throw XsltException.Create(Res.Xslt_UnknownXsltFunction, name);
            }
            if (argTypes.Length < func.Minargs || func.Maxargs < argTypes.Length) {
               throw XsltException.Create(Res.Xslt_WrongNumberArgs, name, argTypes.Length.ToString(CultureInfo.InvariantCulture));
            }
            return func;
        }

        //
        // Xslt Function Extensions to XPath
        //
        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        private Uri ComposeUri(string thisUri, string baseUri) {
            Debug.Assert(thisUri != null && baseUri != null);
            XmlResolver resolver = processor.Resolver;
            Uri uriBase = null;
            if (baseUri.Length != 0) {
                uriBase = resolver.ResolveUri(null, baseUri);
            }
            return resolver.ResolveUri(uriBase, thisUri);
        }

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        private XPathNodeIterator Document(object arg0, string baseUri) {
            if (this.processor.permissions != null) {
                this.processor.permissions.PermitOnly();
            }
            XPathNodeIterator it = arg0 as XPathNodeIterator;
            if (it != null) {
                ArrayList list = new ArrayList();
                Hashtable documents = new Hashtable();
                while (it.MoveNext()) {
                    Uri uri = ComposeUri(it.Current.Value, baseUri ?? it.Current.BaseURI);
                    if (! documents.ContainsKey(uri)) {
                        documents.Add(uri, null);
                        list.Add(this.processor.GetNavigator(uri));
                    }
                }
                return new XPathArrayIterator(list);
            } else {
                return new XPathSingletonIterator(
                    this.processor.GetNavigator(
                        ComposeUri(XmlConvert.ToXPathString(arg0), baseUri ?? this.manager.Navigator.BaseURI)
                    )
                );
            }
        }

        private Hashtable BuildKeyTable(Key key, XPathNavigator root) {
            Hashtable keyTable = new Hashtable();

            string matchStr = this.processor.GetQueryExpression(key.MatchKey);
            Query matchExpr = this.processor.GetCompiledQuery(key.MatchKey);
            Query useExpr   = this.processor.GetCompiledQuery(key.UseKey);

            XPathNodeIterator  sel = root.SelectDescendants(XPathNodeType.All, /*matchSelf:*/ false);

            while(sel.MoveNext()) {
                XPathNavigator node = sel.Current;
                EvaluateKey(node, matchExpr, matchStr, useExpr, keyTable);
                if (node.MoveToFirstAttribute()) {
                    do {
                        EvaluateKey(node, matchExpr, matchStr, useExpr, keyTable);
                    } while (node.MoveToNextAttribute());
                    node.MoveToParent();
                }
            }
            return keyTable;
        }

        private static void AddKeyValue(Hashtable keyTable, String key, XPathNavigator value, bool checkDuplicates) {
            ArrayList list = (ArrayList)keyTable[key];
            if (list == null) {
                list = new ArrayList();
                keyTable.Add(key, list);
            } else {
                Debug.Assert(
                    value.ComparePosition((XPathNavigator) list[list.Count - 1]) != XmlNodeOrder.Before,
                    "The way we traversing nodes should garantees node-order"
                );
                if (checkDuplicates) {
                    // it's posible that this value already was ----osiated with current node
                    // but if this happened the node is last in the list of values.
                    if (value.ComparePosition((XPathNavigator) list[list.Count - 1]) == XmlNodeOrder.Same) {
                        return;
                    }
                } else {
                    Debug.Assert(
                        value.ComparePosition((XPathNavigator) list[list.Count - 1]) != XmlNodeOrder.Same,
                        "checkDuplicates == false : We can't have duplicates"
                    );
                }
            }
            list.Add(value.Clone());
        }

        private static void EvaluateKey(XPathNavigator node, Query matchExpr, string matchStr, Query useExpr, Hashtable keyTable) {
            try {
                if (matchExpr.MatchNode(node) == null) {
                    return;
                }
            } catch(XPathException) {
                throw XsltException.Create(Res.Xslt_InvalidPattern, matchStr);
            }
            object result = useExpr.Evaluate(new XPathSingletonIterator(node, /*moved:*/true));
            XPathNodeIterator it = result as XPathNodeIterator;
            if (it != null) {
                bool checkDuplicates = false;
                while (it.MoveNext()) {
                    AddKeyValue(keyTable, /*key:*/it.Current.Value, /*value:*/node, checkDuplicates);
                    checkDuplicates = true;
                }
            } else {
                String key = XmlConvert.ToXPathString(result);
                AddKeyValue(keyTable, key, /*value:*/node, /*checkDuplicates:*/ false);
            }
        }

        private DecimalFormat ResolveFormatName(string formatName) {
            string ns = string.Empty, local = string.Empty;
            if (formatName != null) {
                string prefix;
                PrefixQName.ParseQualifiedName(formatName, out prefix, out local);
                ns = LookupNamespace(prefix);
            }
            DecimalFormat formatInfo = this.processor.RootAction.GetDecimalFormat(new XmlQualifiedName(local, ns));
            if (formatInfo == null) {
                if (formatName != null) {
                    throw XsltException.Create(Res.Xslt_NoDecimalFormat, formatName);
                }
                formatInfo = new DecimalFormat(new NumberFormatInfo(), '#', '0', ';');
            }
            return formatInfo;
        }

        // see http://www.w3.org/TR/xslt#function-element-available
        private bool ElementAvailable(string qname) {
            string name, prefix;
            PrefixQName.ParseQualifiedName(qname, out prefix, out name);
            string ns = this.manager.ResolveXmlNamespace(prefix);
            // msxsl:script - is not an "instruction" so we return false for it.
            if (ns == XmlReservedNs.NsXslt) {
                return (
                    name == "apply-imports" ||
                    name == "apply-templates" ||
                    name == "attribute" ||
                    name == "call-template" ||
                    name == "choose" ||
                    name == "comment" ||
                    name == "copy" ||
                    name == "copy-of" ||
                    name == "element" ||
                    name == "fallback" ||
                    name == "for-each" ||
                    name == "if" ||
                    name == "message" ||
                    name == "number" ||
                    name == "processing-instruction" ||
                    name == "text" ||
                    name == "value-of" ||
                    name == "variable"
                );
            }
            return false;
        }

        // see: http://www.w3.org/TR/xslt#function-function-available
        private bool FunctionAvailable(string qname) {
            string name, prefix;
            PrefixQName.ParseQualifiedName(qname, out prefix, out name);
            string ns = LookupNamespace(prefix);

            if (ns == XmlReservedNs.NsMsxsl) {
                return name == f_NodeSet;
            } else if(ns.Length == 0) {
                return (
                    // It'll be better to get this information from XPath
                    name == "last"              ||
                    name == "position"          ||
                    name == "name"              ||
                    name == "namespace-uri"     ||
                    name == "local-name"        ||
                    name == "count"             ||
                    name == "id"                ||
                    name == "string"            ||
                    name == "concat"            ||
                    name == "starts-with"       ||
                    name == "contains"          ||
                    name == "substring-before"  ||
                    name == "substring-after"   ||
                    name == "substring"         ||
                    name == "string-length"     ||
                    name == "normalize-space"   ||
                    name == "translate"         ||
                    name == "boolean"           ||
                    name == "not"               ||
                    name == "true"              ||
                    name == "false"             ||
                    name == "lang"              ||
                    name == "number"            ||
                    name == "sum"               ||
                    name == "floor"             ||
                    name == "ceiling"           ||
                    name == "round"             ||
                    // XSLT functions:
                    (s_FunctionTable[name] != null && name != "unparsed-entity-uri")
                );
            } else {
                // Is this script or extention function?
                object extension;
                return GetExtentionMethod(ns, name, /*argTypes*/null, out extension) != null;
            }
        }

        private XPathNodeIterator Current() {
            XPathNavigator nav = this.processor.Current;
            if (nav != null) {
                return new XPathSingletonIterator(nav.Clone());
            }
            return XPathEmptyIterator.Instance;
        }

        private String SystemProperty(string qname) {
            String result = string.Empty;

            string prefix;
            string local;
            PrefixQName.ParseQualifiedName(qname, out prefix, out local);

            // verify the prefix corresponds to the Xslt namespace
            string urn = LookupNamespace(prefix);

            if (urn == XmlReservedNs.NsXslt) {
                if(local == "version") {
                    result = "1";
                } else if(local == "vendor") {
                    result = "Microsoft";
                } else if(local == "vendor-url") {
                    result = "http://www.microsoft.com";
                }
            } else {
                if(urn == null && prefix != null) {
                // if prefix exist it has to be mapped to namespace.
                // Can it be "" here ?
                    throw XsltException.Create(Res.Xslt_InvalidPrefix, prefix);
                }
                return string.Empty;
            }

            return result;
        }

        public static XPathResultType GetXPathType(Type type) {
            switch(Type.GetTypeCode(type)) {
            case TypeCode.String :
                return XPathResultType.String;
            case TypeCode.Boolean :
                return XPathResultType.Boolean;
            case TypeCode.Object :
                if (
                    typeof(XPathNavigator ).IsAssignableFrom(type) ||
                    typeof(IXPathNavigable).IsAssignableFrom(type)
                ) {
                    return XPathResultType.Navigator;
                }
                if (typeof(XPathNodeIterator).IsAssignableFrom(type)) {
                    return XPathResultType.NodeSet;
                }
                // Microsoft: It be better to check that type is realy object and otherwise return XPathResultType.Error
                return XPathResultType.Any;
            case TypeCode.DateTime :
                return XPathResultType.Error;
            default: /* all numeric types */
                return XPathResultType.Number;
            }
        }

        // ---------------- Xslt Function Implementations -------------------
        //
        static Hashtable CreateFunctionTable() {
            Hashtable ft = new Hashtable(10); {
                ft["current"            ] = new FuncCurrent          ();
                ft["unparsed-entity-uri"] = new FuncUnEntityUri      ();
                ft["generate-id"        ] = new FuncGenerateId       ();
                ft["system-property"    ] = new FuncSystemProp       ();
                ft["element-available"  ] = new FuncElementAvailable ();
                ft["function-available" ] = new FuncFunctionAvailable();
                ft["document"           ] = new FuncDocument         ();
                ft["key"                ] = new FuncKey              ();
                ft["format-number"      ] = new FuncFormatNumber     ();
            }
            return ft;
        }

        // + IXsltContextFunction
        //   + XsltFunctionImpl             func. name,       min/max args,      return type                args types
        //       FuncCurrent            "current"              0   0         XPathResultType.NodeSet   { }
        //       FuncUnEntityUri        "unparsed-entity-uri"  1   1         XPathResultType.String    { XPathResultType.String  }
        //       FuncGenerateId         "generate-id"          0   1         XPathResultType.String    { XPathResultType.NodeSet }
        //       FuncSystemProp         "system-property"      1   1         XPathResultType.String    { XPathResultType.String  }
        //       FuncElementAvailable   "element-available"    1   1         XPathResultType.Boolean   { XPathResultType.String  }
        //       FuncFunctionAvailable  "function-available"   1   1         XPathResultType.Boolean   { XPathResultType.String  }
        //       FuncDocument           "document"             1   2         XPathResultType.NodeSet   { XPathResultType.Any    , XPathResultType.NodeSet }
        //       FuncKey                "key"                  2   2         XPathResultType.NodeSet   { XPathResultType.String , XPathResultType.Any     }
        //       FuncFormatNumber       "format-number"        2   3         XPathResultType.String    { XPathResultType.Number , XPathResultType.String, XPathResultType.String }
        //       FuncNodeSet            "msxsl:node-set"       1   1         XPathResultType.NodeSet   { XPathResultType.Navigator }
        //       FuncExtension
        //
        private abstract class XsltFunctionImpl : IXsltContextFunction {
            private int               minargs;
            private int               maxargs;
            private XPathResultType   returnType;
            private XPathResultType[] argTypes;

            public XsltFunctionImpl() {}
            public XsltFunctionImpl(int minArgs, int maxArgs, XPathResultType returnType, XPathResultType[] argTypes) {
                this.Init(minArgs, maxArgs, returnType, argTypes);
            }
            protected void Init(int minArgs, int maxArgs, XPathResultType returnType, XPathResultType[] argTypes) {
                this.minargs    = minArgs;
                this.maxargs    = maxArgs;
                this.returnType = returnType;
                this.argTypes   = argTypes;
            }

            public int               Minargs       { get { return this.minargs;    } }
            public int               Maxargs       { get { return this.maxargs;    } }
            public XPathResultType   ReturnType    { get { return this.returnType; } }
            public XPathResultType[] ArgTypes      { get { return this.argTypes;   } }
            public abstract object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext);

        // static helper methods:
            public static XPathNodeIterator ToIterator(object argument) {
                XPathNodeIterator it = argument as XPathNodeIterator;
                if(it == null) {
                    throw XsltException.Create(Res.Xslt_NoNodeSetConversion);
                }
                return it;
            }

            public static XPathNavigator ToNavigator(object argument) {
                XPathNavigator nav = argument as XPathNavigator;
                if(nav == null) {
                    throw XsltException.Create(Res.Xslt_NoNavigatorConversion);
                }
                return nav;
            }

            private static string IteratorToString(XPathNodeIterator it) {
                Debug.Assert(it != null);
                if(it.MoveNext()) {
                    return it.Current.Value;
                }
                return string.Empty;
            }

            public static string ToString(object argument) {
                XPathNodeIterator it = argument as XPathNodeIterator;
                if(it != null) {
                    return IteratorToString(it);
                } else {
                    return XmlConvert.ToXPathString(argument);
                }
            }

            public static bool ToBoolean(object argument) {
                XPathNodeIterator it = argument as XPathNodeIterator;
                if(it != null) {
                    return Convert.ToBoolean(IteratorToString(it), CultureInfo.InvariantCulture);
                }
                XPathNavigator nav = argument as XPathNavigator;
                if (nav != null) {
                    return Convert.ToBoolean(nav.ToString(), CultureInfo.InvariantCulture);
                }
                return Convert.ToBoolean(argument, CultureInfo.InvariantCulture);
            }

            public static double ToNumber(object argument) {
                XPathNodeIterator it = argument as XPathNodeIterator;
                if (it != null) {
                    return XmlConvert.ToXPathDouble(IteratorToString(it));
                }
                XPathNavigator nav = argument as XPathNavigator;
                if (nav != null) {
                    return XmlConvert.ToXPathDouble(nav.ToString());
                }
                return XmlConvert.ToXPathDouble(argument);
            }

            private static object ToNumeric(object argument, TypeCode typeCode) {
                return Convert.ChangeType(ToNumber(argument), typeCode, CultureInfo.InvariantCulture);
            }

            public static object ConvertToXPathType(object val, XPathResultType xt, TypeCode typeCode) {
                switch(xt) {
                case XPathResultType.String    :
                    // Unfortunetely XPathResultType.String == XPathResultType.Navigator (This is wrong but cant be changed in Everett)
                    // Fortunetely we have typeCode hare so let's discriminate by typeCode
                    if (typeCode == TypeCode.String) {
                        return ToString(val);
                    } else {
                        return ToNavigator(val);
                    }
                case XPathResultType.Number    : return ToNumeric(val, typeCode);
                case XPathResultType.Boolean   : return ToBoolean(val);
                case XPathResultType.NodeSet   : return ToIterator(val);
//                case XPathResultType.Navigator : return ToNavigator(val);
                case XPathResultType.Any       :
                case XPathResultType.Error     :
                    return val;
                default :
                    Debug.Assert(false, "unexpected XPath type");
                    return val;
                }
            }
        }

        private class FuncCurrent : XsltFunctionImpl {
            public FuncCurrent() : base(0, 0, XPathResultType.NodeSet, new XPathResultType[] {}) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                return ((XsltCompileContext)xsltContext).Current();
            }
        }

        private class FuncUnEntityUri : XsltFunctionImpl {
            public FuncUnEntityUri() : base(1, 1, XPathResultType.String , new XPathResultType[] { XPathResultType.String  }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                throw XsltException.Create(Res.Xslt_UnsuppFunction, "unparsed-entity-uri");
            }
        }

        private class FuncGenerateId : XsltFunctionImpl {
            public FuncGenerateId() : base(0, 1, XPathResultType.String , new XPathResultType[] { XPathResultType.NodeSet }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                if(args.Length > 0) {
                    XPathNodeIterator it = ToIterator(args[0]);
                    if(it.MoveNext()) {
                        return it.Current.UniqueId;
                    } else {
                        // if empty nodeset, return empty string, otherwise return generated id
                        return string.Empty;
                    }
                } else {
                    return docContext.UniqueId;
                }
            }
        }

        private class FuncSystemProp : XsltFunctionImpl {
            public FuncSystemProp() : base(1, 1, XPathResultType.String , new XPathResultType[] { XPathResultType.String  }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                return ((XsltCompileContext)xsltContext).SystemProperty(ToString(args[0]));
            }
        }

        // see http://www.w3.org/TR/xslt#function-element-available
        private class FuncElementAvailable : XsltFunctionImpl {
            public FuncElementAvailable() : base(1, 1, XPathResultType.Boolean, new XPathResultType[] { XPathResultType.String  }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                return ((XsltCompileContext)xsltContext).ElementAvailable(ToString(args[0]));
            }
        }

        // see: http://www.w3.org/TR/xslt#function-function-available
        private class FuncFunctionAvailable : XsltFunctionImpl {
            public FuncFunctionAvailable() : base(1, 1, XPathResultType.Boolean, new XPathResultType[] { XPathResultType.String  }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                return ((XsltCompileContext)xsltContext).FunctionAvailable(ToString(args[0]));
            }
        }

        private class FuncDocument : XsltFunctionImpl {
            public FuncDocument() : base(1, 2, XPathResultType.NodeSet, new XPathResultType[] { XPathResultType.Any    , XPathResultType.NodeSet}) {}
            
            // SxS: This method uses resource names read from source document and does not expose any resources to the caller.
            // It's OK to suppress the SxS warning.
            [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
            [ResourceExposure(ResourceScope.None)]
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                string baseUri = null;
                if (args.Length == 2) {
                    XPathNodeIterator it = ToIterator(args[1]);
                    if (it.MoveNext()){
                        baseUri = it.Current.BaseURI;
                    } else {
                        // http://www.w3.org/1999/11/REC-xslt-19991116-errata (E14):
                        // It is an error if the second argument node-set is empty and the URI reference is relative; the XSLT processor may signal the error;
                        // if it does not signal an error, it must recover by returning an empty node-set.
                        baseUri = string.Empty; // call to Document will fail if args[0] is reletive.
                    }
                }
                try {
                    return ((XsltCompileContext)xsltContext).Document(args[0], baseUri);
                }
                catch (Exception e) {
                    if (!XmlException.IsCatchableException(e)) {
                        throw;
                    }
                    return XPathEmptyIterator.Instance;
                }
            }
        }

        private class FuncKey : XsltFunctionImpl {
            public FuncKey() : base(2, 2, XPathResultType.NodeSet, new XPathResultType[] { XPathResultType.String , XPathResultType.Any     }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                XsltCompileContext xsltCompileContext = (XsltCompileContext) xsltContext;

                string local, prefix;
                PrefixQName.ParseQualifiedName(ToString(args[0]), out prefix, out local);
                string ns = xsltContext.LookupNamespace(prefix);
                XmlQualifiedName keyName = new XmlQualifiedName(local, ns);

                XPathNavigator root = docContext.Clone();
                root.MoveToRoot();

                ArrayList resultCollection = null;

                foreach (Key key in xsltCompileContext.processor.KeyList) {
                    if (key.Name == keyName) {
                        Hashtable keyTable = key.GetKeys(root);
                        if (keyTable == null) {
                            keyTable = xsltCompileContext.BuildKeyTable(key, root);
                            key.AddKey(root, keyTable);
                        }

                        XPathNodeIterator it = args[1] as XPathNodeIterator;
                        if (it != null) {
                            it = it.Clone();
                            while (it.MoveNext()) {
                                resultCollection = AddToList(resultCollection, (ArrayList) keyTable[it.Current.Value]);
                            }
                        } else {
                            resultCollection = AddToList(resultCollection, (ArrayList) keyTable[ToString(args[1])]);
                        }
                    }
                }
                if (resultCollection == null) {
                    return XPathEmptyIterator.Instance;
                } else if (resultCollection[0] is XPathNavigator) {
                    return new XPathArrayIterator(resultCollection);
                } else {
                    return new XPathMultyIterator(resultCollection);
                }
            }

            static ArrayList AddToList(ArrayList resultCollection, ArrayList newList) {
                if (newList == null) {
                    return resultCollection;
                }
                if (resultCollection == null) {
                    return newList;
                }
                Debug.Assert(resultCollection.Count != 0);
                Debug.Assert(newList.Count != 0);
                if (! (resultCollection[0] is ArrayList)) {
                    // Transform resultCollection from ArrayList(XPathNavigator) to ArrayList(ArrayList(XPathNavigator))
                    Debug.Assert(resultCollection[0] is XPathNavigator);
                    ArrayList firstList = resultCollection;
                    resultCollection = new ArrayList();
                    resultCollection.Add(firstList);
                }
                resultCollection.Add(newList);
                return resultCollection;
            }
        }

        private class FuncFormatNumber : XsltFunctionImpl {
            public FuncFormatNumber() : base(2, 3, XPathResultType.String , new XPathResultType[] { XPathResultType.Number , XPathResultType.String, XPathResultType.String }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                DecimalFormat formatInfo = ((XsltCompileContext)xsltContext).ResolveFormatName(args.Length == 3 ? ToString(args[2]) : null);
                return DecimalFormatter.Format(ToNumber(args[0]), ToString(args[1]), formatInfo);
            }
        }

        private class FuncNodeSet : XsltFunctionImpl {
            public FuncNodeSet() : base(1, 1, XPathResultType.NodeSet, new XPathResultType[] { XPathResultType.Navigator }) {}
            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                return new XPathSingletonIterator(ToNavigator(args[0]));
            }
        }

        private class FuncExtension : XsltFunctionImpl {
            private object            extension;
            private MethodInfo        method;
            private TypeCode[]        typeCodes;
            private PermissionSet     permissions;

            public FuncExtension(object extension, MethodInfo method, PermissionSet permissions) {
                Debug.Assert(extension != null);
                Debug.Assert(method    != null);
                this.extension   = extension;
                this.method      = method;
                this.permissions = permissions;
                XPathResultType returnType = GetXPathType(method.ReturnType);

                ParameterInfo[] parameters = method.GetParameters();
                int minArgs = parameters.Length;
                int maxArgs = parameters.Length;
                this.typeCodes = new TypeCode[parameters.Length];
                XPathResultType[] argTypes = new XPathResultType[parameters.Length];
                bool optionalParams = true; // we allow only last params be optional. Set false on the first non optional.
                for (int i = parameters.Length - 1; 0 <= i; i --) {            // Revers order is essential: counting optional parameters
                    typeCodes[i] = Type.GetTypeCode(parameters[i].ParameterType);
                    argTypes[i] = GetXPathType(parameters[i].ParameterType);
                    if(optionalParams) {
                        if(parameters[i].IsOptional) {
                            minArgs --;
                        } else {
                            optionalParams = false;
                        }
                    }
                }
                base.Init(minArgs, maxArgs, returnType, argTypes);
            }

            public override object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
                Debug.Assert(args.Length <= this.Minargs, "We cheking this on resolve time");
                for(int i = args.Length -1; 0 <= i; i --) {
                    args[i] = ConvertToXPathType(args[i], this.ArgTypes[i], this.typeCodes[i]);
                }
                if (this.permissions != null) {
                    this.permissions.PermitOnly();
                }
                return method.Invoke(extension, args);
            }
        }
    }
}
