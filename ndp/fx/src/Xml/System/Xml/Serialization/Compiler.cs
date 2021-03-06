//------------------------------------------------------------------------------
// <copyright file="Compiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Serialization {
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections;
    using System.IO;
    using System;
    using System.Text;
    using System.ComponentModel;
    using System.CodeDom.Compiler;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Security.Principal;
    using System.Security.Policy;
    using System.Threading;
    using System.Xml.Serialization.Configuration;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;

    internal class Compiler {
        bool debugEnabled = DiagnosticsSwitches.KeepTempFiles.Enabled;
        Hashtable imports = new Hashtable();
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

        [ResourceExposure(ResourceScope.Machine)]
        protected string[] Imports {
            get { 
                string[] array = new string[imports.Values.Count];
                imports.Values.CopyTo(array, 0);
                return array;
            }
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.None)]
        internal void AddImport(Type type, Hashtable types) {
            if (type == null)
                return;
            if (TypeScope.IsKnownType(type))
                return;
            if (types[type] != null)
                return;
            types[type] = type;
            Type baseType = type.BaseType;
            if (baseType != null)
                AddImport(baseType, types);

            Type declaringType = type.DeclaringType;
            if (declaringType != null)
                AddImport(declaringType, types);

            foreach (Type intf in type.GetInterfaces())
                AddImport(intf, types);

            ConstructorInfo[] ctors = type.GetConstructors();
            for (int i = 0; i < ctors.Length; i++) {
                ParameterInfo[] parms = ctors[i].GetParameters();
                for (int j = 0; j < parms.Length; j++) {
                    AddImport(parms[j].ParameterType, types);
                }
            }

            if (type.IsGenericType) {
                Type[] arguments = type.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++) {
                    AddImport(arguments[i], types);
                }
            }

            TempAssembly.FileIOPermission.Assert();
            Module module = type.Module;
            Assembly assembly = module.Assembly;
            if (DynamicAssemblies.IsTypeDynamic(type)) {
                DynamicAssemblies.Add(assembly);
                return;
            }

            object[] typeForwardedFromAttribute = type.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false);
            if (typeForwardedFromAttribute.Length > 0)
            {
                TypeForwardedFromAttribute originalAssemblyInfo = typeForwardedFromAttribute[0] as TypeForwardedFromAttribute;
                Assembly originalAssembly = Assembly.Load(originalAssemblyInfo.AssemblyFullName);
                imports[originalAssembly] = originalAssembly.Location;
            }

            imports[assembly] = assembly.Location;
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.None)]
        internal void AddImport(Assembly assembly) {
            TempAssembly.FileIOPermission.Assert();
            imports[assembly] = assembly.Location;
        }

        internal TextWriter Source {
            get { return writer; }
        }

        internal void Close() { }

        [ResourceConsumption(ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static string GetTempAssemblyPath(string baseDir, Assembly assembly, string defaultNamespace) {
            if (assembly.IsDynamic) {
                throw new InvalidOperationException(Res.GetString(Res.XmlPregenAssemblyDynamic));
            }

            PermissionSet perms = new PermissionSet(PermissionState.None);
            perms.AddPermission(new FileIOPermission(PermissionState.Unrestricted));
            perms.AddPermission(new EnvironmentPermission(PermissionState.Unrestricted));
            perms.Assert();

            try {
                if (baseDir != null && baseDir.Length > 0) {
                    // check that the dirsctory exists
                    if (!Directory.Exists(baseDir)) {
                        throw new UnauthorizedAccessException(Res.GetString(Res.XmlPregenMissingDirectory, baseDir));
                    }
                }
                else {
                    baseDir = Path.GetTempPath();
                    // check that the dirsctory exists
                    if (!Directory.Exists(baseDir)) {
                        throw new UnauthorizedAccessException(Res.GetString(Res.XmlPregenMissingTempDirectory));
                    }
                }
                if (baseDir.EndsWith("\\", StringComparison.Ordinal))
                    baseDir += GetTempAssemblyName(assembly.GetName(), defaultNamespace);
                else 
                    baseDir += "\\" + GetTempAssemblyName(assembly.GetName(), defaultNamespace);
            }
            finally {
                CodeAccessPermission.RevertAssert();
            }
            return baseDir + ".dll";
        }

        internal static string GetTempAssemblyName(AssemblyName parent, string ns) {
            return parent.Name + ".XmlSerializers" + (ns == null || ns.Length == 0 ? "" : "." +  ns.GetHashCode());
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.None)]
        internal Assembly Compile(Assembly parent, string ns, XmlSerializerCompilerParameters xmlParameters, Evidence evidence) {
            CodeDomProvider codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
            CompilerParameters parameters = xmlParameters.CodeDomParameters;
            parameters.ReferencedAssemblies.AddRange(Imports);
            
            if (debugEnabled) {
                parameters.GenerateInMemory = false;
                parameters.IncludeDebugInformation = true;
                parameters.TempFiles.KeepFiles = true;
            }
            PermissionSet perms = new PermissionSet(PermissionState.None);
            if (xmlParameters.IsNeedTempDirAccess) {
                perms.AddPermission(TempAssembly.FileIOPermission);
            }
            perms.AddPermission(new EnvironmentPermission(PermissionState.Unrestricted));
            perms.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
            perms.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlEvidence));
            perms.Assert();

            if (parent != null && (parameters.OutputAssembly == null || parameters.OutputAssembly.Length ==0)) {
                string assemblyName = AssemblyNameFromOptions(parameters.CompilerOptions);
                if (assemblyName == null)
                    assemblyName = GetTempAssemblyPath(parameters.TempFiles.TempDir, parent, ns);
                // 
                parameters.OutputAssembly = assemblyName;
            }

            if (parameters.CompilerOptions == null || parameters.CompilerOptions.Length == 0)
                parameters.CompilerOptions = "/nostdlib";
            else
                parameters.CompilerOptions += " /nostdlib";

            parameters.CompilerOptions += " /D:_DYNAMIC_XMLSERIALIZER_COMPILATION";
