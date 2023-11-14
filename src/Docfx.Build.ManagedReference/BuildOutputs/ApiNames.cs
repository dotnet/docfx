// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.ManagedReference.BuildOutputs;

public class ApiNames
{
    [YamlMember(Alias = "uid")]
    [JsonProperty("uid")]
    [JsonPropertyName("uid")]
    public string Uid { get; set; }

    [YamlMember(Alias = "id")]
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [YamlMember(Alias = "definition")]
    [JsonProperty("definition")]
    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public List<ApiLanguageValuePair> Name { get; set; }

    [YamlMember(Alias = "nameWithType")]
    [JsonProperty("nameWithType")]
    [JsonPropertyName("nameWithType")]
    public List<ApiLanguageValuePair> NameWithType { get; set; }

    [YamlMember(Alias = "fullName")]
    [JsonProperty("fullName")]
    [JsonPropertyName("fullName")]
    public List<ApiLanguageValuePair> FullName { get; set; }

    [YamlMember(Alias = "specName")]
    [JsonProperty("specName")]
    [JsonPropertyName("specName")]
    public List<ApiLanguageValuePair> Spec { get; set; }

    public static ApiNames FromUid(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            return null;
        }
        return new ApiNames
        {
            Uid = uid,
        };
    }
}
