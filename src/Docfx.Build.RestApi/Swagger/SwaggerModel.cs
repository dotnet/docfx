// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.RestApi.Swagger;

public class SwaggerModel
{
    /// <summary>
    /// The original swagger.json content
    /// </summary>
    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Raw { get; set; }

    /// <summary>
    /// Required. Provides metadata about the API. The metadata can be used by the clients if needed.
    /// </summary>
    [YamlMember(Alias = "info")]
    [JsonProperty("info")]
    [JsonPropertyName("info")]
    public InfoObject Info { get; set; }

    /// <summary>
    /// The host (name or ip) serving the API. This MUST be the host only and does not include the scheme nor sub-paths. It MAY include a port. If the host is not included, the host serving the documentation is to be used (including the port). The host does not support path templating.
    /// </summary>
    [YamlMember(Alias = "host")]
    [JsonProperty("host")]
    [JsonPropertyName("host")]
    public string Host { get; set; }

    /// <summary>
    /// The base path on which the API is served, which is relative to the host. If it is not included, the API is served directly under the host. The value MUST start with a leading slash (/). The basePath does not support path templating.
    /// </summary>
    [YamlMember(Alias = "basePath")]
    [JsonProperty("basePath")]
    [JsonPropertyName("basePath")]
    public string BasePath { get; set; }

    /// <summary>
    /// Required. The available paths and operations for the API.
    /// </summary>
    [YamlMember(Alias = "paths")]
    [JsonProperty("paths")]
    [JsonPropertyName("paths")]
    public PathsObject Paths { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    /// <summary>
    /// An object to hold data types produced and consumed by operations.
    /// </summary>
    [YamlMember(Alias = "definitions")]
    [JsonProperty("definitions")]
    [JsonPropertyName("definitions")]
    public object Definitions { get; set; }

    /// <summary>
    /// An object to hold parameters that can be used across operations. This property does not define global parameters for all operations
    /// </summary>
    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    [JsonPropertyName("parameters")]
    public object Parameters { get; set; }

    /// <summary>
    /// An object to hold responses that can be used across operations. This property does not define global responses for all operations.
    /// </summary>
    [YamlMember(Alias = "responses")]
    [JsonProperty("responses")]
    [JsonPropertyName("responses")]
    public object Responses { get; set; }

    /// <summary>
    /// A list of tags used by the specification with additional metadata. The order of the tags can be used to reflect on their order by the parsing tools.
    /// </summary>
    [YamlMember(Alias = "tags")]
    [JsonProperty("tags")]
    [JsonPropertyName("tags")]
    public List<TagItemObject> Tags { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
