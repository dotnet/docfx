// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiRootItemViewModel : RestApiItemViewModelBase
{
    [YamlMember(Alias = "securityDefinitions")]
    [JsonProperty("securityDefinitions", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("securityDefinitions")]
    public Dictionary<string, RestApiSecuritySchemeViewModel> SecurityDefinitions { get; set; }

    [YamlMember(Alias = "info")]
    [JsonProperty("info", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("info")]
    public RestApiInfoViewModel Info { get; set; }

    [YamlMember(Alias = "externalDocs")]
    [JsonProperty("externalDocs", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("externalDocs")]
    public RestApiExternalDocumentationViewModel ExternalDocs { get; set; }

    /// <summary>
    /// The original swagger.json content
    /// `_` prefix indicates that this metadata is generated
    /// </summary>
    [YamlMember(Alias = "_raw")]
    [JsonProperty("_raw")]
    [JsonPropertyName("_raw")]
    [MergeOption(MergeOption.Ignore)]
    public string Raw { get; set; }

    [YamlMember(Alias = "tags")]
    [JsonProperty("tags")]
    [JsonPropertyName("tags")]
    public List<RestApiTagViewModel> Tags { get; set; }

    [YamlMember(Alias = "children")]
    [JsonProperty("children")]
    [JsonPropertyName("children")]
    public List<RestApiChildItemViewModel> Children { get; set; }
}
