//------------------------------------------------------------------------------
// <copyright file="XslTransform.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml.Xsl {
    using System.Reflection;
    using System.Diagnostics;
    using System.IO;
    using System.Xml.XPath;
    using System.Xml.Xsl.XsltOld;
    using MS.Internal.Xml.XPath;
    using MS.Internal.Xml.Cache;
    using System.Collections.Generic;
    using System.Xml.Xsl.XsltOld.Debugger;
    using System.Security.Policy;
    using System.Security.Permissions;
    using System.Runtime.Versioning;
    using System.Xml.XmlConfiguration;

    [Obsolete("This class has been deprecated. Please use System.Xml.Xsl.XslCompiledTransform instead. http://go.microsoft.com/fwlink/?linkid=14202")]
    public sealed class XslTransform {
        private XmlResolver _documentResolver = null;
        private bool isDocumentResolverSet = false;
        private XmlResolver _DocumentResolver {
            get {
                if (isDocumentResolverSet)
                    return _documentResolver;
                else
                    return XsltConfigSection.CreateDefaultResolver();
            }
        }


        //
        // Compiled stylesheet state
        //
        private Stylesheet  _CompiledStylesheet;
        private List<TheQuery>   _QueryStore;
        private RootAction  _RootAction;

        private IXsltDebugger debugger;

        public XslTransform() {}

        public XmlResolver XmlResolver {
            set {
                _documentResolver = value;
                isDocumentResolverSet = true;
            }
        }

        public void Load(XmlReader stylesheet) {
            Load(stylesheet, XsltConfigSection.CreateDefaultResolver());
        }
        public void Load(XmlReader stylesheet, XmlResolver resolver) {
            Load(new XPathDocument(stylesheet, XmlSpace.Preserve), resolver);
        }

        public void Load(IXPathNavigable stylesheet) {
            Load(stylesheet, XsltConfigSection.CreateDefaultResolver());
        }
        public void Load(IXPathNavigable stylesheet, XmlResolver resolver) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet");
            }
            Load(stylesheet.CreateNavigator(), resolver);
        }

        public void Load(XPathNavigator stylesheet) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet");
            }
            Load(stylesheet, XsltConfigSection.CreateDefaultResolver());
        }
        public void Load(XPathNavigator stylesheet, XmlResolver resolver) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet");
            }
            Compile(stylesheet, resolver, /*evidence:*/null);
        }

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        public void Load(string url) {
            XmlTextReaderImpl tr = new XmlTextReaderImpl(url);
            Evidence evidence = XmlSecureResolver.CreateEvidenceForUrl(tr.BaseURI); // We should ask BaseURI before we start reading because it's changing with each node
            Compile(Compiler.LoadDocument(tr).CreateNavigator(), XsltConfigSection.CreateDefaultResolver(), evidence);
        }

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        public void Load(string url, XmlResolver resolver) {
            XmlTextReaderImpl tr = new XmlTextReaderImpl(url); {
                tr.XmlResolver = resolver;
            }
            Evidence evidence = XmlSecureResolver.CreateEvidenceForUrl(tr.BaseURI); // We should ask BaseURI before we start reading because it's changing with each node
            Compile(Compiler.LoadDocument(tr).CreateNavigator(), resolver, evidence);
        }

        public void Load(IXPathNavigable stylesheet, XmlResolver resolver, Evidence evidence) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet");
            }
            Load(stylesheet.CreateNavigator(), resolver, evidence);
        }
        public void Load(XmlReader stylesheet, XmlResolver resolver, Evidence evidence) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet");
            }
            Load(new XPathDocument(stylesheet, XmlSpace.Preserve), resolver, evidence);
        }
        public void Load(XPathNavigator stylesheet, XmlResolver resolver, Evidence evidence) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet");
            }
            if (evidence == null) {
                evidence = new Evidence();
            }
            else {
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
            }
            Compile(stylesheet, resolver, evidence);
        }

        // ------------------------------------ Transform() ------------------------------------ //

        private void CheckCommand() {
            if (_CompiledStylesheet == null) {
                throw new InvalidOperationException(Res.GetString(Res.Xslt_NoStylesheetLoaded));
            }
        }

        public XmlReader Transform(XPathNavigator input, XsltArgumentList args, XmlResolver resolver) {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, debugger);
            return processor.StartReader();
        }

        public XmlReader Transform(XPathNavigator input, XsltArgumentList args) {
            return Transform(input, args, _DocumentResolver);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, XmlWriter output, XmlResolver resolver) {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, debugger);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, XmlWriter output) {
            Transform(input, args, output, _DocumentResolver);
        }
        public void Transform(XPathNavigator input, XsltArgumentList args, Stream output, XmlResolver resolver) {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, debugger);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, Stream output) {
            Transform(input, args, output, _DocumentResolver);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, TextWriter output, XmlResolver resolver) {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, debugger);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, TextWriter output) {
            CheckCommand();
            Processor processor = new Processor(input, args, _DocumentResolver, _CompiledStylesheet, _QueryStore, _RootAction, debugger);
            processor.Execute(output);
        }

        public XmlReader Transform(IXPathNavigable input, XsltArgumentList args, XmlResolver resolver) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            return Transform(input.CreateNavigator(), args, resolver);
        }

        public XmlReader Transform(IXPathNavigable input, XsltArgumentList args) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            return Transform(input.CreateNavigator(), args, _DocumentResolver);
        }
        public void Transform(IXPathNavigable input, XsltArgumentList args, TextWriter output, XmlResolver resolver) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            Transform(input.CreateNavigator(), args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, TextWriter output) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            Transform(input.CreateNavigator(), args, output, _DocumentResolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, Stream output, XmlResolver resolver) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            Transform(input.CreateNavigator(), args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, Stream output) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            Transform(input.CreateNavigator(), args, output, _DocumentResolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, XmlWriter output, XmlResolver resolver) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            Transform(input.CreateNavigator(), args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, XmlWriter output) {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            Transform(input.CreateNavigator(), args, output, _DocumentResolver);
        }

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        public void Transform(String inputfile, String outputfile, XmlResolver resolver) {
            FileStream fs = null;
            try {
                // We should read doc before creating output file in case they are the same
                XPathDocument doc = new XPathDocument(inputfile);
                fs = new FileStream(outputfile, FileMode.Create, FileAccess.ReadWrite);
                Transform(doc, /*args:*/null, fs, resolver);
            }
            finally {
                if (fs != null) {
                    fs.Close();
                }
            }
	    }

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        public void Transform(String inputfile, String outputfile) {
            Transform(inputfile, outputfile, _DocumentResolver);
        }

        // Implementation

        private void Compile(XPathNavigator stylesheet, XmlResolver resolver, Evidence evidence) {
            Debug.Assert(stylesheet != null);

            Compiler  compiler = (Debugger == null) ? new Compiler() : new DbgCompiler(this.Debugger);
            NavigatorInput input = new NavigatorInput(stylesheet);
            compiler.Compile(input, resolver ?? XmlNullResolver.Singleton, evidence);

            Debug.Assert(compiler.CompiledStylesheet != null);
            Debug.Assert(compiler.QueryStore != null);
            Debug.Assert(compiler.QueryStore != null);
            _CompiledStylesheet = compiler.CompiledStylesheet;
            _QueryStore         = compiler.QueryStore;
            _RootAction         = compiler.RootAction;
        }

        internal IXsltDebugger Debugger {
            get { return this.debugger; }
        }

