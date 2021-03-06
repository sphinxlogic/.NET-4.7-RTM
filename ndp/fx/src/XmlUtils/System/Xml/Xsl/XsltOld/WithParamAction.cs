//------------------------------------------------------------------------------
// <copyright file="WithParamAction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml.Xsl.XsltOld {
    using Res = System.Xml.Utils.Res;
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Xml;
    using System.Xml.XPath;

    internal class WithParamAction : VariableAction {
        internal WithParamAction() : base(VariableType.WithParameter) {}

        internal override void Compile(Compiler compiler) {
            CompileAttributes(compiler);
            CheckRequiredAttribute(compiler, this.name, "name");
            if (compiler.Recurse()) {
                CompileTemplate(compiler);
                compiler.ToParent();

                if (this.selectKey != Compiler.InvalidQueryKey && this.containedActions != null) {
                    throw XsltException.Create(Res.Xslt_VariableCntSel2, this.nameStr);
                }
            }
        }
        
        internal override void Execute(Processor processor, ActionFrame frame) {
            Debug.Assert(processor != null && frame != null);
            object ParamValue;
            switch(frame.State) {
            case Initialized:           
                if (this.selectKey != Compiler.InvalidQueryKey) {
                    ParamValue = processor.RunQuery(frame, this.selectKey);
                    processor.SetParameter(this.name, ParamValue);
                    frame.Finished();
                }
                else {
                    if (this.containedActions == null) {
                        processor.SetParameter(this.name, string.Empty);
                        frame.Finished();
                        break;
                    }
                    NavigatorOutput output = new NavigatorOutput(baseUri);
                    processor.PushOutput(output);
                    processor.PushActionFrame(frame);
                    frame.State = ProcessingChildren;
                }
                break;
            case ProcessingChildren:
                RecordOutput recOutput = processor.PopOutput();
                Debug.Assert(recOutput is NavigatorOutput);
                processor.SetParameter(this.name,((NavigatorOutput)recOutput).Navigator);
                frame.Finished();
                break;
            default:
                Debug.Fail("Invalid execution state inside VariableAction.Execute");
		    break;
            }
        }
    }
}
