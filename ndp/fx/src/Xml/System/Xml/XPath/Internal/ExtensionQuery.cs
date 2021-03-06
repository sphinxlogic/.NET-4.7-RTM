//------------------------------------------------------------------------------
// <copyright file="ExtensionQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace MS.Internal.Xml.XPath {
    using System;
    using System.Xml;
    using System.Xml.Xsl;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections;

    internal abstract class ExtensionQuery : Query {
        protected string prefix;
        protected string name;
        protected XsltContext xsltContext;
        private ResetableIterator queryIterator;

        public ExtensionQuery(string prefix, string name) : base() {
            this.prefix = prefix;
            this.name   = name;
        }
        protected ExtensionQuery(ExtensionQuery other) : base(other) {
            this.prefix      = other.prefix;
            this.name        = other.name;
            this.xsltContext = other.xsltContext;
            this.queryIterator = (ResetableIterator)Clone(other.queryIterator);
        }

        public override void Reset() {
            if (queryIterator != null) {
                queryIterator.Reset();
            }
        }

        public override XPathNavigator Current {
            get {
                if (queryIterator == null) {
                    throw XPathException.Create(Res.Xp_NodeSetExpected);
                }
                if (queryIterator.CurrentPosition == 0) {
                    Advance();
                }
                return queryIterator.Current;
            } 
        }

        public override XPathNavigator Advance() {
            if (queryIterator == null) {
                throw XPathException.Create(Res.Xp_NodeSetExpected);
            }
            if (queryIterator.MoveNext()) {
                return queryIterator.Current;
            }
            return null;
        }

        public override int CurrentPosition { 
            get { 
                if (queryIterator != null) {
                    return queryIterator.CurrentPosition;
                }
                return 0;
            } 
        }

        protected object ProcessResult(object value) {
            if (value is string        ) return value;
            if (value is double        ) return value;
            if (value is bool          ) return value;
            if (value is XPathNavigator) return value;
            if (value is Int32         ) return (double)(Int32)value;

            if (value == null) {
                queryIterator = XPathEmptyIterator.Instance;
                return this; // We map null to NodeSet to let $null/foo work well.
            }

            ResetableIterator resetable = value as ResetableIterator;
            if (resetable != null) {
                // We need Clone() value because variable may be used several times 
                // and they shouldn't 
                queryIterator = (ResetableIterator)resetable.Clone(); 
                return this;
            }
            XPathNodeIterator nodeIterator = value as XPathNodeIterator;
            if (nodeIterator != null) {
                queryIterator = new XPathArrayIterator(nodeIterator);
                return this;
            }
            IXPathNavigable navigable = value as IXPathNavigable;
            if(navigable != null) {
                return navigable.CreateNavigator();
            }

            if (value is Int16 ) return (double)(Int16)value;
            if (value is Int64 ) return (double)(Int64)value;
            if (value is UInt32) return (double)(UInt32)value;
            if (value is UInt16) return (double)(UInt16)value;
            if (value is UInt64) return (double)(UInt64)value;
            if (value is Single) return (double)(Single)value;
            if (value is Decimal) return (double)(Decimal)value;
            return value.ToString();
        }

        protected string QName { get { return prefix.Length != 0 ? prefix + ":" + name : name; } }

        public override int Count { get { return queryIterator == null ? 1 : queryIterator.Count; } }
        public override XPathResultType StaticType { get { return XPathResultType.Any; } }
    }
}