#if false
        internal XslTransform(IXsltDebugger debugger) {
            this.debugger = debugger;
        }
#endif

        internal XslTransform(object debugger) {
            if (debugger != null) {
                this.debugger = new DebuggerAddapter(debugger);
            }
        }

        private class DebuggerAddapter : IXsltDebugger {
            private object unknownDebugger;
            private MethodInfo getBltIn;
            private MethodInfo onCompile;
            private MethodInfo onExecute;
            public DebuggerAddapter(object unknownDebugger) {
                this.unknownDebugger = unknownDebugger;
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                Type unknownType = unknownDebugger.GetType();
                getBltIn  = unknownType.GetMethod("GetBuiltInTemplatesUri", flags);
                onCompile = unknownType.GetMethod("OnInstructionCompile"  , flags);
                onExecute = unknownType.GetMethod("OnInstructionExecute"  , flags);
            }
            // ------------------ IXsltDebugger ---------------
            public string GetBuiltInTemplatesUri() {
                if (getBltIn == null) {
                    return null;
                }
                return (string) getBltIn.Invoke(unknownDebugger, new object[] {});
            }
            public void OnInstructionCompile(XPathNavigator styleSheetNavigator) {
                if (onCompile != null) {
                    onCompile.Invoke(unknownDebugger, new object[] { styleSheetNavigator });
                }
            }
            public void OnInstructionExecute(IXsltProcessor xsltProcessor) {
                if (onExecute != null) {
                    onExecute.Invoke(unknownDebugger, new object[] { xsltProcessor });
                }
            }
        }
    }
}
