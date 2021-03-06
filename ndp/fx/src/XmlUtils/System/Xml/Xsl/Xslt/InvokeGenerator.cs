//------------------------------------------------------------------------------
// <copyright file="InvokeGenerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt {
    using T = XmlQueryTypeFactory;

    /**
    InvokeGenerator is one of the trikest peaces here.
    ARGS:
         QilFunction func      -- Functions which should be invoked. Arguments of this function (formalArgs) are Let nodes
                                  anotated with names and default valies.
                                  Problem 1 is that default values can contain references to previouse args of this function.
                                  Problem 2 is that default values shouldn't contain fixup nodes.
         ArrayList actualArgs  -- Array of QilNodes anotated with names. When name of formalArg match name actualArg last one
                                  is used as invokeArg, otherwise formalArg's default value is cloned and used.
    **/

    internal class InvokeGenerator : QilCloneVisitor {
        private bool                debug;
        private Stack<QilIterator>  iterStack;

        private QilList             formalArgs;
        private QilList             invokeArgs;
        private int                 curArg;     // this.Clone() depends on this value

        private XsltQilFactory      fac;

        public InvokeGenerator(XsltQilFactory f, bool debug) : base(f.BaseFactory) {
            this.debug  = debug;
            this.fac    = f;
            this.iterStack = new Stack<QilIterator>();
        }

        public QilNode GenerateInvoke(QilFunction func, IList<XslNode> actualArgs) {
            iterStack.Clear();
            formalArgs = func.Arguments;
            invokeArgs = fac.ActualParameterList();

            // curArg is an instance variable used in Clone() method
            for (curArg = 0; curArg < formalArgs.Count; curArg ++) {
                // Find actual value for a given formal arg
                QilParameter formalArg = (QilParameter)formalArgs[curArg];
                QilNode      invokeArg = FindActualArg(formalArg, actualArgs);

                // If actual value was not specified, use the default value and copy its debug comment
                if (invokeArg == null) {
                    if (debug) {
                        if (formalArg.Name.NamespaceUri == XmlReservedNs.NsXslDebug) {
                            Debug.Assert(formalArg.Name.LocalName == "namespaces", "Cur,Pos,Last don't have default values and should be always added to by caller in AddImplicitArgs()");
                            Debug.Assert(formalArg.DefaultValue != null, "PrecompileProtoTemplatesHeaders() set it");
                            invokeArg = Clone(formalArg.DefaultValue);
                        } else {
                            invokeArg = fac.DefaultValueMarker();
                        }
                    } else {
                        Debug.Assert(formalArg.Name.NamespaceUri != XmlReservedNs.NsXslDebug, "Cur,Pos,Last don't have default values and should be always added to by caller in AddImplicitArgs(). We don't have $namespaces in !debug.");
                        invokeArg = Clone(formalArg.DefaultValue);
                    }
                }

                XmlQueryType formalType = formalArg.XmlType;
                XmlQueryType invokeType = invokeArg.XmlType;

                // Possible arg types: anyType, node-set, string, boolean, and number
                fac.CheckXsltType(formalArg);
                fac.CheckXsltType(invokeArg);

                if (!invokeType.IsSubtypeOf(formalType)) {
                    // This may occur only if inferred type of invokeArg is XslFlags.None
                    Debug.Assert(invokeType == T.ItemS, "Actual argument type is not a subtype of formal argument type");
                    invokeArg = fac.TypeAssert(invokeArg, formalType);
                }

                invokeArgs.Add(invokeArg);
            }

            // Create Invoke node and wrap it with previous parameter declarations
            QilNode invoke = fac.Invoke(func, invokeArgs);
            while (iterStack.Count != 0)
                invoke = fac.Loop(iterStack.Pop(), invoke);

            return invoke;
        }

        private QilNode FindActualArg(QilParameter formalArg, IList<XslNode> actualArgs) {
            QilName argName = formalArg.Name;
            Debug.Assert(argName != null);
            foreach (XslNode actualArg in actualArgs) {
                if (actualArg.Name.Equals(argName)) {
                    return ((VarPar)actualArg).Value;
                }
            }
            return null;
        }

        // ------------------------------------ QilCloneVisitor -------------------------------------

        protected override QilNode VisitReference(QilNode n) {
            QilNode replacement = FindClonedReference(n);

            // If the reference is internal for the subtree being cloned, return it as is
            if (replacement != null) {
                return replacement;
            }

            // Replacement was not found, thus the reference is external for the subtree being cloned.
            // The case when it refers to one of previous arguments (xsl:param can refer to previous
            // xsl:param's) must be taken care of.
            for (int prevArg = 0; prevArg < curArg; prevArg++) {
                Debug.Assert(formalArgs[prevArg] != null, "formalArg must be in the list");
                Debug.Assert(invokeArgs[prevArg] != null, "This arg should be compiled already");

                // Is this a reference to prevArg?
                if (n == formalArgs[prevArg]) {
                    // If prevArg is a literal, just clone it
                    if (invokeArgs[prevArg] is QilLiteral) {
                        return invokeArgs[prevArg].ShallowClone(fac.BaseFactory);
                    }

                    // If prevArg is not an iterator, cache it in an iterator, and return it
                    if (!(invokeArgs[prevArg] is QilIterator)) {
                        QilIterator var = fac.BaseFactory.Let(invokeArgs[prevArg]);
                        iterStack.Push(var);
                        invokeArgs[prevArg] = var;
                    }
                    Debug.Assert(invokeArgs[prevArg] is QilIterator);
                    return invokeArgs[prevArg];
                }
            }

            // This is a truly external reference, return it as is
            return n;
        }

        protected override QilNode VisitFunction(QilFunction n) {
            // No need to change function references
            return n;
        }
    }
}
