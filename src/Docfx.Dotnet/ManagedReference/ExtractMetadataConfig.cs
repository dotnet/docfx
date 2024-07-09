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
