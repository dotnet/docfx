// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;
using YamlDotNet.Serialization;

namespace Docfx.Build.RestApi.Swagger;

public class OperationObject
{
    /// <summary>
    /// Unique string used to identify the operation. The id MUST be unique among all operations described in the API. Tools and libraries MAY use the operationId to uniquely identify an operation, therefore, it is recommended to follow common programming naming conventions.
    /// </summary>
    [YamlMember(Alias = "operationId")]
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = "tags")]
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonPropertyName("parameters")]
    public List<ParameterObject> Parameters { get; set; }

    /// <summary>
    /// Key: `default` or HttpStatusCode
    /// </summary>
    [YamlMember(Alias = "responses")]
    [JsonPropertyName("responses")]
    public Dictionary<string, ResponseObject> Responses { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
