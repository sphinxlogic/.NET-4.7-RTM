//------------------------------------------------------------------------------
// <copyright file="QilGenerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <spec>http://www.w3.org/TR/xslt.html</spec>
// <spec>http://www.w3.org/TR/xslt20/</spec>
//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt {
    using Res           = System.Xml.Utils.Res;
    using ScopeRecord   = CompilerScopeManager<QilIterator>.ScopeRecord;
    using T             = XmlQueryTypeFactory;

    // Everywhere in this code in case of error in the stylesheet we should call ReportError or ReportWarning

    internal class ReferenceReplacer : QilReplaceVisitor {
        private QilReference lookFor, replaceBy;

        public ReferenceReplacer(QilFactory f) : base(f) {
        }

        public QilNode Replace(QilNode expr, QilReference lookFor, QilReference replaceBy) {
            QilDepthChecker.Check(expr);
            this.lookFor = lookFor;
            this.replaceBy = replaceBy;
            return VisitAssumeReference(expr);
        }

        protected override QilNode VisitReference(QilNode n) {
            return (n == lookFor) ? replaceBy : n;
        }
    }

    internal partial class QilGenerator : IErrorHelper {
        private CompilerScopeManager<QilIterator> scope;
        private OutputScopeManager      outputScope;
        private HybridDictionary        prefixesInUse;

        private XsltQilFactory          f;
        private XPathBuilder            xpathBuilder;
        private XPathParser<QilNode>    xpathParser;
        private XPathPatternBuilder     ptrnBuilder;
        private XPathPatternParser      ptrnParser;
        private ReferenceReplacer       refReplacer;
        private KeyMatchBuilder         keyMatchBuilder;
        private InvokeGenerator         invkGen;
        private MatcherBuilder          matcherBuilder;
        private QilStrConcatenator      strConcat;
        private VariableHelper          varHelper;

        private Compiler        compiler;
        private QilList         functions;
        private QilFunction     generalKey;
        private bool            formatNumberDynamicUsed;
        private QilList         extPars;
        private QilList         gloVars;
        private QilList         nsVars;

        private XmlQueryType    elementOrDocumentType;
        private XmlQueryType    textOrAttributeType;
        private XslNode         lastScope;
        private XslVersion      xslVersion;

        private QilName         nameCurrent;
        private QilName         namePosition;
        private QilName         nameLast;
        private QilName         nameNamespaces;
        private QilName         nameInit;

        private SingletonFocus  singlFocus;
        private FunctionFocus   funcFocus;
        private LoopFocus       curLoop;

        private int             formatterCnt;

        public static QilExpression CompileStylesheet(Compiler compiler) {
            return new QilGenerator(compiler.IsDebug).Compile(compiler);
        }

        private QilGenerator(bool debug) {
            scope           = new CompilerScopeManager<QilIterator>();
            outputScope     = new OutputScopeManager();
            prefixesInUse   = new HybridDictionary();
            f               = new XsltQilFactory(new QilFactory(), debug);
            xpathBuilder    = new XPathBuilder((IXPathEnvironment) this);
            xpathParser     = new XPathParser<QilNode>();
            ptrnBuilder     = new XPathPatternBuilder((IXPathEnvironment) this);
            ptrnParser      = new XPathPatternParser();
            refReplacer     = new ReferenceReplacer(f.BaseFactory);
            invkGen         = new InvokeGenerator(f, debug);
            matcherBuilder  = new MatcherBuilder(f, refReplacer, invkGen);
            singlFocus      = new SingletonFocus(f);
            funcFocus       = new FunctionFocus();
            curLoop         = new LoopFocus(f);
            strConcat       = new QilStrConcatenator(f);
            varHelper       = new VariableHelper(f);

            elementOrDocumentType = T.DocumentOrElement;
            textOrAttributeType   = T.NodeChoice(XmlNodeKindFlags.Text | XmlNodeKindFlags.Attribute);

            nameCurrent     = f.QName("current"   , XmlReservedNs.NsXslDebug);
            namePosition    = f.QName("position"  , XmlReservedNs.NsXslDebug);
            nameLast        = f.QName("last"      , XmlReservedNs.NsXslDebug);
            nameNamespaces  = f.QName("namespaces", XmlReservedNs.NsXslDebug);
            nameInit        = f.QName("init"      , XmlReservedNs.NsXslDebug);

            formatterCnt    = 0;
        }

        private bool IsDebug {
            get { return compiler.IsDebug; }
        }

        private bool EvaluateFuncCalls  { get { return !IsDebug; } }
        private bool InferXPathTypes    { get { return !IsDebug; } }

        private QilExpression Compile(Compiler compiler) {
            Debug.Assert(compiler != null);
            this.compiler   = compiler;
            this.functions  = f.FunctionList();
            this.extPars    = f.GlobalParameterList();
            this.gloVars    = f.GlobalVariableList();
            this.nsVars     = f.GlobalVariableList();

            compiler.Scripts.CompileScripts();

            // Refactor huge templates into smaller ones (more JIT friendly)
            (new XslAstRewriter()).Rewrite(compiler);

            if (!IsDebug) {
                (new XslAstAnalyzer()).Analyze(compiler);
            }

            // Global variables and external params are visible from everywhere, so we have
            // to prepopulate the scope with all of them before starting compilation
            CreateGlobalVarPars();

            try {
                CompileKeys();
                CompileAndSortMatches(compiler.Root.Imports[0]);
                PrecompileProtoTemplatesHeaders();
                CompileGlobalVariables();

                foreach (ProtoTemplate tmpl in compiler.AllTemplates) {
                    CompileProtoTemplate(tmpl);
                }
                varHelper.CheckEmpty();
            }
            catch (XslLoadException e) {
                e.SetSourceLineInfo(lastScope.SourceLine);
                throw;
            }
            catch (Exception e) {
                if (!XmlException.IsCatchableException(e)) {
                    throw;
                }
                throw new XslLoadException(e, lastScope.SourceLine);
            }

            CompileInitializationCode();
            QilNode root = CompileRootExpression(compiler.StartApplyTemplates);

            // Clean default values which we calculate in caller context
            foreach (ProtoTemplate tmpl in compiler.AllTemplates) {
                foreach (QilParameter par in tmpl.Function.Arguments) {
                    if (!IsDebug || par.Name.Equals(nameNamespaces)) {
                        par.DefaultValue = null;
                    }
                }
            }

            // Create list of all early bound objects
            Dictionary<string, Type> scriptClasses = compiler.Scripts.ScriptClasses;
            List<EarlyBoundInfo> ebTypes = new List<EarlyBoundInfo>(scriptClasses.Count);
            foreach (KeyValuePair<string, Type> pair in scriptClasses) {
                if (pair.Value != null) {
                    ebTypes.Add(new EarlyBoundInfo(pair.Key, pair.Value));
                }
            }

            QilExpression qil = f.QilExpression(root, f.BaseFactory); {
                qil.EarlyBoundTypes       = ebTypes;
                qil.FunctionList          = functions;
                qil.GlobalParameterList   = extPars;
                qil.GlobalVariableList    = gloVars;
                qil.WhitespaceRules       = compiler.WhitespaceRules;
                qil.IsDebug               = IsDebug;
                qil.DefaultWriterSettings = compiler.Output.Settings;
            }

            QilDepthChecker.Check(qil);

            return qil;
        }

        private QilNode InvokeOnCurrentNodeChanged() {
            Debug.Assert(IsDebug && curLoop.IsFocusSet);
            QilIterator i;
            return f.Loop(i = f.Let(f.InvokeOnCurrentNodeChanged(curLoop.GetCurrent())), f.Sequence());
        }

        [Conditional("DEBUG")]
        private void CheckSingletonFocus() {
            Debug.Assert(!curLoop.IsFocusSet && !funcFocus.IsFocusSet, "Must be compiled using singleton focus");
        }

        private void CompileInitializationCode() {
            // Initialization code should be executed before any other code (global variables/parameters or root expression)
            // For this purpose we insert it as THE FIRST global variable $init (global variables are calculated before global parameters)
            // and put all initalization code in it.
            // In retail mode global variables are calculated lasely if they don't have side effects. 
            // To mark $init as variable with side effect we put all code to function and set SideEffect flag on this function.
            // ILGen expects that all library functions are sideeffect free. To prevent calls to RegisterDecimalFormat() to be optimized out 
            // we add results returned from these calls and return them as a result of initialization function.
            QilNode init = f.Int32(0);

            // Register all decimal formats, they are needed for format-number()
            if (formatNumberDynamicUsed || IsDebug) {
                bool defaultDefined = false;
                foreach (DecimalFormatDecl format in compiler.DecimalFormats) {
                    init = f.Add(init, f.InvokeRegisterDecimalFormat(format));
                    defaultDefined |= (format.Name == DecimalFormatDecl.Default.Name);
                }
                if (!defaultDefined) {
                    init = f.Add(init, f.InvokeRegisterDecimalFormat(DecimalFormatDecl.Default));
                }
            }

            // Register all script namespaces
            foreach (string scriptNs in compiler.Scripts.ScriptClasses.Keys) {
                init = f.Add(init, f.InvokeCheckScriptNamespace(scriptNs));
            }

            if (init.NodeType == QilNodeType.Add) {
                QilFunction initFunction = f.Function(f.FormalParameterList(), init, /*sideEffects:*/f.True());
                initFunction.DebugName = "Init";
                this.functions.Add(initFunction);

                QilNode initBinding = f.Invoke(initFunction, f.ActualParameterList());
                if (IsDebug) {
                    // In debug mode all variables must have type item*
                    initBinding = f.TypeAssert(initBinding, T.ItemS);
                }
                QilIterator initVar = f.Let(initBinding);
                initVar.DebugName = nameInit.ToString();
                gloVars.Insert(0, initVar);
            }
        }

        private QilNode CompileRootExpression(XslNode applyTmpls) {
            // Compile start apply-templates call
            CheckSingletonFocus();
            singlFocus.SetFocus(SingletonFocusType.InitialContextNode);
            QilNode result = GenerateApply(compiler.Root, applyTmpls);
            singlFocus.SetFocus(null);

            return f.DocumentCtor(result);
        }

        private QilList EnterScope(XslNode node) {
            // This is the only place where lastScope is changed
            lastScope = node;
            xslVersion = node.XslVersion;
            if (this.scope.EnterScope(node.Namespaces)) {
                return BuildDebuggerNamespaces();
            }
            return null;
        }

        private void ExitScope() {
            this.scope.ExitScope();
        }

        private QilList BuildDebuggerNamespaces() {
            if (IsDebug) {
                QilList nsDecls = f.BaseFactory.Sequence();
                foreach (ScopeRecord rec in this.scope) {
                    nsDecls.Add(f.NamespaceDecl(f.String(rec.ncName), f.String(rec.nsUri)));
                }
                return nsDecls;
            }
            return null;
        }

        // For each call instruction - call-template, use-attribute-sets, apply-template, apply-imports - we have
        // to pass the current execution context which may be represented as three additional implicit arguments:
        // current, position, last.  In most cases the last two ones are never used, so for the purpose
        // of optimization in non-debug mode we bind them only if they are actually needed.

        // Strictly speaking, a (proto)template function is supplied with the additional position argument if both
        // the following conditions are true:
        //   1. At least one template within the given stylesheet contains a "----" position() function invocation
        //      (needPositionArgs == true).  ---- here means "not within any of for-each instructions".
        //   2. THIS template contains a ---- position() invocation or a ---- call-template, use-attribute-sets,
        //      or apply-imports instruction.  Note: apply-template's are not taken into account because in that
        //      case the call will be actually wrapped in a tuple.
        //
        // The same is true for additional last arguments.

        // There are 3 cases when context methods may be called:
        // 1. In context of for-each expression
        // 2. In context of template
        // 3. In context of global variable
        // We treating this methods differentely when they are called to create implicit arguments.
        // Implicite argument (position, last) are rare and lead to uneficiant code. So we treating them
        // specialy to be able eliminate them later, wen we compiled everithing and can detect was they used or not.

        // Returns context node
        private QilNode GetCurrentNode() {
            if (curLoop.IsFocusSet) {
                return curLoop.GetCurrent();
            } else if (funcFocus.IsFocusSet) {
                return funcFocus.GetCurrent();
            } else {
                return singlFocus.GetCurrent();
            }
        }

        // Returns context position
        private QilNode GetCurrentPosition() {
            if (curLoop.IsFocusSet) {
                return curLoop.GetPosition();
            } else if (funcFocus.IsFocusSet) {
                return funcFocus.GetPosition();
            } else {
                return singlFocus.GetPosition();
            }
        }

        // Returns context size
        private QilNode GetLastPosition() {
            if (curLoop.IsFocusSet) {
                return curLoop.GetLast();
            } else if (funcFocus.IsFocusSet) {
                return funcFocus.GetLast();
            } else {
                return singlFocus.GetLast();
            }
        }

        private XmlQueryType ChooseBestType(VarPar var) {
            if (IsDebug || !InferXPathTypes) {
                return T.ItemS;
            }

            switch (var.Flags & XslFlags.TypeFilter) {
            case XslFlags.String  : return T.StringX;;
            case XslFlags.Number  : return T.DoubleX;
            case XslFlags.Boolean : return T.BooleanX;
            case XslFlags.Node    : return T.NodeNotRtf;
            case XslFlags.Nodeset : return T.NodeNotRtfS;
            case XslFlags.Rtf     : return T.Node;
            case XslFlags.Node    | XslFlags.Rtf     : return T.Node;
            case XslFlags.Node    | XslFlags.Nodeset : return T.NodeNotRtfS;
            case XslFlags.Nodeset | XslFlags.Rtf     : return T.NodeS;
            case XslFlags.Node    | XslFlags.Nodeset | XslFlags.Rtf : return T.NodeS;
            default               : return T.ItemS;
            }
        }

        // In debugger we need to pass to each (almost) template $namespace parameter with list of namespaces that
        // are defined on stylesheet and this template. In most cases this will be only xmlns:xsl="..."
        // To prevent creating these list with each call-template/apply-template we create one global variable for each unique set of namespaces
        // This function looks through list of existent global variables for suitable ns list and add one if none was found.
        private QilIterator GetNsVar(QilList nsList) {
            Debug.Assert(IsDebug, "This is debug only logic");
            // All global vars at this point are nsList like one we are looking now.
            foreach (QilIterator var in this.nsVars) {
                Debug.Assert(var.XmlType.IsSubtypeOf(T.NamespaceS));
                Debug.Assert(var.Binding is QilList);
                QilList varList = (QilList)var.Binding;
                if (varList.Count != nsList.Count) {
                    continue;
                }
                bool found = true;
                for (int i = 0; i < nsList.Count; i ++) {
                    Debug.Assert(nsList[i].NodeType  == QilNodeType.NamespaceDecl);
                    Debug.Assert(varList[i].NodeType == QilNodeType.NamespaceDecl);
                    if (
                        ((QilLiteral)((QilBinary)nsList[i]).Right).Value != ((QilLiteral)((QilBinary)varList[i]).Right).Value ||
                        ((QilLiteral)((QilBinary)nsList[i]).Left ).Value != ((QilLiteral)((QilBinary)varList[i]).Left ).Value
                    ) {
                        found = false;
                        break;
                    }
                }
                if (found) {
                    return var;  // Found!
                }
            }
            QilIterator newVar = f.Let(nsList);
            newVar.DebugName = f.QName("ns" + this.nsVars.Count, XmlReservedNs.NsXslDebug).ToString();
            this.gloVars.Add(newVar);
            this.nsVars.Add(newVar);
            return newVar;
        }

        private void PrecompileProtoTemplatesHeaders() {
            // All global variables should be in scoupe here.
            List<VarPar>                     paramWithCalls  = null;
            Dictionary<VarPar, Template   >  paramToTemplate = null;
            Dictionary<VarPar, QilFunction>  paramToFunction = null;

            foreach (ProtoTemplate tmpl in compiler.AllTemplates) {
                Debug.Assert(tmpl != null && tmpl.Function == null);
                Debug.Assert(tmpl.NodeType == XslNodeType.AttributeSet || tmpl.NodeType == XslNodeType.Template);
                QilList args = f.FormalParameterList();
                XslFlags flags = !IsDebug ? tmpl.Flags : XslFlags.FullFocus;

                QilList nsList = EnterScope(tmpl);
                if ((flags & XslFlags.Current) != 0) {
                    args.Add(CreateXslParam(CloneName(nameCurrent), T.NodeNotRtf));
                }
                if ((flags & XslFlags.Position) != 0) {
                    args.Add(CreateXslParam(CloneName(namePosition), T.DoubleX));
                }
                if ((flags & XslFlags.Last) != 0) {
                    args.Add(CreateXslParam(CloneName(nameLast), T.DoubleX));
                }
                if (IsDebug && nsList != null) {
                    // AttributeSet doesn't need this logic because: 1) it doesn't have args; 2) we merge them.
                    // SimplifiedStylesheet has nsList == null as well.
                    QilParameter ns = CreateXslParam(CloneName(nameNamespaces), T.NamespaceS);
                    ns.DefaultValue = GetNsVar(nsList);
                    args.Add(ns);
                }

                Template template = tmpl as Template;
                if (template != null) {
                    Debug.Assert(tmpl.NodeType == XslNodeType.Template);

                    CheckSingletonFocus();
                    funcFocus.StartFocus(args, flags);
                    for (int i = 0; i < tmpl.Content.Count; i++) {
                        XslNode node = tmpl.Content[i];
                        if (node.NodeType == XslNodeType.Text) {
                            // NOTE: We should take care of a bizarre case when xsl:param comes after TextCtor:
                            // <xsl:template match="/" xml:space="preserve">  <xsl:param name="par"/>
                            continue;
                        }
                        if (node.NodeType == XslNodeType.Param) {
                            VarPar xslPar = (VarPar)node;
                            EnterScope(xslPar);
                            if (scope.IsLocalVariable(xslPar.Name.LocalName, xslPar.Name.NamespaceUri)) {
                                ReportError(/*[XT0580]*/Res.Xslt_DupLocalVariable, xslPar.Name.QualifiedName);
                            }
                            QilParameter param = CreateXslParam(xslPar.Name, ChooseBestType(xslPar));
                            if (IsDebug) {
                                param.Annotation = xslPar;
                                // Actual compilation will happen in CompileProtoTemplate()
                            } else {
                                if ((xslPar.DefValueFlags & XslFlags.HasCalls) == 0) {
                                    param.DefaultValue = CompileVarParValue(xslPar);
                                } else {
                                    // We can't compile param default value here because it contains xsl:call-template and
                                    // we will not be able to compile any calls befor we finish with all headers
                                    // So we compile this default value as a call to helper function. Now we create header for this function
                                    // and preserve this param in paramWithCall list. Later in this function we finaly compile all preserved
                                    // parameters and set resulted default values as helper function definition.
                                    QilList paramFormal = f.FormalParameterList();
                                    QilList paramActual = f.ActualParameterList();
                                    for (int j = 0; j < args.Count; j ++) {
                                        QilParameter formal = f.Parameter(args[j].XmlType); {
                                            formal.DebugName = ((QilParameter) args[j]).DebugName;
                                            formal.Name = CloneName(((QilParameter)args[j]).Name);
                                            SetLineInfo(formal, args[j].SourceLine);
                                        }
                                        paramFormal.Add(formal);
                                        paramActual.Add(args[j]);
                                    }
                                    // Param doesn't know what implicit args it needs, so we pass all implicit args that was passed to its template.
                                    // let's reflect this fact in parans FocusFlags:
                                    xslPar.Flags |= (template.Flags & XslFlags.FocusFilter);
                                    QilFunction paramFunc = f.Function(paramFormal,
                                        f.Boolean((xslPar.DefValueFlags & XslFlags.SideEffects) != 0),
                                        ChooseBestType(xslPar)
                                    );
                                    paramFunc.SourceLine = SourceLineInfo.NoSource;
                                    paramFunc.DebugName = "<xsl:param name=\"" + xslPar.Name.QualifiedName + "\">";
                                    param.DefaultValue = f.Invoke(paramFunc, paramActual);
                                    // store VarPar here to compile it on next pass:
                                    if (paramWithCalls == null) {
                                        paramWithCalls  = new List<VarPar>();
                                        paramToTemplate = new Dictionary<VarPar, Template>();
                                        paramToFunction = new Dictionary<VarPar, QilFunction>();
                                    }
                                    paramWithCalls.Add(xslPar);
                                    paramToTemplate.Add(xslPar, template );
                                    paramToFunction.Add(xslPar, paramFunc);
                                }
                            }
                            SetLineInfo(param, xslPar.SourceLine);
                            ExitScope();
                            scope.AddVariable(xslPar.Name, param);
                            args.Add(param);
                        } else {
                            break;
                        }
                    }
                    funcFocus.StopFocus();
                }
                ExitScope();

                tmpl.Function = f.Function(args,
                    f.Boolean((tmpl.Flags & XslFlags.SideEffects) != 0),
                    tmpl is AttributeSet ? T.AttributeS : T.NodeNotRtfS
                );
                tmpl.Function.DebugName = tmpl.GetDebugName();
                Debug.Assert((template != null) == (tmpl.SourceLine != null), "Templates must have line information, and attribute sets must not");
                SetLineInfo(tmpl.Function, tmpl.SourceLine ?? SourceLineInfo.NoSource);
                this.functions.Add(tmpl.Function);
            } // foreach (ProtoTemplate tmpl in compiler.AllTemplates)

            // Finish compiling postponed parameters (those having calls in their default values)
            if (paramWithCalls != null) {
                Debug.Assert(! IsDebug, "In debug mode we don't generate parumWithCalls functions. Otherwise focus flags should be adjusted");
                foreach (VarPar par in paramWithCalls) {
                    Template    tmpl = paramToTemplate[par];
                    QilFunction func = paramToFunction[par];
                    CheckSingletonFocus();
                    funcFocus.StartFocus(func.Arguments, par.Flags);
                    EnterScope(tmpl);
                    EnterScope(par);
                    foreach (QilParameter arg in func.Arguments) {
                        scope.AddVariable(arg.Name, arg);
                    }
                    func.Definition = CompileVarParValue(par);
                    SetLineInfo(func.Definition, par.SourceLine);
                    ExitScope();
                    ExitScope();
                    funcFocus.StopFocus();
                    this.functions.Add(func);
                }
            }
        }

        private QilParameter CreateXslParam(QilName name, XmlQueryType xt) {
            QilParameter arg = f.Parameter(xt);
            arg.DebugName = name.ToString();
            arg.Name = name;
            return arg;
        }

        private void CompileProtoTemplate(ProtoTemplate tmpl) {
            Debug.Assert(tmpl != null && tmpl.Function != null && tmpl.Function.Definition.NodeType == QilNodeType.Unknown);

            EnterScope(tmpl);

            CheckSingletonFocus();
            funcFocus.StartFocus(tmpl.Function.Arguments, !IsDebug ? tmpl.Flags : XslFlags.FullFocus);
            foreach (QilParameter arg in tmpl.Function.Arguments) {
                if (arg.Name.NamespaceUri != XmlReservedNs.NsXslDebug) {
                    Debug.Assert(tmpl is Template, "Only templates can have explicit arguments");
                    if (IsDebug) {
                        Debug.Assert(arg.DefaultValue == null, "Argument must not be compiled yet");
                        VarPar xslParam = (VarPar)arg.Annotation;
                        QilList nsListParam = EnterScope(xslParam);
                        arg.DefaultValue = CompileVarParValue(xslParam);
                        ExitScope();
                        arg.DefaultValue = SetDebugNs(arg.DefaultValue, nsListParam);
                    } else {
                        // in !IsDebug we compile argument default value in PrecompileProtoTemplatesHeaders()
                    }
                    scope.AddVariable(arg.Name, arg);
                }
            }
            tmpl.Function.Definition = CompileInstructions(tmpl.Content);
            // tmpl.Function.Definition = AddCurrentPositionLast(tmpl.Function.Definition); We don't mask Cur,Pos,Last parameters with Cur,Pos,Last wariables any more
            // tmpl.Function.Definition = SetDebugNs(tmpl.Function.Definition, nsList); We add it as parameter now.
            funcFocus.StopFocus();

            ExitScope();
        }

        private QilList InstructionList() {
            return f.BaseFactory.Sequence();
        }

        private QilNode CompileInstructions(IList<XslNode> instructions) {
            return CompileInstructions(instructions, 0, InstructionList());
        }

        private QilNode CompileInstructions(IList<XslNode> instructions, int from) {
            return CompileInstructions(instructions, from, InstructionList());
        }

        private QilNode CompileInstructions(IList<XslNode> instructions, QilList content) {
            return CompileInstructions(instructions, 0, content);
        }

        private QilNode CompileInstructions(IList<XslNode> instructions, int from, QilList content) {
            Debug.Assert(instructions != null);
            for (int i = from; i < instructions.Count; i++) {
                XslNode     node     = instructions[i];
                XslNodeType nodeType = node.NodeType;
                if (nodeType == XslNodeType.Param) {
                    continue; // already compiled by CompileProtoTemplate()
                }
                QilList     nsList   = EnterScope(node);
                QilNode     result;

                switch (nodeType) {
                case XslNodeType.ApplyImports:      result = CompileApplyImports    (node); break;
                case XslNodeType.ApplyTemplates:    result = CompileApplyTemplates  ((XslNodeEx)node); break;
                case XslNodeType.Attribute:         result = CompileAttribute       ((NodeCtor)node); break;
                case XslNodeType.CallTemplate:      result = CompileCallTemplate    ((XslNodeEx)node); break;
                case XslNodeType.Choose:            result = CompileChoose          (node); break;
                case XslNodeType.Comment:           result = CompileComment         (node); break;
                case XslNodeType.Copy:              result = CompileCopy            (node); break;
                case XslNodeType.CopyOf:            result = CompileCopyOf          (node); break;
                case XslNodeType.Element:           result = CompileElement         ((NodeCtor)node); break;
                case XslNodeType.Error:             result = CompileError           (node); break;
                case XslNodeType.ForEach:           result = CompileForEach         ((XslNodeEx)node); break;
                case XslNodeType.If:                result = CompileIf              (node); break;
                case XslNodeType.List:              result = CompileList            (node); break;
                case XslNodeType.LiteralAttribute:  result = CompileLiteralAttribute(node); break;
                case XslNodeType.LiteralElement:    result = CompileLiteralElement  (node); break;
                case XslNodeType.Message:           result = CompileMessage         (node); break;
                case XslNodeType.Nop:               result = CompileNop             (node); break;
                case XslNodeType.Number:            result = CompileNumber          ((Number)node); break;
//              case XslNodeType.Otherwise:         wrapped by Choose
//              case XslNodeType.Param:             already compiled by CompileProtoTemplate()
                case XslNodeType.PI:                result = CompilePI              (node); break;
//              case XslNodeType.Sort:              wrapped by ForEach or ApplyTemplates, see CompileSorts()
//              case XslNodeType.Template:          global level element
                case XslNodeType.Text:              result = CompileText            ((Text)node); break;
                case XslNodeType.UseAttributeSet:   result = CompileUseAttributeSet (node); break;
                case XslNodeType.ValueOf:           result = CompileValueOf         (node); break;
                case XslNodeType.ValueOfDoe:        result = CompileValueOfDoe      (node); break;
                case XslNodeType.Variable:          result = CompileVariable        (node); break;
//              case XslNodeType.WithParam:         wrapped by CallTemplate or ApplyTemplates, see CompileWithParam()
                default:                            Debug.Fail("Unexpected type of AST node: " + nodeType.ToString()); result = null; break;
                }

                ExitScope();
                Debug.Assert(result != null, "Result of compilation should not be null");
                if (result.NodeType == QilNodeType.Sequence && result.Count == 0) {
                    continue;
                }

                // Do not create sequence points for literal attributes and use-attribute-sets
                if (nodeType != XslNodeType.LiteralAttribute && nodeType != XslNodeType.UseAttributeSet) {
                    SetLineInfoCheck(result, node.SourceLine);
                }

                result = SetDebugNs(result, nsList);
                if (nodeType == XslNodeType.Variable) {
                    QilIterator var = f.Let(result);
                    var.DebugName = node.Name.ToString();
                    scope.AddVariable(node.Name, var);
                    // Process all remaining instructions in the recursive call
                    result = f.Loop(var, CompileInstructions(instructions, i + 1));
                    i = instructions.Count;
                }

                content.Add(result);
            }
            if (!IsDebug && content.Count == 1) {
                return content[0];
            }
            return content;
        }

        private QilNode CompileList(XslNode node) {
            return CompileInstructions(node.Content);
        }

        private QilNode CompileNop(XslNode node) {
            return f.Nop(f.Sequence());
        }

        private void AddNsDecl(QilList content, string prefix, string nsUri) {
            if (this.outputScope.LookupNamespace(prefix) == nsUri) {
                return; // This prefix is already bound to required namespace. Nothing to do.
            }
            this.outputScope.AddNamespace(prefix, nsUri);
            content.Add(f.NamespaceDecl(f.String(prefix), f.String(nsUri)));
        }

        private QilNode CompileLiteralElement(XslNode node) {
            // IlGen requires that namespace declarations do not conflict with the namespace used by the element
            // constructor, see XmlILNamespaceAnalyzer.CheckNamespaceInScope() and SQLBUDT 389481, 389482. First
            // we try to replace all prefixes bound to aliases by result-prefixes of the corresponding
            // xsl:namespace-alias instructions. If there is at least one conflict, we leave all prefixes
            // untouched, changing only namespace URIs.
            bool changePrefixes = true;

        Start:
            prefixesInUse.Clear();

            QilName qname   = node.Name;
            string  prefix  = qname.Prefix;
            string  nsUri   = qname.NamespaceUri;

            compiler.ApplyNsAliases(ref prefix, ref nsUri);
            if (changePrefixes) {
                prefixesInUse.Add(prefix, nsUri);
            } else {
                prefix = qname.Prefix;
            }

            outputScope.PushScope();

            // Declare all namespaces that should be declared
            // <spec>http://www.w3.org/TR/xslt.html#literal-result-element</spec>
            QilList nsList = InstructionList();
            foreach (ScopeRecord rec in this.scope) {
                string recPrefix = rec.ncName;
                string recNsUri  = rec.nsUri;
                if (recNsUri != XmlReservedNs.NsXslt && !scope.IsExNamespace(recNsUri)) {
                    compiler.ApplyNsAliases(ref recPrefix, ref recNsUri);
                    if (changePrefixes) {
                        if (prefixesInUse.Contains(recPrefix)) {
                            if ((string)prefixesInUse[recPrefix] != recNsUri) {
                                // Found a prefix conflict. Start again from the beginning leaving all prefixes untouched.
                                outputScope.PopScope();
                                changePrefixes = false;
                                goto Start;
                            }
                        } else {
                            prefixesInUse.Add(recPrefix, recNsUri);
                        }
                    } else {
                        recPrefix = rec.ncName;
                    }
                    AddNsDecl(nsList, recPrefix, recNsUri);
                }
            }

            QilNode content = CompileInstructions(node.Content, nsList);
            outputScope.PopScope();

            qname.Prefix = prefix;
            qname.NamespaceUri = nsUri;
            return f.ElementCtor(qname, content);
        }

        private QilNode CompileElement(NodeCtor node) {
            QilNode qilNs   = CompileStringAvt(node.NsAvt);
            QilNode qilName = CompileStringAvt(node.NameAvt);
            QilNode qname;

            if (qilName.NodeType == QilNodeType.LiteralString && (qilNs == null || qilNs.NodeType == QilNodeType.LiteralString)) {
                string name = (string)(QilLiteral)qilName;
                string prefix, local, nsUri;

                bool isValid = compiler.ParseQName(name, out prefix, out local, (IErrorHelper)this);

                if (qilNs == null) {
                    nsUri = isValid ? ResolvePrefix(/*ignoreDefaultNs:*/false, prefix) : compiler.CreatePhantomNamespace();
                } else {
                    nsUri = (string)(QilLiteral)qilNs;
                }
                qname = f.QName(local, nsUri, prefix);
            } else {           // Process AVT
                if (qilNs != null) {
                    qname = f.StrParseQName(qilName, qilNs);
                } else {
                    qname = ResolveQNameDynamic(/*ignoreDefaultNs:*/false, qilName);
                }
            }

            outputScope.PushScope();
            // ToDo if we don't have AVT we shouldn't do this:
            this.outputScope.InvalidateAllPrefixes();
            QilNode content = CompileInstructions(node.Content);
            outputScope.PopScope();

            return f.ElementCtor(qname, content);
        }

        private QilNode CompileLiteralAttribute(XslNode node) {
            QilName qname   = node.Name;
            string  prefix  = qname.Prefix;
            string  nsUri   = qname.NamespaceUri;
            // The default namespace do not apply directly to attributes
            if (prefix.Length != 0) {
                compiler.ApplyNsAliases(ref prefix, ref nsUri);
            }
            qname.Prefix = prefix;
            qname.NamespaceUri = nsUri;
            return f.AttributeCtor(qname, CompileTextAvt(node.Select));
        }

        private QilNode CompileAttribute(NodeCtor node) {
            QilNode qilNs   = CompileStringAvt(node.NsAvt);
            QilNode qilName = CompileStringAvt(node.NameAvt);
            QilNode qname;
            bool    explicitNamespace = false;

            if (qilName.NodeType == QilNodeType.LiteralString && (qilNs == null || qilNs.NodeType == QilNodeType.LiteralString)) {
                string name  = (string)(QilLiteral)qilName;
                string prefix, local, nsUri;

                bool isValid = compiler.ParseQName(name, out prefix, out local, (IErrorHelper)this);

                if (qilNs == null) {
                    nsUri = isValid ? ResolvePrefix(/*ignoreDefaultNs:*/true, prefix) : compiler.CreatePhantomNamespace();
                } else {
                    nsUri = (string)(QilLiteral)qilNs;
                    // if both name and ns are non AVT and this ns is already bind to the same prefix we can avoid reseting ns management
                    explicitNamespace = true;
                }
                // Check the case <xsl:attribute name="foo:xmlns" namespace=""/>
                if (name == "xmlns" || local == "xmlns" && nsUri.Length == 0) {
                    ReportError(/*[XT_031]*/Res.Xslt_XmlnsAttr, "name", name);
                }
                qname = f.QName(local, nsUri, prefix);
            } else {
                // Process AVT
                if (qilNs != null) {
                    qname = f.StrParseQName(qilName, qilNs);
                } else {
                    qname = ResolveQNameDynamic(/*ignoreDefaultNs:*/true, qilName);
                }
            }
            if (explicitNamespace) {
                // Optimization: attribute cannot change the default namespace
                this.outputScope.InvalidateNonDefaultPrefixes();
            }
            return f.AttributeCtor(qname, CompileInstructions(node.Content));
        }

        private readonly StringBuilder unescapedText = new StringBuilder();

        private QilNode ExtractText(string source, ref int pos) {
            Debug.Assert(pos < source.Length);
            int i, start = pos;

            unescapedText.Length = 0;
            for (i = pos; i < source.Length; i++) {
                char ch = source[i];

                if (ch == '{' || ch == '}') {
                    if (i + 1 < source.Length && source[i + 1] == ch) {     // "{{" or "}}"
                        // Double curly brace outside an expression is replaced by a single one
                        i++;
                        unescapedText.Append(source, start, i - start);
                        start = i + 1;
                    } else if (ch == '{') {                                 // single '{'
                        // Expression encountered, returning
                        break;
                    } else {                                                // single '}'
                        // Single '}' outside an expression is an error
                        pos = source.Length;
                        if (xslVersion != XslVersion.ForwardsCompatible) {
                            ReportError(/*[XT0370]*/Res.Xslt_SingleRightBraceInAvt, source);
                            return null;
                        }
                        return f.Error(lastScope.SourceLine, Res.Xslt_SingleRightBraceInAvt, source);
                    }
                }
            }

            Debug.Assert(i == source.Length || source[i] == '{');
            pos = i;
            if (unescapedText.Length == 0) {
                return i > start ? f.String(source.Substring(start, i - start)) : null;
            } else {
                unescapedText.Append(source, start, i - start);
                return f.String(unescapedText.ToString());
            }
        }

        private QilNode CompileAvt(string source) {
            QilList result = f.BaseFactory.Sequence();
            int pos = 0;
            while (pos < source.Length) {
                QilNode fixedPart = ExtractText(source, ref pos);
                if (fixedPart != null) {
                    result.Add(fixedPart);
                }
                if (pos < source.Length) { // '{' encountered, parse an expression
                    pos++;
                    QilNode exp = CompileXPathExpressionWithinAvt(source, ref pos);
                    result.Add(f.ConvertToString(exp));
                }
            }
            if (result.Count == 1) {
                return result[0];
            }
            return result;
        }

        static readonly char[] curlyBraces = {'{', '}'};

        private QilNode CompileStringAvt(string avt) {
            if (avt == null) {
                return null;
            }
            if (avt.IndexOfAny(curlyBraces) == -1) {
                return f.String(avt);
            }
            return f.StrConcat(CompileAvt(avt));
        }

        private QilNode CompileTextAvt(string avt) {
            Debug.Assert(avt != null);
            if (avt.IndexOfAny(curlyBraces) == -1) {
                return f.TextCtor(f.String(avt));
            }
            QilNode avtParts = CompileAvt(avt);
            if (avtParts.NodeType == QilNodeType.Sequence) {
                QilList result = InstructionList();
                foreach (QilNode node in avtParts) {
                    result.Add(f.TextCtor(node));
                }
                return result;
            } else {
                return f.TextCtor(avtParts);
            }
        }

        private QilNode CompileText(Text node) {
            if (node.Hints == SerializationHints.None)
                return f.TextCtor(f.String(node.Select));

            return f.RawTextCtor(f.String(node.Select));
        }

        private QilNode CompilePI(XslNode node) {
            QilNode qilName = CompileStringAvt(node.Select);
            if (qilName.NodeType == QilNodeType.LiteralString) {
                string name = (string)(QilLiteral)qilName;
                compiler.ValidatePiName(name, (IErrorHelper)this);
            }
            return f.PICtor(qilName, CompileInstructions(node.Content));
        }

        private QilNode CompileComment(XslNode node) {
            return f.CommentCtor(CompileInstructions(node.Content));
        }

        private QilNode CompileError(XslNode node) {
            return f.Error(f.String(node.Select));
        }

        private QilNode WrapLoopBody(ISourceLineInfo before, QilNode expr, ISourceLineInfo after) {
            Debug.Assert(curLoop.IsFocusSet);
            if (IsDebug) {
                return f.Sequence(
                    SetLineInfo(InvokeOnCurrentNodeChanged(), before),
                    expr,
                    SetLineInfo(f.Nop(f.Sequence()), after)
                );
            }
            return expr;
        }

        private QilNode CompileForEach(XslNodeEx node) {
            QilNode         result;
            IList<XslNode>  content = node.Content;

            // Push new loop frame on the stack
            LoopFocus curLoopSaved = curLoop;
            QilIterator it = f.For(CompileNodeSetExpression(node.Select));
            curLoop.SetFocus(it);

            // Compile sort keys and body
            int varScope = varHelper.StartVariables();
            curLoop.Sort(CompileSorts(content, ref curLoopSaved));
            result = CompileInstructions(content);
            result = WrapLoopBody(node.ElemNameLi, result, node.EndTagLi);
            result = AddCurrentPositionLast(result);
            result = curLoop.ConstructLoop(result);
            result = varHelper.FinishVariables(result, varScope);

            // Pop loop frame
            curLoop = curLoopSaved;
            return result;
        }

        private QilNode CompileApplyTemplates(XslNodeEx node) {
            QilNode         result;
            IList<XslNode>  content = node.Content;

            // Calculate select expression
            int varScope = varHelper.StartVariables();

            QilIterator select = f.Let(CompileNodeSetExpression(node.Select));
            varHelper.AddVariable(select);

            // Compile with-param's, they must be calculated outside the loop and
            // if they are neither constant nor reference we need to cache them in Let's
            for (int i = 0; i < content.Count; i++) {
                VarPar withParam = content[i] as VarPar;
                if (withParam != null) {
                    Debug.Assert(withParam.NodeType == XslNodeType.WithParam);
                    CompileWithParam(withParam);
                    QilNode val = withParam.Value;
                    if (IsDebug || !(val is QilIterator || val is QilLiteral)) {
                        QilIterator let = f.Let(val);
                        let.DebugName = f.QName("with-param " + withParam.Name.QualifiedName, XmlReservedNs.NsXslDebug).ToString();
                        varHelper.AddVariable(let);
                        withParam.Value = let;
                    }
                }
            }

            // Push new loop frame on the stack
            LoopFocus curLoopSaved = curLoop;
            QilIterator it = f.For(select);
            curLoop.SetFocus(it);

            // Compile sort keys and body
            curLoop.Sort(CompileSorts(content, ref curLoopSaved));

            result = GenerateApply(compiler.Root, node);

            result = WrapLoopBody(node.ElemNameLi, result, node.EndTagLi);
            result = AddCurrentPositionLast(result);
            result = curLoop.ConstructLoop(result);

            // Pop loop frame
            curLoop = curLoopSaved;

            result = varHelper.FinishVariables(result, varScope);
            return result;
        }

        private QilNode CompileApplyImports(XslNode node) {
            Debug.Assert(node.NodeType == XslNodeType.ApplyImports);
            Debug.Assert(!curLoop.IsFocusSet, "xsl:apply-imports cannot be inside of xsl:for-each");

            return GenerateApply((StylesheetLevel)node.Arg, node);
        }

        private QilNode CompileCallTemplate(XslNodeEx node) {
            VerifyXPathQName(node.Name);
            int varScope = varHelper.StartVariables();

            IList<XslNode> content = node.Content;
            foreach (VarPar withParam in content) {
                CompileWithParam(withParam);
                // In debug mode precalculate all with-param's
                if (IsDebug) {
                    QilNode val = withParam.Value;
                    QilIterator let = f.Let(val);
                    let.DebugName = f.QName("with-param " + withParam.Name.QualifiedName, XmlReservedNs.NsXslDebug).ToString();
                    varHelper.AddVariable(let);
                    withParam.Value = let;
                }
            }

            QilNode result; {
                Template tmpl;
                if (compiler.NamedTemplates.TryGetValue(node.Name, out tmpl)) {
                    Debug.Assert(tmpl.Function != null, "All templates should be already compiled");
                    result = invkGen.GenerateInvoke(tmpl.Function, AddRemoveImplicitArgs(node.Content, tmpl.Flags));
                } else {
                    if (! compiler.IsPhantomName(node.Name)) {
                        compiler.ReportError(/*[XT0710]*/node.SourceLine, Res.Xslt_InvalidCallTemplate, node.Name.QualifiedName);
                    }
                    result = f.Sequence();
                }
            }

            // Do not create an additional sequence point if there are no parameters
            if (content.Count > 0) {
                result = SetLineInfo(result, node.ElemNameLi);
            }
            result = varHelper.FinishVariables(result, varScope);
            if (IsDebug) {
                return f.Nop(result);
            }
            return result;
        }

        private QilNode CompileUseAttributeSet(XslNode node) {
            VerifyXPathQName(node.Name);
            // 
            outputScope.InvalidateAllPrefixes();

            AttributeSet attSet;
            if (compiler.AttributeSets.TryGetValue(node.Name, out attSet)) {
                Debug.Assert(attSet.Function != null, "All templates should be already compiled");
                return invkGen.GenerateInvoke(attSet.Function, AddRemoveImplicitArgs(node.Content, attSet.Flags));
            } else {
                if (! compiler.IsPhantomName(node.Name)) {
                    compiler.ReportError(/*[XT0710]*/node.SourceLine, Res.Xslt_NoAttributeSet, node.Name.QualifiedName);
                }
                return f.Sequence();
            }
        }

        private const XmlNodeKindFlags InvalidatingNodes = (XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Namespace);

        private QilNode CompileCopy(XslNode copy) {
            QilNode node = GetCurrentNode();
            f.CheckNodeNotRtf(node);
            if ((node.XmlType.NodeKinds & InvalidatingNodes) != XmlNodeKindFlags.None) {
                outputScope.InvalidateAllPrefixes();
            }
            if (node.XmlType.NodeKinds == XmlNodeKindFlags.Element) {
                // Context node is always an element
                // The namespace nodes of the current node are automatically copied
                QilList content = InstructionList();
                content.Add(f.XPathNamespace(node));
                outputScope.PushScope();
                outputScope.InvalidateAllPrefixes();
                QilNode result = CompileInstructions(copy.Content, content);
                outputScope.PopScope();
                return f.ElementCtor(f.NameOf(node), result);
            } else if (node.XmlType.NodeKinds == XmlNodeKindFlags.Document) {
                // Context node is always a document
                // xsl:copy will not create a document node, but will just use the content template
                return CompileInstructions(copy.Content);
            } else if ((node.XmlType.NodeKinds & (XmlNodeKindFlags.Element | XmlNodeKindFlags.Document)) == XmlNodeKindFlags.None) {
                // Context node is neither an element, nor a document
                // The content of xsl:copy is not instantiated
                return node;
            } else {
                // Static classifying of the context node is not possible
                return f.XsltCopy(node, CompileInstructions(copy.Content));
            }
        }

        private QilNode CompileCopyOf(XslNode node) {
            QilNode selectExpr = CompileXPathExpression(node.Select);
            if (selectExpr.XmlType.IsNode) {
                if ((selectExpr.XmlType.NodeKinds & InvalidatingNodes) != XmlNodeKindFlags.None) {
                    outputScope.InvalidateAllPrefixes();
                }

                if (selectExpr.XmlType.IsNotRtf && (selectExpr.XmlType.NodeKinds & XmlNodeKindFlags.Document) == XmlNodeKindFlags.None) {
                    // Expression returns non-document nodes only
                    return selectExpr;
                }

                // May be an Rtf or may return Document nodes, so use XsltCopyOf operator
                if (selectExpr.XmlType.IsSingleton) {
                    return f.XsltCopyOf(selectExpr);
                } else {
                    QilIterator it;
                    return f.Loop(it = f.For(selectExpr), f.XsltCopyOf(it));
                }
            }
            else if (selectExpr.XmlType.IsAtomicValue) {
                // Expression returns non-nodes only
                // When the result is neither a node-set nor a result tree fragment, the result is converted
                // to a string and then inserted into the result tree, as with xsl:value-of.
                return f.TextCtor(f.ConvertToString(selectExpr));
            }
            else {
                // Static classifying is not possible
                QilIterator it;
                outputScope.InvalidateAllPrefixes();
                return f.Loop(
                    it = f.For(selectExpr),
                    f.Conditional(f.IsType(it, T.Node),
                        f.XsltCopyOf(f.TypeAssert(it, T.Node)),
                        f.TextCtor(f.XsltConvert(it, T.StringX))
                    )
                );
            }
        }

        private QilNode CompileValueOf(XslNode valueOf) {
            return f.TextCtor(f.ConvertToString(CompileXPathExpression(/*select:*/valueOf.Select)));
        }

        private QilNode CompileValueOfDoe(XslNode valueOf) {
            return f.RawTextCtor(f.ConvertToString(CompileXPathExpression(/*select:*/valueOf.Select)));
        }

        private QilNode CompileWhen(XslNode whenNode, QilNode otherwise) {
            return f.Conditional(
                f.ConvertToBoolean(CompileXPathExpression(/*test:*/whenNode.Select)),
                CompileInstructions(whenNode.Content),
                otherwise
            );
        }

        private QilNode CompileIf(XslNode ifNode) {
            return CompileWhen(ifNode, InstructionList());
        }

        private QilNode CompileChoose(XslNode node) {
            IList<XslNode> cases = node.Content;
            QilNode result = null;

            // It's easier to compile xsl:choose from bottom to top
            for (int i = cases.Count - 1; 0 <= i; i--) {
                XslNode when = cases[i];
                Debug.Assert(when.NodeType == XslNodeType.If || when.NodeType == XslNodeType.Otherwise);
                QilList nsList = EnterScope(when);
                if (when.NodeType == XslNodeType.Otherwise) {
                    Debug.Assert(result == null, "xsl:otherwise must be the last child of xsl:choose");
                    result = CompileInstructions(when.Content);
                } else {
                    result = CompileWhen(when, /*otherwise:*/result ?? InstructionList());
                }
                ExitScope();
                SetLineInfoCheck(result, when.SourceLine);
                result = SetDebugNs(result, nsList);
            }
            if (result == null) {
                return f.Sequence();
            }
            return IsDebug ? f.Sequence(result) : result;
        }

        private QilNode CompileMessage(XslNode node) {
            string  baseUri = lastScope.SourceLine.Uri;
            QilNode content = f.RtfCtor(CompileInstructions(node.Content), f.String(baseUri));

            //content = f.ConvertToString(content);
            content = f.InvokeOuterXml(content);

            // If terminate="no", then create QilNodeType.Warning
            if (!(bool)node.Arg) {
                return f.Warning(content);
            }

            // Otherwise create both QilNodeType.Warning and QilNodeType.Error
            QilIterator i;
            return f.Loop(i = f.Let(content), f.Sequence(f.Warning(i), f.Error(i)));
        }

        private QilNode CompileVariable(XslNode node) {
            Debug.Assert(node.NodeType == XslNodeType.Variable);
            if (scope.IsLocalVariable(node.Name.LocalName, node.Name.NamespaceUri)) {
                ReportError(/*[XT_030]*/Res.Xslt_DupLocalVariable, node.Name.QualifiedName);
            }
            return CompileVarParValue(node);
        }

        private QilNode CompileVarParValue(XslNode node) {
            Debug.Assert(node.NodeType == XslNodeType.Variable || node.NodeType == XslNodeType.Param || node.NodeType == XslNodeType.WithParam);
            VerifyXPathQName(node.Name);

            string          baseUri = lastScope.SourceLine.Uri;
            IList<XslNode>  content = node.Content;
            string          select  = node.Select;
            QilNode         varValue;

            if (select != null) {
                // In case of incorrect stylesheet, variable or parameter may have both a 'select' attribute and non-empty content
                QilList list = InstructionList();
                list.Add(CompileXPathExpression(select));
                varValue = CompileInstructions(content, list);
            } else if (content.Count != 0) {
                this.outputScope.PushScope();
                // Rtf will be instantiated in an unknown namespace context, so we should not assume anything here
                this.outputScope.InvalidateAllPrefixes();
                varValue = f.RtfCtor(CompileInstructions(content), f.String(baseUri));
                this.outputScope.PopScope();
            } else {
                varValue = f.String(string.Empty);
            }
            if (IsDebug) {
                // In debug mode every variable/param must be of type 'any'
                varValue = f.TypeAssert(varValue, T.ItemS);
            }
            Debug.Assert(varValue.SourceLine == null);
            return varValue;
        }

        private void CompileWithParam(VarPar withParam) {
            Debug.Assert(withParam.NodeType == XslNodeType.WithParam);
            QilList nsList = EnterScope(withParam);
            QilNode paramValue = CompileVarParValue(withParam);
            ExitScope();
            SetLineInfo(paramValue, withParam.SourceLine);
            paramValue = SetDebugNs(paramValue, nsList);
            withParam.Value = paramValue;
        }

        // 

        private QilNode CompileSorts(IList<XslNode> content, ref LoopFocus parentLoop) {
            QilList keyList = f.BaseFactory.SortKeyList();

            int i = 0;

            while (i < content.Count) {
                Sort sort = content[i] as Sort;
                if (sort != null) {
                    CompileSort(sort, keyList, ref parentLoop);
                    content.RemoveAt(i);
                } else {
                    i++;
                }
            }

            if (keyList.Count == 0)
                return null;

            return keyList;
        }

        private QilNode CompileLangAttribute(string attValue, bool fwdCompat) {
            QilNode result = CompileStringAvt(attValue);

            if (result == null) {
                // Do nothing
            } else if (result.NodeType == QilNodeType.LiteralString) {
                string lang = (string)(QilLiteral)result;
                int lcid = XsltLibrary.LangToLcidInternal(lang, fwdCompat, (IErrorHelper)this);
                if (lcid == XsltLibrary.InvariantCultureLcid) {
                    result = null;
                }
            } else {
                // NOTE: We should have the same checks for both compile time and execution time
                QilIterator i;
                result = f.Loop(i = f.Let(result),
                    f.Conditional(f.Eq(f.InvokeLangToLcid(i, fwdCompat), f.Int32(XsltLibrary.InvariantCultureLcid)),
                        f.String(string.Empty),
                        i
                    )
                );
            }
            return result;
        }

        private QilNode CompileLangAttributeToLcid(string attValue, bool fwdCompat) {
            return CompileLangToLcid(CompileStringAvt(attValue), fwdCompat);
         }

        private QilNode CompileLangToLcid(QilNode lang, bool fwdCompat) {
            if (lang == null) {
                return f.Double(XsltLibrary.InvariantCultureLcid);
            } else if (lang.NodeType == QilNodeType.LiteralString) {
                return f.Double(XsltLibrary.LangToLcidInternal((string)(QilLiteral)lang, fwdCompat, (IErrorHelper)this));
            } else {
                return f.XsltConvert(f.InvokeLangToLcid(lang, fwdCompat), T.DoubleX);
            }
        }

        private void CompileDataTypeAttribute(string attValue, bool fwdCompat, ref QilNode select, out QilNode select2) {
            const string DtText   = "text";
            const string DtNumber = "number";
            QilNode result = CompileStringAvt(attValue);
            if (result != null) {
                if (result.NodeType == QilNodeType.LiteralString) {
                    string dataType = (string)(QilLiteral)result;
                    if (dataType == DtNumber) {
                        select  = f.ConvertToNumber(select);
                        select2 = null;
                        return;
                    } else if (dataType == DtText) {
                        // fall through to default case
                    } else {
                        if (!fwdCompat) {
                            // check for qname-but-not-ncname
                            string prefix, local, nsUri;
                            bool isValid = compiler.ParseQName(dataType, out prefix, out local, (IErrorHelper)this);
                            nsUri = isValid ? ResolvePrefix(/*ignoreDefaultNs:*/true, prefix) : compiler.CreatePhantomNamespace();

                            if (nsUri.Length == 0) {
                                // this is a ncname; we might report Res.Xslt_InvalidAttrValue,
                                // but the following error message is more user friendly
                            }
                            ReportError(/*[XT_034]*/Res.Xslt_BistateAttribute, "data-type", DtText, DtNumber);
                        }
                        // fall through to default case
                    }
                } else {
                    // Precalculate its value outside of for-each loop
                    QilIterator dt, qname;

                    result = f.Loop(dt = f.Let(result),
                        f.Conditional(f.Eq(dt, f.String(DtNumber)), f.False(),
                        f.Conditional(f.Eq(dt, f.String(DtText)),   f.True(),
                        fwdCompat ? f.True() :
                        f.Loop(qname = f.Let(ResolveQNameDynamic(/*ignoreDefaultNs:*/true, dt)),
                            f.Error(lastScope.SourceLine,
                                Res.Xslt_BistateAttribute, "data-type", DtText, DtNumber
                            )
                        )
                    )));

                    QilIterator text = f.Let(result);
                    varHelper.AddVariable(text);

                    // Make two sort keys since heterogenous sort keys are not allowed
                    select2 = select.DeepClone(f.BaseFactory);
                    select  = f.Conditional(text, f.ConvertToString(select), f.String(string.Empty)    );
                    select2 = f.Conditional(text, f.Double(0),               f.ConvertToNumber(select2));
                    return;
                }
            }

            // Default case
            select  = f.ConvertToString(select);
            select2 = null;
        }

        /// <summary>
        /// Compiles AVT with two possible values
        /// </summary>
        /// <param name="attName"  >NodeCtor name (used for constructing an error message)</param>
        /// <param name="attValue" >NodeCtor value</param>
        /// <param name="value0"   >First possible value of attribute</param>
        /// <param name="value1"   >Second possible value of attribute</param>
        /// <param name="fwdCompat">If true, unrecognized value does not report an error</param>
        /// <returns>
        /// If AVT is null (i.e. the attribute is omitted), null is returned. Otherwise, QilExpression
        /// returning "1" if AVT evaluates to value1, or "0" if AVT evaluates to value0 or any other value.
        /// If AVT evaluates to neither value0 nor value1 and fwdCompat == false, an error is reported.
        /// </returns>
        private QilNode CompileOrderAttribute(string attName, string attValue, string value0, string value1, bool fwdCompat) {
            QilNode result = CompileStringAvt(attValue);
            if (result != null) {
                if (result.NodeType == QilNodeType.LiteralString) {
                    string value = (string)(QilLiteral)result;
                    if (value == value1) {
                        result = f.String("1");
                    } else {
                        if (value != value0 && !fwdCompat) {
                            ReportError(/*[XT_034]*/Res.Xslt_BistateAttribute, attName, value0, value1);
                        }
                        result = f.String("0");
                    }
                } else {
                    QilIterator i;
                    result = f.Loop(i = f.Let(result),
                        f.Conditional(f.Eq(i, f.String(value1)), f.String("1"),
                        fwdCompat ? f.String("0") :
                        f.Conditional(f.Eq(i, f.String(value0)), f.String("0"),
                        f.Error(lastScope.SourceLine,
                            Res.Xslt_BistateAttribute, attName, value0, value1
                        )
                    )));
                }
                Debug.Assert(result.XmlType == T.StringX);
            }
            return result;
        }

        private void CompileSort(Sort sort, QilList keyList, ref LoopFocus parentLoop) {
            Debug.Assert(sort.NodeType == XslNodeType.Sort);
            QilNode select, select2, lang, order, caseOrder;
            bool    fwdCompat;

            EnterScope(sort);
            fwdCompat = sort.ForwardsCompatible;

            select = CompileXPathExpression(sort.Select);

            if (sort.Lang != null || sort.DataType != null || sort.Order != null || sort.CaseOrder != null) {
                // Calculate these attributes in the context of the parent loop
                LoopFocus curLoopSaved = curLoop;
                curLoop = parentLoop;

                lang = CompileLangAttribute(sort.Lang, fwdCompat);

                CompileDataTypeAttribute(sort.DataType, fwdCompat, ref select, out select2);

                order = CompileOrderAttribute(
                    /*attName:  */  "order",
                    /*attValue: */  sort.Order,
                    /*value0:   */  "ascending",
                    /*value1:   */  "descending",
                    /*fwdCompat:*/  fwdCompat
                );

                caseOrder = CompileOrderAttribute(
                    /*attName:  */  "case-order",
                    /*attValue: */  sort.CaseOrder,
                    /*value0:   */  "lower-first",
                    /*value1:   */  "upper-first",
                    /*fwdCompat:*/  fwdCompat
                );

                // Restore loop context
                curLoop = curLoopSaved;
            } else {
                select  = f.ConvertToString(select);
                select2 = lang = order = caseOrder = null;
            }

            strConcat.Reset();
            strConcat.Append(XmlReservedNs.NsCollationBase);
            strConcat.Append('/');
            strConcat.Append(lang);

            char separator = '?';
            if (order != null) {
                strConcat.Append(separator);
                strConcat.Append("descendingOrder=");
                strConcat.Append(order);
                separator = '&';
            }
            if (caseOrder != null) {
                strConcat.Append(separator);
                strConcat.Append("upperFirst=");
                strConcat.Append(caseOrder);
                separator = '&';
            }

            QilNode collation = strConcat.ToQil();

            QilSortKey result = f.SortKey(select, collation);
            // Line information is not copied
            keyList.Add(result);

            if (select2 != null) {
                result = f.SortKey(select2, collation.DeepClone(f.BaseFactory));
                // Line information is not copied
                keyList.Add(result);
            }

            ExitScope();
        }

        private QilNode MatchPattern(QilNode pattern, QilIterator testNode) {
            QilList list;
            if (pattern.NodeType == QilNodeType.Error) {
                // Invalid pattern
                return pattern;
            } else if (pattern.NodeType == QilNodeType.Sequence) {
                list = (QilList)pattern;
                Debug.Assert(0 < list.Count, "Pattern should have at least one filter");
            } else {
                list = f.BaseFactory.Sequence();
                list.Add(pattern);
            }

            QilNode result = f.False();
            for (int i = list.Count - 1; 0 <= i; i --) {
                QilLoop filter = (QilLoop) list[i];
                ptrnBuilder.AssertFilter(filter);
                result = f.Or(
                    refReplacer.Replace(filter.Body, filter.Variable, testNode),
                    result
                );
            }
            return result;
        }

        private QilNode MatchCountPattern(QilNode countPattern, QilIterator testNode) {
            /*
                If the 'count' attribute is not specified, then it defaults to the pattern that matches any node
                with the same node kind as the context node and, if the context node has an expanded-QName, with
                the same expanded-QName as the context node.
            */
            if (countPattern != null) {
                return MatchPattern(countPattern, testNode);
            } else {
                QilNode current = GetCurrentNode();
                QilNode result;
                XmlNodeKindFlags nodeKinds = current.XmlType.NodeKinds;

                // If node kind is not known, invoke a runtime function
                if ((nodeKinds & (nodeKinds - 1)) != 0) {
                    return f.InvokeIsSameNodeSort(testNode, current);
                }

                // Otherwise generate IsType check along with expanded QName check
                switch (nodeKinds) {
                case XmlNodeKindFlags.Document  :   return f.IsType(testNode, T.Document);
                case XmlNodeKindFlags.Element   :   result = f.IsType(testNode, T.Element);   break;
                case XmlNodeKindFlags.Attribute :   result = f.IsType(testNode, T.Attribute); break;
                case XmlNodeKindFlags.Text      :   return f.IsType(testNode, T.Text);
                case XmlNodeKindFlags.Comment   :   return f.IsType(testNode, T.Comment);
                case XmlNodeKindFlags.PI        :   return f.And(f.IsType(testNode, T.PI)       , f.Eq(f.LocalNameOf(testNode), f.LocalNameOf(current)));
                case XmlNodeKindFlags.Namespace :   return f.And(f.IsType(testNode, T.Namespace), f.Eq(f.LocalNameOf(testNode), f.LocalNameOf(current)));
                default :
                    Debug.Fail("Unexpected NodeKind: " + nodeKinds.ToString());
                    return f.False();
                }

                // Elements and attributes have both local name and namespace URI
                return f.And(result, f.And(
                    f.Eq(f.LocalNameOf(testNode)   , f.LocalNameOf(current)),
                    f.Eq(f.NamespaceUriOf(testNode), f.NamespaceUriOf(GetCurrentNode()))
                ));
            }
        }

        private QilNode PlaceMarker(QilNode countPattern, QilNode fromPattern, bool multiple) {
            /*
                Quotation from XSLT 2.0 spec:
                * Let $A be the node sequence selected by the expression
                    ancestor-or-self::node()[matches-count(.)]          (level = "multiple")
                    ancestor-or-self::node()[matches-count(.)][1]       (level = "single")
                * Let $F be the node sequence selected by the expression
                    ancestor-or-self::node()[matches-from(.)][1]
                * Let $AF be the value of
                    $A intersect ($F/descendant-or-self::node())
                * Return the result of the expression
                    for $af in $AF return 1+count($af/preceding-sibling::node()[matches-count(.)])

                NOTE: There are some distinctions between XSLT 1.0 and XSLT 2.0 specs. In our 1.0 implementation we:
                1) Assume that the 'matches-from()' function does not match root nodes by default.
                2) Instead of '$A intersect ($F/descendant-or-self::node())' (which, by the way,
                   would filter out attribute and namespace nodes from $A) we calculate
                     '$A'           if the 'from' attribute is omitted,
                     '$A[. >> $F]'  if the 'from' attribute is present.
            */

            QilNode     countPattern2, countMatches, fromMatches, A, F, AF;
            QilIterator i, j;

            countPattern2 = (countPattern != null) ? countPattern.DeepClone(f.BaseFactory) : null;
            countMatches = f.Filter(i = f.For(f.AncestorOrSelf(GetCurrentNode())), MatchCountPattern(countPattern, i));
            if (multiple) {
                A = f.DocOrderDistinct(countMatches);
            } else {
                A = f.Filter(i = f.For(countMatches), f.Eq(f.PositionOf(i), f.Int32(1)));
            }

            if (fromPattern == null) {
                AF = A;
            } else {
                fromMatches = f.Filter(i = f.For(f.AncestorOrSelf(GetCurrentNode())), MatchPattern(fromPattern, i));
                F = f.Filter(i = f.For(fromMatches), f.Eq(f.PositionOf(i), f.Int32(1)));
                AF = f.Loop(i = f.For(F), f.Filter(j = f.For(A), f.Before(i, j)));
            }

            return f.Loop(j = f.For(AF),
                f.Add(f.Int32(1), f.Length(f.Filter(i = f.For(f.PrecedingSibling(j)), MatchCountPattern(countPattern2, i))))
            );
        }

        private QilNode PlaceMarkerAny(QilNode countPattern, QilNode fromPattern) {
            /*
                Quotation from XSLT 2.0 spec:
                * If the context node is a document node, return the empty sequence, ()
                * Let $A be the node sequence selected by the expression
                    (preceding::node()|ancestor-or-self::node())[matches-count(.)]
                * Let $F be the node sequence selected by the expression
                    (preceding::node()|ancestor::node())[matches-from(.)][last()]
                * Let $AF be the node sequence $A[. is $F or . >> $F].
                * If $AF is empty, return the empty sequence, ()
                * Otherwise return the value of the expression count($AF)

                NOTE: There are some distinctions between XSLT 1.0 and XSLT 2.0 specs. In our 1.0 implementation we:
                1) Assume that the 'matches-from()' function does not match root nodes by default.
                2) Instead of '$A[. is $F or . >> $F]' we calculate
                     '$A'           if the 'from' attribute is omitted,
                     '$A[. >> $F]'  if the 'from' attribute is present.
            */

            QilNode     range, fromMatches, F, AF;
            QilIterator i, j, k;

            if (fromPattern == null) {
                // According to XSLT 2.0 spec, if the 'from' attribute is omitted, matches-from() returns true
                // only for the root node. It means $F is a sequence of length one containing the root node,
                // and $AF = $A. XSLT 1.0 spec rules lead to the same result $AF = $A, so two specs are compliant here.
                range = f.NodeRange(f.Root(GetCurrentNode()), GetCurrentNode());
                AF = f.Filter(i = f.For(range), MatchCountPattern(countPattern, i));
            } else {
                fromMatches = f.Filter(i = f.For(f.Preceding(GetCurrentNode())), MatchPattern(fromPattern, i));
                F = f.Filter(i = f.For(fromMatches), f.Eq(f.PositionOf(i), f.Int32(1)));
                AF = f.Loop(i = f.For(F),
                    f.Filter(j = f.For(f.Filter(k = f.For(f.NodeRange(i, GetCurrentNode())), MatchCountPattern(countPattern, k))),
                        f.Not(f.Is(i, j))
                    )
                );
            }

            return f.Loop(k = f.Let(f.Length(AF)),
                f.Conditional(f.Eq(k, f.Int32(0)), f.Sequence(),
                k
            ));
        }

        // Returns one of XsltLibrary.LetterValue enum values
        private QilNode CompileLetterValueAttribute(string attValue, bool fwdCompat) {
            const string Default     = "default";
            const string Alphabetic  = "alphabetic";
            const string Traditional = "traditional";

            string letterValue;
            QilNode result = CompileStringAvt(attValue);

            if (result != null) {
                if (result.NodeType == QilNodeType.LiteralString) {
                    letterValue = (string)(QilLiteral)result;
                    if (letterValue != Alphabetic && letterValue != Traditional) {
                        if (!fwdCompat) {
                            ReportError(/*[XT_034]*/Res.Xslt_BistateAttribute, "letter-value", Alphabetic, Traditional);
                        }
                        else {
                            // Use default value
                            return f.String(Default);
                        }
                    }
                    return result;
                } else {
                    QilIterator i = f.Let(result);
                    return f.Loop(i,
                        f.Conditional(
                            f.Or(f.Eq(i, f.String(Alphabetic)), f.Eq(i, f.String(Traditional))),
                            i,
                            fwdCompat ? f.String(Default) :
                            f.Error(lastScope.SourceLine, Res.Xslt_BistateAttribute, "letter-value", Alphabetic, Traditional)
                    ));
                }
            }
            return f.String(Default);
        }

        private QilNode CompileGroupingSeparatorAttribute(string attValue, bool fwdCompat) {
            QilNode result = CompileStringAvt(attValue);

            if (result == null) {
                // NOTE: string.Empty value denotes unspecified attribute
                result = f.String(string.Empty);
            } else if (result.NodeType == QilNodeType.LiteralString) {
                string value = (string)(QilLiteral)result;
                if (value.Length != 1) {
                    if (!fwdCompat) {
                        ReportError(/*[XT_035]*/Res.Xslt_CharAttribute, "grouping-separator");
                    }
                    // See the comment above
                    result = f.String(string.Empty);
                }
            } else {
                QilIterator i = f.Let(result);
                result = f.Loop(i,
                    f.Conditional(f.Eq(f.StrLength(i), f.Int32(1)), i,
                    fwdCompat ? f.String(string.Empty) :
                    f.Error(lastScope.SourceLine, Res.Xslt_CharAttribute, "grouping-separator")
                ));
            }
            return result;
        }

        private QilNode CompileGroupingSizeAttribute(string attValue, bool fwdCompat) {
            QilNode result = CompileStringAvt(attValue);

            if (result == null) {
                return f.Double(0);
            } else if (result.NodeType == QilNodeType.LiteralString) {
                string groupingSize = (string)(QilLiteral)result;

                // NOTE: It is unclear from the spec what we should do with float numbers here.
                // Let's apply XPath number and round functions as usual, suppressing any conversion errors.
                double dblGroupingSize = XsltFunctions.Round(XPathConvert.StringToDouble(groupingSize));
                if (0 <= dblGroupingSize && dblGroupingSize <= int.MaxValue) {
                    return f.Double(dblGroupingSize);
                }
                // NaN goes here as well
                return f.Double(0);
            } else {
                // NOTE: We should have the same checks for both compile time and execution time
                QilIterator i = f.Let(f.ConvertToNumber(result));
                return f.Loop(i,
                    f.Conditional(f.And(f.Lt(f.Double(0), i), f.Lt(i, f.Double(int.MaxValue))),
                        i,
                        f.Double(0)
                    )
                );
            }
        }

        private QilNode CompileNumber(Number num) {
            QilNode value;

            if (num.Value != null) {
                // 

                value = f.ConvertToNumber(CompileXPathExpression(num.Value));
            } else {
                QilNode countPattern = (num.Count != null) ? CompileNumberPattern(num.Count) : null;
                QilNode fromPattern  = (num.From  != null) ? CompileNumberPattern(num.From ) : null;

                switch (num.Level) {
                case NumberLevel.Single   : value = PlaceMarker(countPattern, fromPattern, false); break;
                case NumberLevel.Multiple : value = PlaceMarker(countPattern, fromPattern, true ); break;
                default:
                    Debug.Assert(num.Level == NumberLevel.Any);
                    value = PlaceMarkerAny(countPattern, fromPattern);
                    break;
                }
            }

            bool fwdCompat = num.ForwardsCompatible;
            return f.TextCtor(f.InvokeNumberFormat(
                value, CompileStringAvt(num.Format),
                CompileLangAttributeToLcid       (num.Lang,              fwdCompat),
                CompileLetterValueAttribute      (num.LetterValue,       fwdCompat),
                CompileGroupingSeparatorAttribute(num.GroupingSeparator, fwdCompat),
                CompileGroupingSizeAttribute     (num.GroupingSize,      fwdCompat)
            ));
        }

        // ------------- CompileAndSortMatchPatterns() -------------

        private void CompileAndSortMatches(Stylesheet sheet) {
            Debug.Assert(sheet.TemplateMatches.Count == 0);

            foreach (Template template in sheet.Templates) {
                if (template.Match != null) {
                    EnterScope(template);
                    QilNode result = CompileMatchPattern(template.Match);
                    if (result.NodeType == QilNodeType.Sequence) {
                        QilList filters = (QilList)result;
                        for (int idx = 0; idx < filters.Count; idx++) {
                            sheet.AddTemplateMatch(template, (QilLoop)filters[idx]);
                        }
                    } else {
                        sheet.AddTemplateMatch(template, (QilLoop)result);
                    }
                    ExitScope();
                }
            }

            sheet.SortTemplateMatches();

            foreach (Stylesheet import in sheet.Imports) {
                CompileAndSortMatches(import);
            }
        }

        // ------------- CompileKeys() -------------

        private void CompileKeys() {
            CheckSingletonFocus();
            for (int idx = 0; idx < compiler.Keys.Count; idx++) {
                foreach (Key key in compiler.Keys[idx]) {
                    EnterScope(key);
                    QilParameter context = f.Parameter(T.NodeNotRtf);
                    singlFocus.SetFocus(context);
                    QilIterator values  = f.For(f.OptimizeBarrier(CompileKeyMatch(key.Match)));
                    singlFocus.SetFocus(values);
                    QilIterator keys = f.For(CompileKeyUse(key));
                    keys = f.For(f.OptimizeBarrier(f.Loop(keys, f.ConvertToString(keys))));

                    QilParameter value = f.Parameter(T.StringX);

                    QilFunction func = f.Function(f.FormalParameterList(context, value),
                        f.Filter(values,
                            f.Not(f.IsEmpty(f.Filter(keys, f.Eq(keys, value))))
                        ),
                        f.False()
                    );

                    func.DebugName = key.GetDebugName();
                    SetLineInfo(func, key.SourceLine);
                    key.Function = func;
                    this.functions.Add(func);
                    ExitScope();
                }
            }
            singlFocus.SetFocus(null);
        }

        // ---------------------- Global variables and parameters -----------------------

        private void CreateGlobalVarPars() {
            foreach (VarPar par in compiler.ExternalPars) {
                CreateGlobalVarPar(par);
            }
            foreach (VarPar var in compiler.GlobalVars) {
                CreateGlobalVarPar(var);
            }
        }

        private void CreateGlobalVarPar(VarPar varPar) {
            Debug.Assert(varPar.NodeType == XslNodeType.Variable || varPar.NodeType == XslNodeType.Param);
            XmlQueryType xt = ChooseBestType(varPar);
            QilIterator it;
            if (varPar.NodeType == XslNodeType.Variable) {
                it = f.Let(f.Unknown(xt));
            } else {
                it = f.Parameter(null, varPar.Name, xt);
            }
            it.DebugName = varPar.Name.ToString();
            varPar.Value = it;
            SetLineInfo(it, varPar.SourceLine);
            scope.AddVariable(varPar.Name, it);
        }

        private void CompileGlobalVariables() {
            CheckSingletonFocus();
            singlFocus.SetFocus(SingletonFocusType.InitialDocumentNode);

            foreach (VarPar par in compiler.ExternalPars) {
                extPars.Add(CompileGlobalVarPar(par));
            }
            foreach (VarPar var in compiler.GlobalVars) {
                gloVars.Add(CompileGlobalVarPar(var));
            }

            singlFocus.SetFocus(null);
        }

        private QilIterator CompileGlobalVarPar(VarPar varPar) {
            Debug.Assert(varPar.NodeType == XslNodeType.Variable || varPar.NodeType == XslNodeType.Param);
            QilIterator it = (QilIterator)varPar.Value;

            QilList nsList = EnterScope(varPar);
            QilNode content = CompileVarParValue(varPar);
            SetLineInfo(content, it.SourceLine);
            content = AddCurrentPositionLast(content);
            content = SetDebugNs(content, nsList);
            it.SourceLine = SourceLineInfo.NoSource;
            it.Binding = content;
            ExitScope();
            return it;
        }

        // ------------- CompileXPathExpression() / CompileMatchPattern() / CompileKeyPattern() -----------

        private void ReportErrorInXPath(XslLoadException e) {
            XPathCompileException ex = e as XPathCompileException;
            string errorText = (ex != null) ? ex.FormatDetailedMessage() : e.Message;
            compiler.ReportError(lastScope.SourceLine, Res.Xml_UserException, errorText);
        }

        private QilNode PhantomXPathExpression() {
            return f.TypeAssert(f.Sequence(), T.ItemS);
        }

        private QilNode PhantomKeyMatch() {
            return f.TypeAssert(f.Sequence(), T.NodeNotRtfS);
        }

        // Calls to CompileXPathExpression() can't be nested in the XSLT. So we can reuse the same instance of xpathBuilder.
        // The only thing we need to do before its use is adjustment of IXPathEnvironment to have correct context tuple.
        private QilNode CompileXPathExpression(string expr) {
            XPathScanner    scanner;
            QilNode         result;

            SetEnvironmentFlags(/*allowVariables:*/true, /*allowCurrent:*/true, /*allowKey:*/true);
            if (expr == null) {
                result = PhantomXPathExpression();
            } else {
                try {
                    // Note that the constructor may throw an exception, for example, in case of the expression "'"
                    scanner = new XPathScanner(expr);
                    result = xpathParser.Parse(scanner, xpathBuilder, LexKind.Eof);
                } catch (XslLoadException e) {
                    if (xslVersion != XslVersion.ForwardsCompatible) {
                        ReportErrorInXPath(/*[XT0300]*/e);
                    }
                    result = f.Error(f.String(e.Message));
                }
            }
            if (result is QilIterator) {
                result = f.Nop(result);
            }
            return result;
        }

        private QilNode CompileNodeSetExpression(string expr) {
            QilNode result = f.TryEnsureNodeSet(CompileXPathExpression(expr));
            if (result == null) {
                // The expression is never a node-set
                XPathCompileException e = new XPathCompileException(expr, 0, expr.Length, Res.XPath_NodeSetExpected, null);
                if (xslVersion != XslVersion.ForwardsCompatible) {
                    ReportErrorInXPath(/*[XTTE_101]*/e);
                }
                result = f.Error(f.String(e.Message));
            }
            return result;
        }

        private QilNode CompileXPathExpressionWithinAvt(string expr, ref int pos) {
            Debug.Assert(expr != null);
            XPathScanner    scanner;
            QilNode         result;
            int             startPos = pos;

            SetEnvironmentFlags(/*allowVariables:*/true, /*allowCurrent:*/true, /*allowKey:*/true);
            try {
                scanner = new XPathScanner(expr, pos);
                result = xpathParser.Parse(scanner, xpathBuilder, LexKind.RBrace);
                pos = scanner.LexStart + 1;
            } catch (XslLoadException e) {
                if (xslVersion != XslVersion.ForwardsCompatible) {
                    ReportErrorInXPath(/*[XT0350][XT0360]*/e);
                }
                result = f.Error(f.String(e.Message));
                pos = expr.Length;
            }
            if (result is QilIterator) {
                result = f.Nop(result);
            }
            return result;
        }

        private QilNode CompileMatchPattern(string pttrn) {
            Debug.Assert(pttrn != null);
            XPathScanner    scanner;
            QilNode         result;

            SetEnvironmentFlags(/*allowVariables:*/false, /*allowCurrent:*/false, /*allowKey:*/true);
            try {
                scanner = new XPathScanner(pttrn);
                result = ptrnParser.Parse(scanner, ptrnBuilder);
            } catch (XslLoadException e) {
                if (xslVersion != XslVersion.ForwardsCompatible) {
                    ReportErrorInXPath(/*[XT0340]*/e);
                }
                result = f.Loop(f.For(ptrnBuilder.FixupNode),
                    f.Error(f.String(e.Message))
                );
                XPathPatternBuilder.SetPriority(result, 0.5);
            }
            return result;
        }

        private QilNode CompileNumberPattern(string pttrn) {
            Debug.Assert(pttrn != null);
            XPathScanner    scanner;
            QilNode         result;

            SetEnvironmentFlags(/*allowVariables:*/true, /*allowCurrent:*/false, /*allowKey:*/true);
            try {
                scanner = new XPathScanner(pttrn);
                result = ptrnParser.Parse(scanner, ptrnBuilder);
            } catch (XslLoadException e) {
                if (xslVersion != XslVersion.ForwardsCompatible) {
                    ReportErrorInXPath(/*[XT0340]*/e);
                }
                result = f.Error(f.String(e.Message));
            }
            return result;
        }

        private QilNode CompileKeyMatch(string pttrn) {
            XPathScanner    scanner;
            QilNode         result;

            if (keyMatchBuilder == null) {
                keyMatchBuilder = new KeyMatchBuilder((IXPathEnvironment) this);
            }
            SetEnvironmentFlags(/*allowVariables:*/false, /*allowCurrent:*/false, /*allowKey:*/false);
            if (pttrn == null) {
                result = PhantomKeyMatch();
            } else {
                try {
                    scanner = new XPathScanner(pttrn);
                    result = ptrnParser.Parse(scanner, keyMatchBuilder);
                } catch (XslLoadException e) {
                    if (xslVersion != XslVersion.ForwardsCompatible) {
                        ReportErrorInXPath(/*[XT0340]*/e);
                    }
                    result = f.Error(f.String(e.Message));
                }
            }
            return result;
        }

        private QilNode CompileKeyUse(Key key) {
            string          expr = key.Use;
            XPathScanner    scanner;
            QilNode         result;

            SetEnvironmentFlags(/*allowVariables:*/false, /*allowCurrent:*/true, /*allowKey:*/false);
            if (expr == null) {
                result = f.Error(f.String(XslLoadException.CreateMessage(key.SourceLine, Res.Xslt_MissingAttribute, "use")));
            } else {
                try {
                    scanner = new XPathScanner(expr);
                    result = xpathParser.Parse(scanner, xpathBuilder, LexKind.Eof);
                } catch (XslLoadException e) {
                    if (xslVersion != XslVersion.ForwardsCompatible) {
                        ReportErrorInXPath(/*[XT0300]*/e);
                    }
                    result = f.Error(f.String(e.Message));
                }
            }
            if (result is QilIterator) {
                result = f.Nop(result);
            }
            return result;
        }

        private QilNode ResolveQNameDynamic(bool ignoreDefaultNs, QilNode qilName) {
            f.CheckString(qilName);
            QilList nsDecls = f.BaseFactory.Sequence();
            if (ignoreDefaultNs) {
                nsDecls.Add(f.NamespaceDecl(f.String(string.Empty), f.String(string.Empty)));
            }
            foreach (ScopeRecord rec in this.scope) {
                string recPrefix = rec.ncName;
                string recNsUri  = rec.nsUri;

                if (ignoreDefaultNs && recPrefix.Length == 0) {
                    // Do not take into account the default namespace
                } else {
                    nsDecls.Add(f.NamespaceDecl(f.String(recPrefix), f.String(recNsUri)));
                }
            }
            return f.StrParseQName(qilName, nsDecls);
        }

        // ----------------- apply-templates, apply-imports ----------------------------- //

        private QilNode GenerateApply(StylesheetLevel sheet, XslNode node) {
            Debug.Assert(
                node.NodeType == XslNodeType.ApplyTemplates && sheet is RootLevel ||
                node.NodeType == XslNodeType.ApplyImports   && sheet is Stylesheet
            );

            if (compiler.Settings.CheckOnly) {
                return f.Sequence();
            }
            return InvokeApplyFunction(sheet, /*mode:*/node.Name, node.Content);
        }

        private void SetArg(IList<XslNode> args, int pos, QilName name, QilNode value) {
            VarPar varPar;
            if (args.Count <= pos || args[pos].Name != name) {
                varPar = AstFactory.WithParam(name);
                args.Insert(pos, varPar); 
            } else {
                varPar = (VarPar) args[pos];
            }
            varPar.Value = value;
        }
        private IList<XslNode> AddRemoveImplicitArgs(IList<XslNode> args, XslFlags flags) {
            //We currently don't reuse the same argument list. So remove is not needed and will not work in this code
            if (IsDebug) {
                flags = XslFlags.FullFocus;
            }
            if ((flags & XslFlags.FocusFilter) != 0) {
                if (args == null || args.IsReadOnly) {
                    args = new List<XslNode>(3);
                }
                int pos = 0;
                if ((flags & XslFlags.Current ) != 0) { SetArg(args, pos ++, nameCurrent , GetCurrentNode    ()); } 
                if ((flags & XslFlags.Position) != 0) { SetArg(args, pos ++, namePosition, GetCurrentPosition()); } 
                if ((flags & XslFlags.Last    ) != 0) { SetArg(args, pos ++, nameLast    , GetLastPosition   ()); } 
            }
            return args;
        }

        // Fills invokeArgs with values from actualArgs in order given by formalArgs
        // Returns true if formalArgs maps 1:1 with actual args.
        // Formaly this is n*n algorithm. We can optimize it by calculationg "signature"
        // of the function as sum of all hashes of its args names.
        private bool FillupInvokeArgs(IList<QilNode> formalArgs, IList<XslNode> actualArgs, QilList invokeArgs) {
            if (actualArgs.Count != formalArgs.Count) {
                return false;
            }
            invokeArgs.Clear();
            for (int invArg = 0; invArg < formalArgs.Count; invArg++) {
                QilName formalArgName = ((QilParameter)formalArgs[invArg]).Name;
                XmlQueryType paramType = formalArgs[invArg].XmlType;
                QilNode arg = null; {
                    for (int actArg = 0; actArg < actualArgs.Count; actArg++) {
                        Debug.Assert(actualArgs[actArg].NodeType == XslNodeType.WithParam, "All Sorts was removed in CompileSorts()");
                        VarPar withParam = (VarPar)actualArgs[actArg];
                        if (formalArgName.Equals(withParam.Name)) {
                            QilNode value = withParam.Value;
                            XmlQueryType valueType = value.XmlType;
                            if (valueType != paramType) {
                                if (valueType.IsNode && paramType.IsNode && valueType.IsSubtypeOf(paramType)) {
                                    // We can pass it
                                } else {
                                    // Formal argument has the same name but a different type
                                    return false;
                                }
                            }
                            arg = value;
                            break;
                        }
                    }
                }
                if (arg == null) {
                    // Formal argument has not been found among actual arguments
                    return false;
                }
                invokeArgs.Add(arg);
            }
            // All arguments have been found
            return true;
        }

        private QilNode InvokeApplyFunction(StylesheetLevel sheet, QilName mode, IList<XslNode> actualArgs) {
            // Here we create function that has one argument for each with-param in apply-templates
            // We have actualArgs -- list of xsl:with-param(name, value)
            // From it we create:
            // invokeArgs -- values to use with QilInvoke
            // formalArgs -- list of iterators to use with QilFunction
            // actualArgs -- modify it to hold iterators (formalArgs) instead of values to ise in invoke generator inside function budy

            XslFlags flags; { 
                if (! sheet.ModeFlags.TryGetValue(mode, out flags)) {
                    flags = 0;
                }
                flags |= XslFlags.Current; // Due to recursive nature of Apply(Templates/Imports) we will need current node any way
            }
            actualArgs = AddRemoveImplicitArgs(actualArgs, flags);

            QilList     invokeArgs = f.ActualParameterList();
            QilFunction applyFunction = null;

            // Look at the list of all functions that have been already built.  If a suitable one is found, reuse it.
            List<QilFunction> functionsForMode;
            if (!sheet.ApplyFunctions.TryGetValue(mode, out functionsForMode)) {
                functionsForMode = sheet.ApplyFunctions[mode] = new List<QilFunction>();
            }

            foreach (QilFunction func in functionsForMode) {
                if (FillupInvokeArgs(func.Arguments, actualArgs, /*ref*/invokeArgs)) {
                    applyFunction = func;
                    break;
                }
            }

            // If a suitable function has not been found, create it
            if (applyFunction == null) {
                invokeArgs.Clear();
                // We wasn't able to find suitable function. Let's build new:
                // 1. Function arguments
                QilList formalArgs = f.FormalParameterList();
                for (int i = 0; i < actualArgs.Count; i++) {
                    Debug.Assert(actualArgs[i].NodeType == XslNodeType.WithParam, "All Sorts was removed in CompileSorts()");
                    VarPar withParam = (VarPar)actualArgs[i] ;

                    // Add actual arg to 'invokeArgs' array. No need to clone it since it must be
                    // a literal or a reference.
                    invokeArgs.Add(withParam.Value);

                    // Create correspondent formal arg
                    QilParameter formalArg = f.Parameter(i == 0 ? T.NodeNotRtf : withParam.Value.XmlType);
                    formalArg.Name = CloneName(withParam.Name);
                    formalArgs.Add(formalArg);

                    // Change actual arg value to formalArg for reuse in calling built-in templates rules
                    withParam.Value = formalArg;
                }

                // 2. Function header
                applyFunction = f.Function(formalArgs,
                    f.Boolean((flags & XslFlags.SideEffects) != 0),
                    T.NodeNotRtfS
                );
                string attMode = (mode.LocalName.Length == 0) ? string.Empty : " mode=\"" + mode.QualifiedName + '"';
                applyFunction.DebugName = (sheet is RootLevel ? "<xsl:apply-templates" : "<xsl:apply-imports") + attMode + '>';
                functionsForMode.Add(applyFunction);
                this.functions.Add(applyFunction);

                // 3. Function body
                Debug.Assert(actualArgs[0].Name == nameCurrent, "Caller should always pass $current as a first argument to apply-* calls.");
                QilIterator current = (QilIterator)formalArgs[0];

                // 3.1 Built-in templates:
                // 3.1.1 loop over content of current element
                QilLoop loopOnContent; {
                    QilIterator iChild = f.For(f.Content(current));
                    QilNode filter = f.Filter(iChild, f.IsType(iChild, T.Content));
                    filter.XmlType = T.ContentS;    // not attribute

                    LoopFocus curLoopSaved = curLoop;
                    curLoop.SetFocus(f.For(filter));

                    /* Prepare actual arguments */
                    // At XSLT 1.0, if a built-in template rule is invoked with parameters, the parameters are not
                    // passed on to any templates invoked by the built-in rule. At XSLT 2.0, these parameters are
                    // passed through the built-in template rule unchanged.

                    // we can't just modify current/position/last of actualArgs in XSLT 2.0 as we tried before, 
                    // becuase flags for apply-import amy now be different then flags for apply-templates, so 
                    // we may need to add some space for additional position/last arguments
                    QilNode body = InvokeApplyFunction(compiler.Root, mode, /*actualArgs:*/null);
                    if (IsDebug) {
                        body = f.Sequence(InvokeOnCurrentNodeChanged(), body);
                    }
                    loopOnContent = curLoop.ConstructLoop(body);
                    curLoop = curLoopSaved;
                }

                // 3.1.2 switch on type of current node
                QilTernary builtinTemplates = f.BaseFactory.Conditional(f.IsType(current, elementOrDocumentType),                    
                    loopOnContent,
                    f.Conditional(f.IsType(current, textOrAttributeType),
                        f.TextCtor(f.XPathNodeValue(current)),
                        f.Sequence()
                    )
                );

                // 3.2 Stylesheet templates
                matcherBuilder.CollectPatterns(sheet, mode);
                applyFunction.Definition = matcherBuilder.BuildMatcher(current, actualArgs, /*otherwise:*/builtinTemplates);
            }
            return f.Invoke(applyFunction, invokeArgs);
        }

        // -------------------------------- IErrorHelper --------------------------------

        public void ReportError(string res, params string[] args) {
            compiler.ReportError(lastScope.SourceLine, res, args);
        }

        public void ReportWarning(string res, params string[] args) {
            compiler.ReportWarning(lastScope.SourceLine, res, args);
        }

        // ------------------------------------------------------------------------------

        [Conditional("DEBUG")]
        private void VerifyXPathQName(QilName qname) {
            Debug.Assert(
                compiler.IsPhantomName(qname) ||
                qname.NamespaceUri == ResolvePrefix(/*ignoreDefaultNs:*/true, qname.Prefix),
                "QilGenerator must resolve the prefix to the same namespace as XsltLoader"
            );
        }

        private string ResolvePrefix(bool ignoreDefaultNs, string prefix) {
            if (ignoreDefaultNs && prefix.Length == 0) {
                return string.Empty;
            } else {
                string ns = scope.LookupNamespace(prefix);
                if (ns == null) {
                    if (prefix.Length == 0) {
                        ns = string.Empty;
                    } else {
                        ReportError(/*[XT0280]*/Res.Xslt_InvalidPrefix, prefix);
                        ns = compiler.CreatePhantomNamespace();
                    }
                }
                return ns;
            }
        }

        private void SetLineInfoCheck(QilNode n, ISourceLineInfo lineInfo) {
            // Prevent xsl:choose override xsl:when, etc.
            if (n.SourceLine == null) {
                SetLineInfo(n, lineInfo);
            } else {
                Debug.Assert(!IsDebug, "Attempt to override SourceLineInfo in debug mode");
            }
        }

        private static QilNode SetLineInfo(QilNode n, ISourceLineInfo lineInfo) {
            Debug.Assert(n.SourceLine == null);
            if (lineInfo != null) {
                SourceLineInfo.Validate(lineInfo);
                if (0 < lineInfo.Start.Line && lineInfo.Start.LessOrEqual(lineInfo.End)) {
                    n.SourceLine = lineInfo;
                }
            }
            return n;
        }

        private QilNode AddDebugVariable(QilName name, QilNode value, QilNode content) {
            QilIterator var = f.Let(value);
            var.DebugName = name.ToString();
            return f.Loop(var, content);
        }

        private QilNode SetDebugNs(QilNode n, QilList nsList) {
            if (n != null && nsList != null) {
                QilNode nsVar = GetNsVar(nsList);
                Debug.Assert(nsVar.XmlType.IsSubtypeOf(T.NamespaceS));
                if (nsVar.XmlType.Cardinality == XmlQueryCardinality.One) {
                    // We want CLR type to be XmlQuerySequence instead of XPathNavigator
                    nsVar = f.TypeAssert(nsVar, T.NamespaceS);
                }
                n = AddDebugVariable(CloneName(nameNamespaces), nsVar, n);
            }
            return n;
        }

        private QilNode AddCurrentPositionLast(QilNode content) {
            if (IsDebug) {
                content = AddDebugVariable(CloneName(nameLast)    , GetLastPosition   (), content);
                content = AddDebugVariable(CloneName(namePosition), GetCurrentPosition(), content);
                content = AddDebugVariable(CloneName(nameCurrent) , GetCurrentNode    (), content);
            }
            return content;
        }

        private QilName CloneName(QilName name) {
            return (QilName)name.ShallowClone(f.BaseFactory);
        }

        // This helper internal class is used for compiling sort's and with-param's
        private class VariableHelper {
            private Stack<QilIterator>  vars = new Stack<QilIterator>();
            private XPathQilFactory     f;

            public VariableHelper(XPathQilFactory f) {
                this.f = f;
            }

            public int StartVariables() {
                return vars.Count;
            }

            public void AddVariable(QilIterator let) {
                Debug.Assert(let.NodeType == QilNodeType.Let);
                vars.Push(let);
            }

            public QilNode FinishVariables(QilNode node, int varScope) {
                Debug.Assert(0 <= varScope && varScope <= vars.Count);
                for (int i = vars.Count - varScope; i-- != 0; ) {
                    node = f.Loop(vars.Pop(), node);
                }
                return node;
            }

            [Conditional("DEBUG")]
            public void CheckEmpty() {
                Debug.Assert(vars.Count == 0, "Accumulated variables left unclaimed");
            }
        }
    }
}
