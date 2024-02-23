// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx;

/// <summary>
/// Specifies the layout of members.
/// </summary>
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

/// <summary>
/// Specifies the layout of namepsaces.
/// </summary>
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

/// <summary>
/// Specifies the sort order for enums.
/// </summary>
internal enum EnumSortOrder
{
    /// <summary>
    /// Sorts enums in alphabetic order.
    /// </summary>
    Alphabetic,

    /// <summary>
    /// Sorts enums in the order they are declared.
    /// </summary>
    DeclaringOrder
}

/// <summary>
/// Specifies the output file format.
/// </summary>
internal enum MetadataOutputFormat
{
    /// <summary>
    /// Output as ManagedReference YAML files
    /// </summary>
    Mref,

    /// <summary>
    /// Output as common-mark compliant markdown file
    /// </summary>
    Markdown,

    /// <summary>
    /// Output as ApiPage YAML files
    /// </summary>
    ApiPage,
}

/// <summary>
/// MetadataJsonItemConfig.
/// </summary>
/// <see href="https://dotnet.github.io/docfx/reference/docfx-json-reference.html#11-properties-for-metadata"/>
internal class MetadataJsonItemConfig
{
    /// <summary>
    /// Defines the source projects to have metadata generated.
    /// </summary>
    [JsonPropertyName("src")]
    public FileMapping Src { get; set; }

    /// <summary>
    /// Defines the output folder of the generated metadata files.
    /// Command line --output argument prepends this value.
    /// </summary>
    [JsonPropertyName("dest")]
    public string Dest { get; set; }

    /// <summary>
    /// Defines the output folder of the generated metadata files.
    /// Command line --output argument override this value.
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; }

    /// <summary>
    /// Defines the output file format.
    /// </summary>
    [JsonPropertyName("outputFormat")]
    public MetadataOutputFormat OutputFormat { get; set; }

    /// <summary>
    /// If set to true, DocFX would not render triple-slash-comments in source code as markdown.
    /// </summary>
    [JsonPropertyName("shouldSkipMarkup")]
    public bool? ShouldSkipMarkup { get; set; }

    [JsonPropertyName("raw")]
    public bool? Raw { get; set; }

    [JsonPropertyName("references")]
    public FileMapping References { get; set; }

    /// <summary>
    /// Defines the filter configuration file.
    /// </summary>
    [JsonPropertyName("filter")]
    public string Filter { get; set; }

    /// <summary>
    /// Include private or internal APIs.
    /// The default is false.
    /// </summary>
    [JsonPropertyName("includePrivateMembers")]
    public bool IncludePrivateMembers { get; set; }

    /// <summary>
    /// Include explicit interface implementations.
    /// The default is false.
    /// </summary>
    [JsonPropertyName("includeExplicitInterfaceImplementations")]
    public bool IncludeExplicitInterfaceImplementations { get; set; }

    /// <summary>
    /// Specify the name to use for the global namespace.
    /// </summary>
    [JsonPropertyName("globalNamespaceId")]
    public string GlobalNamespaceId { get; set; }

    /// <summary>
    /// An optional set of MSBuild properties used when interpreting project files. These
    ///  are the same properties that are passed to MSBuild via the /property:&lt;n&gt;=&lt;v&gt;
    ///  command line argument.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; }

    /// <summary>
    /// Disables generation of view source links.
    /// </summary>
    [JsonPropertyName("disableGitFeatures")]
    public bool DisableGitFeatures { get; set; }

    /// <summary>
    /// Specify the base directory that is used to resolve code source (e.g. `&lt;code source="Example.cs"&gt;`).
    /// </summary>
    [JsonPropertyName("codeSourceBasePath")]
    public string CodeSourceBasePath { get; set; }

    /// <summary>
    /// Disables the default filter configuration file.
    /// </summary>
    [JsonPropertyName("disableDefaultFilter")]
    public bool DisableDefaultFilter { get; set; }

    /// <summary>
    /// Do not run dotnet restore before building the projects.
    /// </summary>
    [JsonPropertyName("noRestore")]
    public bool NoRestore { get; set; }

    /// <summary>
    /// Defines how namespaces in TOC are organized:
    /// - `flattened` (default): Renders namespaces as a single flat list.
    /// - `nested`: Renders namespaces in a nested tree form.
    /// </summary>
    [JsonPropertyName("namespaceLayout")]
    public NamespaceLayout NamespaceLayout { get; set; }

    /// <summary>
    /// Defines how member pages are organized:
    /// - `samePage` (default): Places members in the same page as their containing type.
    /// - `separatePages`: Places members in separate pages.
    /// </summary>
    [JsonPropertyName("memberLayout")]
    public MemberLayout MemberLayout { get; set; }

    /// <summary>
    /// Defines how member pages are organized:
    /// - `samePage` (default): Places members in the same page as their containing type.
    /// - `separatePages`: Places members in separate pages.
    /// </summary>
    [JsonPropertyName("enumSortOrder")]
    public EnumSortOrder EnumSortOrder { get; init; }

    /// <summary>
    /// When enabled, continues documentation generation in case of compilation errors.
    /// </summary>
    [JsonPropertyName("allowCompilationErrors")]
    public bool AllowCompilationErrors { get; set; }
}

/// <summary>
/// MetadataJsonItemConfig
/// </summary>
internal class MetadataJsonConfig : List<MetadataJsonItemConfig>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataJsonConfig"/> class.
    /// </summary>
    public MetadataJsonConfig(IEnumerable<MetadataJsonItemConfig> configs) : base(configs) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataJsonConfig"/> class.
    /// </summary>
    public MetadataJsonConfig(params MetadataJsonItemConfig[] configs) : base(configs)
    {
    }
}
