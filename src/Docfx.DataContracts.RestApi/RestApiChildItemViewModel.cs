// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiChildItemViewModel : RestApiItemViewModelBase
{
    [YamlMember(Alias = Constants.PropertyName.Path)]
    [JsonProperty(Constants.PropertyName.Path)]
    [JsonPropertyName(Constants.PropertyName.Path)]
    public string Path { get; set; }

    /// <summary>
    /// operation name, for example get, put, post, delete, options, head and patch.
    /// </summary>
    [YamlMember(Alias = "operation")]
    [JsonProperty("operation")]
    [JsonPropertyName("operation")]
    public string OperationName { get; set; }

    [YamlMember(Alias = "operationId")]
    [JsonProperty("operationId")]
    [JsonPropertyName("operationId")]
    public string OperationId { get; set; }

    [YamlMember(Alias = "tags")]
    [JsonProperty("tags")]
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    [JsonPropertyName("parameters")]
    public List<RestApiParameterViewModel> Parameters { get; set; }

    [YamlMember(Alias = "responses")]
    [JsonProperty("responses")]
    [JsonPropertyName("responses")]
    public List<RestApiResponseViewModel> Responses { get; set; }
}
