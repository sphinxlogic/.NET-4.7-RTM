//------------------------------------------------------------------------------
// <copyright file="XPathItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner> 
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Xml.Schema;

namespace System.Xml.XPath {
    /// <summary>
    /// Base class for XPathNavigator and XmlAtomicValue.
    /// </summary>
    public abstract class XPathItem {
        /// <summary>
        /// True if this item is a node, and not an atomic value.
        /// </summary>
        public abstract bool IsNode { get; }

        /// <summary>
        /// Returns Xsd type of atomic value, or of node's content.
        /// </summary>
        public abstract XmlSchemaType XmlType { get; }

        /// <summary>
        /// Typed and untyped value accessors.
        /// </summary>
        public abstract string Value { get; }
        public abstract object TypedValue { get; }
        public abstract Type ValueType { get; }
        public abstract bool ValueAsBoolean { get; }
        public abstract DateTime ValueAsDateTime { get; }
        public abstract double ValueAsDouble { get; }
        public abstract int ValueAsInt { get; }
        public abstract long ValueAsLong { get; }
        public virtual object ValueAs(Type returnType) { return ValueAs(returnType, null); }
        public abstract object ValueAs(Type returnType, IXmlNamespaceResolver nsResolver);
    }
}

