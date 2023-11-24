// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.Git;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

/// <summary>
/// Return state of the <see cref="DotnetApiOptions.IncludeApi"/> and <see cref="DotnetApiOptions.IncludeAttribute"/> callbacks.
/// </summary>
public enum SymbolIncludeState
{
    /// <summary>
    /// Determines whether to include or not using the default configuration.
    /// </summary>
    Default,

    /// <summary>
    /// Ignore default rules and include the symbol in the API catalog.
    /// </summary>
    Include,

    /// <summary>
    /// Ignores default rules and exclude the symbol from the API catalog.
    /// </summary>
    Exclude,
}

/// <summary>
/// Provides options to be used with <see cref="DotnetApiCatalog.GenerateManagedReferenceYamlFiles(string, DotnetApiOptions)"/>.
/// </summary>
public class DotnetApiOptions
{
    /// <summary>
    /// Customizes the namespaces and types to include in the API catalog.
    /// Excluding a parent symbol exclude all child symbols underneath it.
    /// </summary>
    public Func<ISymbol, SymbolIncludeState>? IncludeApi { get; init; }

    /// <summary>
    /// Customizes the attributes to include in the API catalog.
    /// Excluding a parent symbol exclude all child symbols underneath it.
    /// </summary>
    public Func<ISymbol, SymbolIncludeState>? IncludeAttribute { get; init; }

    /// <summary>
    /// Customizes the view source URL for files in a git repository.
    /// Returns `null` to use built-in support for GitHub, Azure Repos, etc.
    /// </summary>
    public Func<GitSource, string?>? SourceUrl { get; init; }
}
