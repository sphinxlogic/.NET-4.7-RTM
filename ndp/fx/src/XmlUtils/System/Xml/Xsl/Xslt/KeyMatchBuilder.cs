//------------------------------------------------------------------------------
// <copyright file="KeyMatchBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using MS.Internal.Xml;
using System.Xml.Xsl.XPath;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt {

    internal class KeyMatchBuilder : XPathBuilder, XPathPatternParser.IPatternBuilder {
        private int depth = 0;
        PathConvertor convertor;

        public KeyMatchBuilder(IXPathEnvironment env) : base(env) {
            convertor = new PathConvertor(env.Factory);
        }

        public override void StartBuild() {
            Debug.Assert(0 <= depth && depth <= 1, "this shouldn't happen");
            if (depth == 0) {
                base.StartBuild();
            }
            depth ++;
        }

        public override QilNode EndBuild(QilNode result) {
            depth --;
            Debug.Assert(0 <= depth && depth <= 1, "this shouldn't happen");
            if (result == null) { // special door to clean builder state in exception handlers
                return base.EndBuild(result);
            }
            if (depth == 0) {
                Debug.Assert(base.numFixupLast     == 0);
                Debug.Assert(base.numFixupPosition == 0);
                result = convertor.ConvertReletive2Absolute(result, base.fixupCurrent);
                result = base.EndBuild(result);
            }
            return result;
        }

        // -------------------------------------- GetPredicateBuilder() ---------------------------------------

        public virtual IXPathBuilder<QilNode> GetPredicateBuilder(QilNode ctx) {
            return this;
        }

        // This code depends on particula shapes that XPathBuilder generates.
        // It works only on pathes.
        // ToDo: We can do better here.
        internal class PathConvertor : QilReplaceVisitor {
            new XPathQilFactory f;
            QilNode fixup;
            public PathConvertor(XPathQilFactory f) : base (f.BaseFactory) {
                this.f = f;
            }

            public QilNode ConvertReletive2Absolute(QilNode node, QilNode fixup) {
                QilDepthChecker.Check(node);
                Debug.Assert(node  != null);
                Debug.Assert(fixup != null);
                this.fixup = fixup;
                return this.Visit(node);
            }

            // transparantly passing through Union and DocOrder
            protected override QilNode Visit(QilNode n) {
                if (
                    n.NodeType == QilNodeType.Union ||
                    n.NodeType == QilNodeType.DocOrderDistinct ||
                    n.NodeType == QilNodeType.Filter ||
                    n.NodeType == QilNodeType.Loop
                ) {
                    return base.Visit(n);
                }
                return n;
            }
            // Filers that travers Content being converted to global travers:
            // Filter($j= ... Filter($i = Content(fixup), ...))  -> Filter($j= ... Filter($i = Loop($j = DesendentOrSelf(Root(fixup)), Content($j), ...)))
            protected override QilNode VisitLoop(QilLoop n) {
                if (n.Variable.Binding.NodeType == QilNodeType.Root || n.Variable.Binding.NodeType == QilNodeType.Deref) {
                    // This is absolute path already. We shouldn't touch it
                    return n;
                }
                if (n.Variable.Binding.NodeType == QilNodeType.Content) {
                    // This is "begin" of reletive path. Let's rewrite it as absolute:
                    QilUnary content = (QilUnary)n.Variable.Binding;
                    Debug.Assert(content.Child == this.fixup, "Unexpected content node");
                    QilIterator it = f.For(f.DescendantOrSelf(f.Root(this.fixup)));
                    content.Child = it;
                    n.Variable.Binding = f.Loop(it, content);
                    return n;
                }
                n.Variable.Binding = Visit(n.Variable.Binding);
                return n;
            }

            protected override QilNode VisitFilter(QilLoop n) {
                return VisitLoop(n);
            }
        }
    }
}
