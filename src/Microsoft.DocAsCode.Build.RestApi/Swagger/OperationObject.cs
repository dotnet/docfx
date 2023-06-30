// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.RestApi.Swagger;

[Serializable]
public class OperationObject
{
    /// <summary>
    /// Unique string used to identify the operation. The id MUST be unique among all operations described in the API. Tools and libraries MAY use the operationId to uniquely identify an operation, therefore, it is recommended to follow common programming naming conventions.
    /// </summary>
    [YamlMember(Alias = "operationId")]
    [JsonProperty("operationId")]
    public string OperationId { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = "tags")]
    [JsonProperty("tags")]
    public List<string> Tags { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    public List<ParameterObject> Parameters { get; set; }

    /// <summary>
    /// Key: `default` or HttpStatusCode
    /// </summary>
    [YamlMember(Alias = "responses")]
    [JsonProperty("responses")]
    public Dictionary<string, ResponseObject> Responses { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
