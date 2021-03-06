//------------------------------------------------------------------------------
// <copyright file="QilInvokeEarlyBound.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Reflection;

namespace System.Xml.Xsl.Qil {

    /// <summary>
    /// A function invocation node which reperesents a call to an early bound Clr function.
    /// </summary>
    internal class QilInvokeEarlyBound : QilTernary {

        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        /// <param name="method">QilLiteral containing the Clr MethodInfo for the early bound function</param>
        public QilInvokeEarlyBound(QilNodeType nodeType, QilNode name, QilNode method, QilNode arguments, XmlQueryType resultType)
            : base(nodeType, name, method, arguments) {
            this.xmlType = resultType;
        }


        //-----------------------------------------------
        // QilInvokeEarlyBound methods
        //-----------------------------------------------

        public QilName Name {
            get { return (QilName) Left; }
            set { Left = value; }
        }

        public MethodInfo ClrMethod {
            get { return (MethodInfo) ((QilLiteral) Center).Value; }
            set { ((QilLiteral) Center).Value = value; }
        }

        public QilList Arguments {
            get { return (QilList) Right; }
            set { Right = value; }
        }
    }
}

