//------------------------------------------------------------------------------
// <copyright file="PrefixQName.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml.Xsl.XsltOld {
    using Res = System.Xml.Utils.Res;
    using System;
    using System.Diagnostics; 
    using System.Xml;

    internal sealed class PrefixQName {
        public string Prefix;
        public string Name;
        public string Namespace;

        internal void ClearPrefix() {
            Prefix = string.Empty;
        }

        internal void SetQName(string qname) {
            PrefixQName.ParseQualifiedName(qname, out Prefix, out Name);
        }

        //
        // Parsing qualified names
        //

        public static void ParseQualifiedName(string qname, out string prefix, out string local) {
            Debug.Assert(qname != null);
            prefix = string.Empty;
            local = string.Empty;

            // parse first NCName (prefix or local name)
            int position = ValidateNames.ParseNCName(qname);
            if (position == 0) {
                throw XsltException.Create(Res.Xslt_InvalidQName, qname);
            }
            local = qname.Substring(0, position);
            
            // not at the end -> parse ':' and the second NCName (local name)
            if (position < qname.Length) {
                if (qname[position] == ':') {
                    int startLocalNamePos = ++position;
                    prefix = local;
                    int len = ValidateNames.ParseNCName(qname, position);
                    position += len;
                    if (len == 0) {
                        throw XsltException.Create(Res.Xslt_InvalidQName, qname);
                    }
                    local = qname.Substring(startLocalNamePos, len);
                }

                // still not at the end -> error
                if (position < qname.Length) {
                    throw XsltException.Create(Res.Xslt_InvalidQName, qname);
                }
            }
        }

        public static bool ValidatePrefix(string prefix) {
            if (prefix.Length == 0) {
                return false;
            }
            int endPos = ValidateNames.ParseNCName(prefix, 0);
            return endPos == prefix.Length;
        }
    }
}
