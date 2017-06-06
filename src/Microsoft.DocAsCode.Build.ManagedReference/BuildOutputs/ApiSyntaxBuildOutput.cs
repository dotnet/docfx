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
                TypeParameters = model.TypeParameters?.Select(s => ApiParameterBuildOutput.FromModel(s)).ToList(),
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
                Parameters = model.Parameters?.Select(s => ApiParameterBuildOutput.FromModel(s)).ToList(),
                TypeParameters = model.TypeParameters?.Select(s => ApiParameterBuildOutput.FromModel(s)).ToList(),
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
}
