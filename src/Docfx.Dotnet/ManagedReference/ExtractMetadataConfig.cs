// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal class ExtractMetadataConfig
{
    public List<string> Files { get; init; }

    public List<string> References { get; init; }

    public string OutputFolder { get; init; }

    public MetadataOutputFormat OutputFormat { get; init; }

    public bool ShouldSkipMarkup { get; init; }

    public string FilterConfigFile { get; init; }

    public bool IncludePrivateMembers { get; init; }

    public bool IncludeExplicitInterfaceImplementations { get; init; }

    public string GlobalNamespaceId { get; init; }

    public string AssemblyUidOverride { get; init; }

    public AssemblyLabel AssemblyLabel { get; init; }

    /// <summary>
    /// <see cref="AssemblyLabel"/> with <see cref="Docfx.AssemblyLabel.Auto"/> resolved against the
    /// namespace layout: a nested layout groups by assembly already, while a flattened one has nothing
    /// else to tell two assemblies that declare the same namespace apart.
    /// </summary>
    public AssemblyLabel ResolvedAssemblyLabel => AssemblyLabel is Docfx.AssemblyLabel.Auto
        ? NamespaceLayout is Docfx.NamespaceLayout.Nested ? Docfx.AssemblyLabel.None : Docfx.AssemblyLabel.Suffix
        : AssemblyLabel;

    public string CodeSourceBasePath { get; init; }

    public bool DisableDefaultFilter { get; init; }

    public bool DisableGitFeatures { get; init; }

    public bool NoRestore { get; init; }

    public CategoryLayout CategoryLayout { get; init; }

    public NamespaceLayout NamespaceLayout { get; init; }

    public MemberLayout MemberLayout { get; init; }

    public EnumSortOrder EnumSortOrder { get; init; }

    public Dictionary<string, string> MSBuildProperties { get; init; }

    public bool AllowCompilationErrors { get; init; }

    public static bool UseClrTypeNames { get; set; }

}
