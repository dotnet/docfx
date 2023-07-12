// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx;

public enum MemberLayout
{
    /// <summary>
    /// Place members in the same page as their containing type
    /// </summary>
    SamePage,

    /// <summary>
    /// Place members in separate pages
    /// </summary>
    SeparatePages,
}

internal enum NamespaceLayout
{
    /// <summary>
    /// Renders the namespaces as a single flat list
    /// </summary>
    Flattened,

    /// <summary>
    /// Renders the namespaces in a nested tree form
    /// </summary>
    Nested,
}

internal class MetadataJsonItemConfig
{
    [JsonProperty("src")]
    public FileMapping Source { get; set; }

    [JsonProperty("dest")]
    public string Destination { get; set; }

    [JsonProperty("shouldSkipMarkup")]
    public bool? ShouldSkipMarkup { get; set; }

    [JsonProperty("raw")]
    public bool? Raw { get; set; }

    [JsonProperty("references")]
    public FileMapping References { get; set; }

    [JsonProperty("filter")]
    public string FilterConfigFile { get; set; }

    [JsonProperty("includePrivateMembers")]
    public bool IncludePrivateMembers { get; set; }

    [JsonProperty("globalNamespaceId")]
    public string GlobalNamespaceId { get; set; }

    /// <summary>
    /// An optional set of MSBuild properties used when interpreting project files. These
    ///  are the same properties that are passed to MSBuild via the /property:&lt;n&gt;=&lt;v&gt;
    ///  command line argument.
    /// </summary>
    [JsonProperty("properties")]
    public Dictionary<string, string> MSBuildProperties { get; set; }

    [JsonProperty("disableGitFeatures")]
    public bool DisableGitFeatures { get; set; }

    [JsonProperty("codeSourceBasePath")]
    public string CodeSourceBasePath { get; set; }

    [JsonProperty("disableDefaultFilter")]
    public bool DisableDefaultFilter { get; set; }

    [JsonProperty("noRestore")]
    public bool NoRestore { get; set; }

    [JsonProperty("namespaceLayout")]
    public NamespaceLayout NamespaceLayout { get; set; }

    [JsonProperty("memberLayout")]
    public MemberLayout MemberLayout { get; set; }

    [JsonProperty("allowCompilationErrors")]
    public bool AllowCompilationErrors { get; set; }
}

internal class MetadataJsonConfig : List<MetadataJsonItemConfig>
{
    public MetadataJsonConfig(IEnumerable<MetadataJsonItemConfig> configs) : base(configs) { }

    public MetadataJsonConfig(params MetadataJsonItemConfig[] configs) : base(configs)
    {
    }
}
