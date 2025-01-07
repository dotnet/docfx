// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.RestApi.Swagger;

public class OperationObject
{
    /// <summary>
    /// Unique string used to identify the operation. The id MUST be unique among all operations described in the API. Tools and libraries MAY use the operationId to uniquely identify an operation, therefore, it is recommended to follow common programming naming conventions.
    /// </summary>
    [YamlMember(Alias = "operationId")]
    [JsonProperty("operationId")]
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = "tags")]
    [JsonProperty("tags")]
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    [JsonPropertyName("parameters")]
    public List<ParameterObject> Parameters { get; set; }

    /// <summary>
    /// Key: `default` or HttpStatusCode
    /// </summary>
    [YamlMember(Alias = "responses")]
    [JsonProperty("responses")]
    [JsonPropertyName("responses")]
    public Dictionary<string, ResponseObject> Responses { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
