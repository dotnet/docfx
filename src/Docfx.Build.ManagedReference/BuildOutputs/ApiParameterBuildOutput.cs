// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.ManagedReference;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.ManagedReference.BuildOutputs;

public class ApiParameterBuildOutput
{
    [YamlMember(Alias = "id")]
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Name { get; set; }

    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public ApiNames Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    private bool _needExpand = true;

    public static ApiParameterBuildOutput FromModel(ApiParameter model)
    {
        if (model == null)
        {
            return null;
        }
        return new ApiParameterBuildOutput
        {
            Name = model.Name,
            Type = ApiNames.FromUid(model.Type),
            Description = model.Description,
        };
    }

    public static ApiParameterBuildOutput FromModel(ApiParameter model, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
    {
        if (model == null)
        {
            return null;
        }
        return new ApiParameterBuildOutput
        {
            Name = model.Name,
            Type = ApiBuildOutputUtility.GetApiNames(model.Type, references, supportedLanguages),
            Description = model.Description,
            _needExpand = false,
        };
    }

    public void Expand(Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
    {
        if (_needExpand)
        {
            _needExpand = false;
            Type = ApiBuildOutputUtility.GetApiNames(Type?.Uid, references, supportedLanguages);
        }
    }
}
