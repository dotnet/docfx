// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.ManagedReference;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.ManagedReference.BuildOutputs;

public class ApiSyntaxBuildOutput
{
    [YamlMember(Alias = "content")]
    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public List<ApiLanguageValuePair> Content { get; set; } = [];

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    [JsonPropertyName("parameters")]
    public List<ApiParameterBuildOutput> Parameters { get; set; }

    [YamlMember(Alias = "typeParameters")]
    [JsonProperty("typeParameters")]
    [JsonPropertyName("typeParameters")]
    public List<ApiParameterBuildOutput> TypeParameters { get; set; }

    [YamlMember(Alias = "return")]
    [JsonProperty("return")]
    [JsonPropertyName("return")]
    public ApiParameterBuildOutput Return { get; set; }

    private bool _needExpand = true;

    public static ApiSyntaxBuildOutput FromModel(SyntaxDetailViewModel model, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
    {
        if (model == null)
        {
            return null;
        }
        return new ApiSyntaxBuildOutput
        {
            Content = ApiBuildOutputUtility.TransformToLanguagePairList(model.Content, model.Contents, supportedLanguages),
            Parameters = model.Parameters?.Select(s => ApiParameterBuildOutput.FromModel(s, references, supportedLanguages)).ToList(),
            TypeParameters = model.TypeParameters?.Select(ApiParameterBuildOutput.FromModel).ToList(),
            Return = ApiParameterBuildOutput.FromModel(model.Return, references, supportedLanguages),
            _needExpand = false,
        };
    }

    public static ApiSyntaxBuildOutput FromModel(SyntaxDetailViewModel model, string[] supportedLanguages)
    {
        if (model == null)
        {
            return null;
        }
        return new ApiSyntaxBuildOutput
        {
            Content = ApiBuildOutputUtility.TransformToLanguagePairList(model.Content, model.Contents, supportedLanguages),
            Parameters = model.Parameters?.Select(ApiParameterBuildOutput.FromModel).ToList(),
            TypeParameters = model.TypeParameters?.Select(ApiParameterBuildOutput.FromModel).ToList(),
            Return = ApiParameterBuildOutput.FromModel(model.Return),
        };
    }

    public void Expand(Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
    {
        if (_needExpand)
        {
            _needExpand = false;
            Parameters?.ForEach(p => p.Expand(references, supportedLanguages));
            TypeParameters?.ForEach(t => t.Expand(references, supportedLanguages));
            Return?.Expand(references, supportedLanguages);
        }
    }
}
