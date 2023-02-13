// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Microsoft.DocAsCode.Dotnet
{
    /// <summary>
    /// Return state of the <see cref="DotnetApiCatalogOptions.ShowApi"/> and <see cref="DotnetApiCatalogOptions.ShowAttribute"/> callbacks.
    /// </summary>
    public enum SymbolShowState
    {
        /// <summary>
        /// The symbol should be included in the API catalog based on the default configuration.
        /// </summary>
        Default,

        /// <summary>
        /// The symbol should be included in the API catalog.
        /// </summary>
        Show,

        /// <summary>
        /// The symbol should not be included in the API catalog.
        /// </summary>
        Hide,
    }

    /// <summary>
    /// Provides options to be used with <see cref="DotnetApiCatalog.GenerateManagedReferenceYamlFiles(string)(string, DotnetApiCatalogOptions)"/>.
    /// </summary>
    public class DotnetApiCatalogOptions
    {
        /// <summary>
        /// Customizes the namespaces and types to include in the API catalog.
        /// </summary>
        /// <remarks>
        /// Show private or internal symbols are not supported in this version.
        /// </remarks>
        public Func<ISymbol, SymbolShowState>? ShowApi { get; init; }

        /// <summary>
        /// Customizes the attributes to include in the API catalog.
        /// </summary>
        /// <remarks>
        /// Show private or internal symbols are not supported in this version.
        /// </remarks>
        public Func<IMethodSymbol, SymbolShowState>? ShowAttribute { get; init; }
    }
}
