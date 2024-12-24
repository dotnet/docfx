// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.Common;

public class OverwriteDocumentModel
{
    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>
    /// The uid for this overwrite document, as defined in YAML header
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    /// <summary>
    /// The markdown content from the overwrite document
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Conceptual)]
    [JsonProperty(Constants.PropertyName.Conceptual)]
    [JsonPropertyName(Constants.PropertyName.Conceptual)]
    public string Conceptual { get; set; }

    /// <summary>
    /// The details for current overwrite document, containing the start/end line numbers, file path, and git info.
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonProperty(Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    /// <summary>
    /// Links to other files
    /// </summary>
    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public HashSet<string> LinkToFiles { get; set; } = [];

    /// <summary>
    /// Links to other Uids
    /// </summary>
    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public HashSet<string> LinkToUids { get; set; } = [];

    /// <summary>
    /// Link sources information for file
    /// </summary>
    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, List<LinkSourceInfo>> FileLinkSources { get; set; } = [];

    /// <summary>
    /// Link sources information for Uid
    /// </summary>
    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, List<LinkSourceInfo>> UidLinkSources { get; set; } = [];

    /// <summary>
    /// Dependencies extracted from the markdown content
    /// </summary>
    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public ImmutableArray<string> Dependency { get; set; } = [];

    public T ConvertTo<T>() where T : class
    {
        using var sw = new StringWriter();
        YamlUtility.Serialize(sw, this);
        using var sr = new StringReader(sw.ToString());
        return YamlUtility.Deserialize<T>(sr);
    }
}
