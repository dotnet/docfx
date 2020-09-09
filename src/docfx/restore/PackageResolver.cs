// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build
{
    internal abstract class PackageResolver : IDisposable
    {
        public abstract Package ResolvePackage2(PackagePath package, PackageFetchOptions options);

        public abstract bool TryResolvePackage(PackagePath package, PackageFetchOptions options, [NotNullWhen(true)] out string? path);

        public abstract string ResolvePackage(PackagePath package, PackageFetchOptions options);

        public virtual void Dispose() { }
    }
}
