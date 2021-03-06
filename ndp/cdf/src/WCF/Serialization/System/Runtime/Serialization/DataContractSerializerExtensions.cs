// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CodeDom;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace System.Runtime.Serialization
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class DataContractSerializerExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ISerializationSurrogateProvider GetSerializationSurrogateProvider(this DataContractSerializer serializer) 
        {
            SurrogateProviderAdapter adapter = serializer.DataContractSurrogate as SurrogateProviderAdapter;
            return adapter == null ? null : adapter.Provider;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetSerializationSurrogateProvider(this DataContractSerializer serializer, ISerializationSurrogateProvider provider)
        {
            // allocate every time, expectation is that this won't happen enough to warrant maintaining a CondtionalWeakTable.
            IDataContractSurrogate adapter = new SurrogateProviderAdapter(provider);

            serializer.SetDataContractSurrogate(adapter);
        }

        private class SurrogateProviderAdapter : IDataContractSurrogate
        {
            private ISerializationSurrogateProvider _provider;
            public SurrogateProviderAdapter(ISerializationSurrogateProvider provider)
            {
                _provider = provider;
            }

            public ISerializationSurrogateProvider Provider { get { return _provider; } }
            public object GetCustomDataToExport(Type clrType, Type dataContractType)
            {
                throw new NotImplementedException();
            }

            public object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
            {
                throw new NotImplementedException();
            }

            public Type GetDataContractType(Type type)
            {
                return _provider.GetSurrogateType(type);
            }

            public object GetDeserializedObject(object obj, Type targetType)
            {
                return _provider.GetDeserializedObject(obj, targetType);
            }

            public void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
            {
                throw new NotImplementedException();
            }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                return _provider.GetObjectToSerialize(obj, targetType);
            }

            public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
            {
                throw new NotImplementedException();
            }

            public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
            {
                throw new NotImplementedException();
            }
        }
    }
}
