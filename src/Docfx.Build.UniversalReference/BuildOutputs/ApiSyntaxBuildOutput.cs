// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiSyntaxBuildOutput
{
    [YamlMember(Alias = Constants.PropertyName.Content)]
    [JsonProperty(Constants.PropertyName.Content)]
    [JsonPropertyName(Constants.PropertyName.Content)]
    public List<ApiLanguageValuePair<string>> Content { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    [JsonPropertyName("parameters")]
    public List<ApiParameterBuildOutput> Parameters { get; set; }

    [YamlMember(Alias = "typeParameters")]
    [JsonProperty("typeParameters")]
    [JsonPropertyName("typeParameters")]
    public List<ApiParameterBuildOutput> TypeParameters { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Return)]
    [JsonProperty(Constants.PropertyName.Return)]
    [JsonPropertyName(Constants.PropertyName.Return)]
    public List<ApiLanguageValuePair<ApiParameterBuildOutput>> Return { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
