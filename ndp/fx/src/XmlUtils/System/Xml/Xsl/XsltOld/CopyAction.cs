//------------------------------------------------------------------------------
// <copyright file="CopyAction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml.Xsl.XsltOld {
    using Res = System.Xml.Utils.Res;
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using MS.Internal.Xml.XPath;

    internal class CopyAction : ContainerAction {
        // Local execution states
        private const int CopyText          = 4;
        private const int NamespaceCopy     = 5;

        private const int ContentsCopy      = 6;
        private const int ProcessChildren   = 7;
        private const int ChildrenOnly      = 8;

        private string useAttributeSets;
        private bool   empty;

        internal override void Compile(Compiler compiler) {
            CompileAttributes(compiler);

            if (compiler.Recurse()) {
                CompileTemplate(compiler);
                compiler.ToParent();
            }
            if (this.containedActions == null)
                this.empty = true;
                
        }

        internal override bool CompileAttribute(Compiler compiler) {
            string name   = compiler.Input.LocalName;
            string value  = compiler.Input.Value;
            if (Ref.Equal(name, compiler.Atoms.UseAttributeSets)) {
                this.useAttributeSets = value;
                AddAction(compiler.CreateUseAttributeSetsAction());
            }
            else {
                return false;
            }
            return true;
        }

        internal override void Execute(Processor processor, ActionFrame frame) {
            Debug.Assert(processor != null && frame != null);

            while (processor.CanContinue) {
                switch (frame.State) {
                case Initialized:
                    if (Processor.IsRoot(frame.Node)) {
                        processor.PushActionFrame(frame);
                        frame.State = ChildrenOnly;
                        break;
                    }

                    if (processor.CopyBeginEvent(frame.Node, this.empty) == false) {
                        // This event wasn't processed
                        break;
                    }
                    frame.State = NamespaceCopy;
                 
                    continue;
                case NamespaceCopy:
                    frame.State = ContentsCopy;
                    if ( frame.Node.NodeType == XPathNodeType.Element ) {
                        processor.PushActionFrame(CopyNamespacesAction.GetAction(), frame.NodeSet);
                        break;
                    }
                    continue;
                case ContentsCopy:
                    if (frame.Node.NodeType == XPathNodeType.Element && !this.empty) {
                        //Debug.Assert(frame.Node.HasValue == false);
                        processor.PushActionFrame(frame);
                        frame.State = ProcessChildren;
                        break;
                    }
                    else {
                        if (processor.CopyTextEvent(frame.Node)) {
                            frame.State = ProcessChildren;
                            continue;
                        }
                        else {
                            // This event wasn't processed
                            break;
                        }
                    }

                case ProcessChildren:
                    if (processor.CopyEndEvent(frame.Node)) {
                        frame.Finished();
                    }
                    break;

                case ChildrenOnly:
                    frame.Finished();
                    break;

                default:
                    Debug.Fail("Invalid CopyAction execution state");
			        break;
                }

                break;
            }
        }
    }
}
