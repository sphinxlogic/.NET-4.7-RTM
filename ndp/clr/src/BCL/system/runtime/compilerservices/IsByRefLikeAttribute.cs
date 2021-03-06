// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This attribute should not be used by developers in source code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class IsByRefLikeAttribute : Attribute
    {
        public IsByRefLikeAttribute()
        {
        }
    }
}
