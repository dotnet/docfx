// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    [Serializable]
    public class ApiSyntaxBuildOutput
    {
        [YamlMember(Alias = "content")]
        [JsonProperty("content")]
        public List<ApiLanguageValuePair> Content { get; set; } = new List<ApiLanguageValuePair>();

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<ApiParameterBuildOutput> Parameters { get; set; }

        [YamlMember(Alias = "typeParameters")]
        [JsonProperty("typeParameters")]
        public List<ApiParameterBuildOutput> TypeParameters { get; set; }

        [YamlMember(Alias = "return")]
        [JsonProperty("return")]
        public ApiParameterBuildOutput Return { get; set; }

        ApiExpandStatus status = ApiExpandStatus.UnExpanded;

        public static ApiSyntaxBuildOutput FromModel(SyntaxDetailViewModel model, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (model == null) return null;

            return new ApiSyntaxBuildOutput
            {
                Content = GetContents(model.Content, model.ContentForCSharp, model.ContentForVB, supportedLanguages),
                Parameters = model.Parameters?.Select(s => ApiParameterBuildOutput.FromModel(s, references, supportedLanguages)).ToList(),
                TypeParameters = model.TypeParameters?.Select(s => ApiParameterBuildOutput.FromModel(s)).ToList(),
                Return = ApiParameterBuildOutput.FromModel(model.Return, references, supportedLanguages),
                status = ApiExpandStatus.Expanded,
            };
        }


        public static ApiSyntaxBuildOutput FromModel(SyntaxDetailViewModel model, string[] supportedLanguages)
        {
            if (model == null) return null;

            return new ApiSyntaxBuildOutput
            {
                Content = GetContents(model.Content, model.ContentForCSharp, model.ContentForVB, supportedLanguages),
                Parameters = model.Parameters?.Select(s => ApiParameterBuildOutput.FromModel(s)).ToList(),
                TypeParameters = model.TypeParameters?.Select(s => ApiParameterBuildOutput.FromModel(s)).ToList(),
                Return = ApiParameterBuildOutput.FromModel(model.Return),
            };
        }

        public void Expand(Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (status == ApiExpandStatus.UnExpanded)
            {
                status = ApiExpandStatus.IsExpanding;
                Parameters?.ForEach(p => p.Expand(references, supportedLanguages));
                TypeParameters?.ForEach(t => t.Expand(references, supportedLanguages));
                Return?.Expand(references, supportedLanguages);
                status = ApiExpandStatus.Expanded;
            }
        }

        private static List<ApiLanguageValuePair> GetContents(string content, string contentForCSharp, string contentForVB, string[] supportedLanguages)
        {
            if (string.IsNullOrEmpty(content) || supportedLanguages == null || supportedLanguages.Length == 0) return null;

            var result = new List<ApiLanguageValuePair>() { new ApiLanguageValuePair() { Language = supportedLanguages[0], Value = content } };
            if (!string.IsNullOrEmpty(contentForCSharp)) result.Add(new ApiLanguageValuePair() { Language = "csharp", Value = contentForCSharp });
            if (!string.IsNullOrEmpty(contentForVB)) result.Add(new ApiLanguageValuePair() { Language = "vb", Value = contentForVB });
            return result;
        }
    }
}
