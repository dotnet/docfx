// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Microsoft.DocAsCode.Dotnet
{
    /// <summary>
    /// Return state of the <see cref="DotnetApiCatalogOptions.IncludeApi"/> and <see cref="DotnetApiCatalogOptions.IncludeAttribute"/> callbacks.
    /// </summary>
    public enum SymbolIncludeState
    {
        /// <summary>
        /// The symbol should be included in the API catalog based on the default configuration.
        /// </summary>
        Default,

        /// <summary>
        /// The symbol should be included in the API catalog.
        /// </summary>
        Include,

        /// <summary>
        /// The symbol should not be included in the API catalog.
        /// </summary>
        Exclude,
    }

    /// <summary>
    /// Provides options to be used with <see cref="DotnetApiCatalog.GenerateManagedReferenceYamlFiles(string)(string, DotnetApiCatalogOptions)"/>.
    /// </summary>
    public class DotnetApiCatalogOptions
    {
        /// <summary>
        /// Customizes the namespaces and types to include in the API catalog.
        /// Excluding a parent symbol exclude all child symbols underneath it.
        /// </summary>
        /// <remarks>
        /// Show private or internal symbols are not supported in this version.
        /// </remarks>
        public Func<ISymbol, SymbolIncludeState>? IncludeApi { get; init; }

        /// <summary>
        /// Customizes the attributes to include in the API catalog.
        /// Excluding a parent symbol exclude all child symbols underneath it.
        /// </summary>
        /// <remarks>
        /// Show private or internal symbols are not supported in this version.
        /// </remarks>
        public Func<ISymbol, SymbolIncludeState>? IncludeAttribute { get; init; }
    }
}
