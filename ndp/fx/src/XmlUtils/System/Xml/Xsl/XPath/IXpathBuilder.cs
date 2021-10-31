//------------------------------------------------------------------------------
// <copyright file="IXPathBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">sdub</owner>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml.XPath;

namespace System.Xml.Xsl.XPath {

    internal interface IXPathBuilder<Node> {
        // Should be called once per build
        void StartBuild();                 

        // Should be called after build for result tree post-processing
        Node EndBuild(Node result);

        Node String(string value);

        Node Number(double value);

        Node Operator(XPathOperator op, Node left, Node right);

        Node Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string prefix, string name);

        Node JoinStep(Node left, Node right);

        // http://www.w3.org/TR/xquery-semantics/#id-axis-steps
        // reverseStep is how parser comunicates to builder diference between "ansestor[1]" and "(ansestor)[1]" 
        Node Predicate(Node node, Node condition, bool reverseStep);

        Node Variable(string prefix, string name);

        Node Function(string prefix, string name, IList<Node> args);
    }
}
