//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace System.Runtime.Serialization
{
    using System;
    using System.Text;
    using System.Reflection;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Xml;

    public static class XPathQueryGenerator
    {
        const string XPathSeparator = "/";
        const string NsSeparator = ":";

        public static string CreateFromDataContractSerializer(Type type, MemberInfo[] pathToMember, out XmlNamespaceManager namespaces)
        {
            return CreateFromDataContractSerializer(type, pathToMember, null, out namespaces);
        }

        // Here you can provide your own root element Xpath which will replace the Xpath of the top level element
        public static string CreateFromDataContractSerializer(Type type, MemberInfo[] pathToMember, StringBuilder rootElementXpath, out XmlNamespaceManager namespaces)
        {
            if (type == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("type"));
            }
            if (pathToMember == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("pathToMember"));
            }

            DataContract currentContract = DataContract.GetDataContract(type);
            ExportContext context;

            if (rootElementXpath == null)
            {
                context = new ExportContext(currentContract);
            }
            else
            {
                // use the provided xpath for top level element
                context = new ExportContext(rootElementXpath);
            }

            for (int pathToMemberIndex = 0; pathToMemberIndex < pathToMember.Length; pathToMemberIndex++)
            {
                currentContract = ProcessDataContract(currentContract, context, pathToMember[pathToMemberIndex]);
            }

            namespaces = context.Namespaces;
            return context.XPath;
        }

        static DataContract ProcessDataContract(DataContract contract, ExportContext context, MemberInfo memberNode)
        {
            if (contract is ClassDataContract)
            {
                return ProcessClassDataContract((ClassDataContract)contract, context, memberNode);
            }
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.GetString(SR.QueryGeneratorPathToMemberNotFound)));
        }

        static DataContract ProcessClassDataContract(ClassDataContract contract, ExportContext context, MemberInfo memberNode)
        {
            string prefix = context.SetNamespace(contract.Namespace.Value);
            foreach (DataMember member in GetDataMembers(contract))
            {
                if (member.MemberInfo.Name == memberNode.Name && member.MemberInfo.DeclaringType.IsAssignableFrom(memberNode.DeclaringType))
                {
                    context.WriteChildToContext(member, prefix);
                    return member.MemberTypeContract;
                }
            }
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.GetString(SR.QueryGeneratorPathToMemberNotFound)));
        }

        static IEnumerable<DataMember> GetDataMembers(ClassDataContract contract)
        {
            if (contract.BaseContract != null)
            {
                foreach (DataMember baseClassMember in GetDataMembers(contract.BaseContract))
                {
                    yield return baseClassMember;
                }
            }
            if (contract.Members != null)
            {
                foreach (DataMember member in contract.Members)
                {
                    yield return member;
                }
            }
        }

        class ExportContext
        {
            XmlNamespaceManager namespaces;
            int nextPrefix;
            StringBuilder xPathBuilder;

            public ExportContext(DataContract rootContract)
            {
                this.namespaces = new XmlNamespaceManager(new NameTable());
                string prefix = SetNamespace(rootContract.TopLevelElementNamespace.Value);
                this.xPathBuilder = new StringBuilder(XPathQueryGenerator.XPathSeparator + prefix + XPathQueryGenerator.NsSeparator + rootContract.TopLevelElementName.Value);
            }

            public ExportContext(StringBuilder rootContractXPath)
            {
                this.namespaces = new XmlNamespaceManager(new NameTable());
                this.xPathBuilder = rootContractXPath;
            }

            public void WriteChildToContext(DataMember contextMember, string prefix)
            {
                this.xPathBuilder.Append(XPathQueryGenerator.XPathSeparator + prefix + XPathQueryGenerator.NsSeparator + contextMember.Name);
            }

            public XmlNamespaceManager Namespaces
            {
                get
                {
                    return this.namespaces;
                }
            }

            public string XPath
            {
                get
                {
                    return this.xPathBuilder.ToString();
                }
            }

            public string SetNamespace(string ns)
            {
                string prefix = namespaces.LookupPrefix(ns);
                if (prefix == null || prefix.Length == 0)
                {
                    prefix = "xg" + (this.nextPrefix++).ToString(NumberFormatInfo.InvariantInfo);
                    Namespaces.AddNamespace(prefix, ns);
                }
                return prefix;
            }
        }
    }
}
