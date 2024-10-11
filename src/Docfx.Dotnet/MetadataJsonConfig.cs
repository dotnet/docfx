// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

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
/// Specifies the layout of categories.
/// </summary>
internal enum CategoryLayout
{
    /// <summary>
    /// Renders the categories as a a plain label.
    /// </summary>
    Flattened,

    /// <summary>
    /// Renders the categories in a nested tree form.
    /// </summary>
    Nested,

    /// <summary>
    /// Don't render category labels.
    /// </summary>
    None,
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
    [JsonProperty("src")]
    [JsonPropertyName("src")]
    public FileMapping Src { get; set; }

    /// <summary>
    /// Defines the output folder of the generated metadata files.
    /// Command line --output argument prepends this value.
    /// </summary>
    [JsonProperty("dest")]
    [JsonPropertyName("dest")]
    public string Dest { get; set; }

    /// <summary>
    /// Defines the output folder of the generated metadata files.
    /// Command line --output argument override this value.
    /// </summary>
    [JsonProperty("output")]
    [JsonPropertyName("output")]
    public string Output { get; set; }

    /// <summary>
    /// Defines the output file format.
    /// </summary>
    [JsonProperty("outputFormat")]
    [JsonPropertyName("outputFormat")]
    public MetadataOutputFormat OutputFormat { get; set; }

    /// <summary>
    /// If set to true, DocFX would not render triple-slash-comments in source code as markdown.
    /// </summary>
    [JsonProperty("shouldSkipMarkup")]
    [JsonPropertyName("shouldSkipMarkup")]
    public bool? ShouldSkipMarkup { get; set; }

    /// <summary>
    /// Specify additinal assembly reference files.
    /// This settings is used when generating metadata from DLLs or source files.
    /// Solution or project file-based metadata generation does not use this property.
    /// </summary>
    [JsonProperty("references")]
    [JsonPropertyName("references")]
    public FileMapping References { get; set; }

    /// <summary>
    /// Defines the filter configuration file.
    /// </summary>
    [JsonProperty("filter")]
    [JsonPropertyName("filter")]
    public string Filter { get; set; }

    /// <summary>
    /// Include private or internal APIs.
    /// The default is false.
    /// </summary>
    [JsonProperty("includePrivateMembers")]
    [JsonPropertyName("includePrivateMembers")]
    public bool IncludePrivateMembers { get; set; }

    /// <summary>
    /// Include explicit interface implementations.
    /// The default is false.
    /// </summary>
    [JsonProperty("includeExplicitInterfaceImplementations")]
    [JsonPropertyName("includeExplicitInterfaceImplementations")]
    public bool IncludeExplicitInterfaceImplementations { get; set; }

    /// <summary>
    /// Specify the name to use for the global namespace.
    /// </summary>
    [JsonProperty("globalNamespaceId")]
    [JsonPropertyName("globalNamespaceId")]
    public string GlobalNamespaceId { get; set; }

    /// <summary>
    /// An optional set of MSBuild properties used when interpreting project files. These
    ///  are the same properties that are passed to MSBuild via the /property:&lt;n&gt;=&lt;v&gt;
    ///  command line argument.
    /// </summary>
    [JsonProperty("properties")]
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; }

    /// <summary>
    /// Disables generation of view source links.
    /// </summary>
    [JsonProperty("disableGitFeatures")]
    [JsonPropertyName("disableGitFeatures")]
    public bool DisableGitFeatures { get; set; }

    /// <summary>
    /// Specify the base directory that is used to resolve code source (e.g. `&lt;code source="Example.cs"&gt;`).
    /// </summary>
    [JsonProperty("codeSourceBasePath")]
    [JsonPropertyName("codeSourceBasePath")]
    public string CodeSourceBasePath { get; set; }

    /// <summary>
    /// Disables the default filter configuration file.
    /// </summary>
    [JsonProperty("disableDefaultFilter")]
    [JsonPropertyName("disableDefaultFilter")]
    public bool DisableDefaultFilter { get; set; }

    /// <summary>
    /// Do not run dotnet restore before building the projects.
    /// </summary>
    [JsonProperty("noRestore")]
    [JsonPropertyName("noRestore")]
    public bool NoRestore { get; set; }

    /// <summary>
    /// Defines how categories in TOC are organized:
    /// - `flattened` (default): Renders categories as a plain label.
    /// - `nested`: Renders categories in a nested tree form.
    /// - `none`: Don't render category labels.
    /// </summary>
    [JsonProperty("categoryLayout")]
    [JsonPropertyName("categoryLayout")]
    public CategoryLayout CategoryLayout { get; set; }

    /// <summary>
    /// Defines how namespaces in TOC are organized:
    /// - `flattened` (default): Renders namespaces as a single flat list.
    /// - `nested`: Renders namespaces in a nested tree form.
    /// </summary>
    [JsonProperty("namespaceLayout")]
    [JsonPropertyName("namespaceLayout")]
    public NamespaceLayout NamespaceLayout { get; set; }

    /// <summary>
    /// Defines how member pages are organized:
    /// - `samePage` (default): Places members in the same page as their containing type.
    /// - `separatePages`: Places members in separate pages.
    /// </summary>
    [JsonProperty("memberLayout")]
    [JsonPropertyName("memberLayout")]
    public MemberLayout MemberLayout { get; set; }

    /// <summary>
    /// Specifies how enum members are sorted:
    /// - `alphabetic`(default): Sort enum members in alphabetic order.
    /// - `declaringOrder`: Sort enum members in the order as they are declared in the source code.
    /// </summary>
    [JsonProperty("enumSortOrder")]
    [JsonPropertyName("enumSortOrder")]
    public EnumSortOrder EnumSortOrder { get; init; }

    /// <summary>
    /// When enabled, continues documentation generation in case of compilation errors.
    /// </summary>
    [JsonProperty("allowCompilationErrors")]
    [JsonPropertyName("allowCompilationErrors")]
    public bool AllowCompilationErrors { get; set; }

    /// <summary>
    ///   When enabled, the types uses the CLR type names instead of the C# aliases.
    /// </summary>
    [JsonProperty("useClrTypeNames")]
    [JsonPropertyName("useClrTypeNames")]
    public bool UseClrTypeNames { get; init; }
}

/// <summary>
/// MetadataJsonItemConfig
/// </summary>
internal class MetadataJsonConfig : List<MetadataJsonItemConfig>
{
    // Constructor that required for System.Text.Json deserialization.
    public MetadataJsonConfig() { }

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
