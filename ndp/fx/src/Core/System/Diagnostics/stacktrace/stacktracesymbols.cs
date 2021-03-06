// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Diagnostics
{
    internal sealed class StackTraceSymbols : IDisposable
    {
        private readonly ConcurrentDictionary<IntPtr, MetadataReaderProvider> _metadataCache;

        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        public StackTraceSymbols()
        {
            _metadataCache = new ConcurrentDictionary<IntPtr, MetadataReaderProvider>();
        }

        /// <summary>
        /// Clean up any cached providers.
        /// </summary>
        void IDisposable.Dispose()
        {
            foreach (MetadataReaderProvider provider in _metadataCache.Values)
            {
                if(provider != null)
                {
                    provider.Dispose();
                }
            }

            _metadataCache.Clear();
        }

        /// <summary>
        /// Returns the source file and line number information for the method.
        /// </summary>
        /// <param name="assemblyPath">file path of the assembly or null</param>
        /// <param name="loadedPeAddress">loaded PE image address or zero</param>
        /// <param name="loadedPeSize">loaded PE image size</param>
        /// <param name="inMemoryPdbAddress">in memory PDB address or zero</param>
        /// <param name="inMemoryPdbSize">in memory PDB size</param>
        /// <param name="methodToken">method token</param>
        /// <param name="ilOffset">il offset of the stack frame</param>
        /// <param name="sourceFile">source file return</param>
        /// <param name="sourceLine">line number return</param>
        /// <param name="sourceColumn">column return</param>
        [SecuritySafeCritical]
        public void GetSourceLineInfo(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize,
            IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset,
            out string sourceFile, out int sourceLine, out int sourceColumn)
        {

            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();

            GetSourceLineInfoWithoutCasAssert(assemblyPath, loadedPeAddress, loadedPeSize,
            inMemoryPdbAddress, inMemoryPdbSize, methodToken, ilOffset,
            out sourceFile, out sourceLine, out sourceColumn);
        }


        /// <summary>
        /// Returns the source file and line number information for the method. 
        /// </summary>
        /// <param name="assemblyPath">file path of the assembly or null</param>
        /// <param name="loadedPeAddress">loaded PE image address or zero</param>
        /// <param name="loadedPeSize">loaded PE image size</param>
        /// <param name="inMemoryPdbAddress">in memory PDB address or zero</param>
        /// <param name="inMemoryPdbSize">in memory PDB size</param>
        /// <param name="methodToken">method token</param>
        /// <param name="ilOffset">il offset of the stack frame</param>
        /// <param name="sourceFile">source file return</param>
        /// <param name="sourceLine">line number return</param>
        /// <param name="sourceColumn">column return</param>
        [SecuritySafeCritical]
        public void GetSourceLineInfoWithoutCasAssert(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize,
            IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset,
            out string sourceFile, out int sourceLine, out int sourceColumn)
        {
            sourceFile = null;
            sourceLine = 0;
            sourceColumn = 0;

            try
            {
                MetadataReader reader = TryGetReader(assemblyPath, loadedPeAddress, loadedPeSize, inMemoryPdbAddress, inMemoryPdbSize);
                if (reader == null)
                {
                    return;
                }

                Handle handle = MetadataTokens.Handle(methodToken);
                if (handle.Kind != HandleKind.MethodDefinition)
                {
                    return;
                }

                MethodDebugInformationHandle methodDebugHandle = ((MethodDefinitionHandle)handle).ToDebugInformationHandle();
                MethodDebugInformation methodInfo = reader.GetMethodDebugInformation(methodDebugHandle);

                if (!methodInfo.SequencePointsBlob.IsNil)
                {
                    SequencePointCollection sequencePoints = methodInfo.GetSequencePoints();

                    SequencePoint? bestPointSoFar = null;
                    foreach (SequencePoint point in sequencePoints)
                    {
                        if (point.Offset > ilOffset)
                            break;

                        if (point.StartLine != SequencePoint.HiddenLine)
                            bestPointSoFar = point;
                    }

                    if (bestPointSoFar.HasValue)
                    {
                        sourceLine = bestPointSoFar.Value.StartLine;
                        sourceColumn = bestPointSoFar.Value.StartColumn;
                        sourceFile = reader.GetString(reader.GetDocument(bestPointSoFar.Value.Document).Name);
                    }
                }
            }
            catch (BadImageFormatException)
            {
                // ignore
            }
            catch (IOException)
            {
                // ignore
            }
        }

        /// <summary>
        /// Returns the portable PDB reader for the assembly path
        /// </summary>
        /// <param name="assemblyPath">
        /// File path of the assembly or null if the module is dynamic (generated by Reflection.Emit).
        /// </param>
        /// <param name="loadedPeAddress">
        /// Loaded PE image address or zero if the module is dynamic (generated by Reflection.Emit). 
        /// Dynamic modules have their PDBs (if any) generated to an in-memory stream 
        /// (pointed to by <paramref name="inMemoryPdbAddress"/> and <paramref name="inMemoryPdbSize"/>).
        /// </param>
        /// <param name="loadedPeSize">loaded PE image size</param>
        /// <param name="inMemoryPdbAddress">in memory PDB address or zero</param>
        /// <param name="inMemoryPdbSize">in memory PDB size</param>
        /// <param name="reader">returns the reader</param>
        /// <returns>reader</returns>
        /// <remarks>
        /// Assumes that neither PE image nor PDB loaded into memory can be unloaded or moved around.
        /// </remarks>
        [SecuritySafeCritical]
        [FileIOPermission(SecurityAction.Assert, AllFiles = FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read)]
        private unsafe MetadataReader TryGetReader(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, IntPtr inMemoryPdbAddress, int inMemoryPdbSize)
        {
            if ((loadedPeAddress == IntPtr.Zero || assemblyPath == null) && inMemoryPdbAddress == IntPtr.Zero)
            {
                // Dynamic or in-memory module without symbols (they would be in-memory if they were available).
                return null;
            }

            IntPtr cacheKey = (inMemoryPdbAddress != IntPtr.Zero) ? inMemoryPdbAddress : loadedPeAddress;

            MetadataReaderProvider provider;
            if (_metadataCache.TryGetValue(cacheKey, out provider))
            {
                if (provider == null)
                {
                    return null;
                }
                return provider.GetMetadataReader();
            }

            provider = (inMemoryPdbAddress != IntPtr.Zero) ?
                TryOpenReaderForInMemoryPdb(inMemoryPdbAddress, inMemoryPdbSize) :
                TryOpenReaderFromAssemblyFile(assemblyPath, loadedPeAddress, loadedPeSize);

            // This may fail as another thread might have beaten us to it, but it doesn't matter
            _metadataCache.TryAdd(cacheKey, provider);

            if (provider == null)
            {
                return null;
            }

            // The reader has already been open, so this doesn't throw:
            return provider.GetMetadataReader();
        }

        [SecuritySafeCritical]
        private static unsafe MetadataReaderProvider TryOpenReaderForInMemoryPdb(IntPtr inMemoryPdbAddress, int inMemoryPdbSize)
        {
            Debug.Assert(inMemoryPdbAddress != IntPtr.Zero);

            // quick check to avoid throwing exceptions below in common cases:
            const uint ManagedMetadataSignature = 0x424A5342;
            if (inMemoryPdbSize < sizeof(uint) || *(uint*)inMemoryPdbAddress != ManagedMetadataSignature)
            {
                // not a Portable PDB
                return null;
            }

            var provider = MetadataReaderProvider.FromMetadataImage((byte*)inMemoryPdbAddress, inMemoryPdbSize);
            try
            {
                // may throw if the metadata is invalid
                provider.GetMetadataReader();
                return provider;
            }
            catch (BadImageFormatException)
            {
                provider.Dispose();
                return null;
            }
        }

        [SecuritySafeCritical]
        private static unsafe PEReader TryGetPEReader(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize)
        {
            // 





            Stream peStream = TryOpenFile(assemblyPath);
            if (peStream != null)
            {
                return new PEReader(peStream);
            }

            return null;
        }

        private static MetadataReaderProvider TryOpenReaderFromAssemblyFile(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize)
        {
            using (var peReader = TryGetPEReader(assemblyPath, loadedPeAddress, loadedPeSize))
            {
                if (peReader == null)
                {
                    return null;
                }

                string pdbPath;
                MetadataReaderProvider provider;
                if (peReader.TryOpenAssociatedPortablePdb(assemblyPath, TryOpenFile, out provider, out pdbPath))
                {
                    // 


                    return provider;
                }
            }

            return null;
        }

        private static Stream TryOpenFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return File.OpenRead(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