#pragma warning disable 618
            parameters.Evidence = evidence;
#pragma warning restore 618
            CompilerResults results = null;
            Assembly assembly = null;
            try {
                results = codeProvider.CompileAssemblyFromSource(parameters, writer.ToString());
                // check the output for errors or a certain level-1 warning (1595)
                if (results.Errors.Count > 0) {
                    StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                    stringWriter.WriteLine(Res.GetString(Res.XmlCompilerError, results.NativeCompilerReturnValue.ToString(CultureInfo.InvariantCulture)));
                    bool foundOne = false;
                    foreach (CompilerError e in results.Errors) {
                        // clear filename. This makes ToString() print just error number and message.
                        e.FileName = "";
                        if (!e.IsWarning || e.ErrorNumber == "CS1595") {
                            foundOne = true;
                            stringWriter.WriteLine(e.ToString());
                        }
                    }
                    if (foundOne) {
                        throw new InvalidOperationException(stringWriter.ToString());
                    }
                }
                assembly = results.CompiledAssembly;
            }
            catch (UnauthorizedAccessException) {
                // try to get the user token
                string user = GetCurrentUser();
                if (user == null || user.Length == 0) {
                    throw new UnauthorizedAccessException(Res.GetString(Res.XmlSerializerAccessDenied));
                }
                else {
                    throw new UnauthorizedAccessException(Res.GetString(Res.XmlIdentityAccessDenied, user));
                }
            }
            catch (FileLoadException fle) {
                throw new InvalidOperationException(Res.GetString(Res.XmlSerializerCompileFailed), fle);
            }
            finally {
                CodeAccessPermission.RevertAssert();
            }
            // somehow we got here without generating an assembly
            if (assembly == null) throw new InvalidOperationException(Res.GetString(Res.XmlInternalError));
            
            return assembly;
        }

        static string AssemblyNameFromOptions(string options) {
            if (options == null || options.Length == 0)
                return null;

            string outName = null;
            string[] flags = options.ToLower(CultureInfo.InvariantCulture).Split(null);
            for (int i = 0; i < flags.Length; i++) {
                string val = flags[i].Trim();
                if (val.StartsWith("/out:", StringComparison.Ordinal)) {
                    outName = val.Substring(5);
                }
            }
            return outName;
        }

        internal static string GetCurrentUser()
        {
#if !FEATURE_PAL
            try {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                if (id != null && id.Name != null)
                    return id.Name;
            } 
            catch (Exception e) {
                if (e is ThreadAbortException || e is StackOverflowException || e is OutOfMemoryException) {
                    throw;
                }
            }
#endif // !FEATURE_PAL
            return "";
        }
    }
}


