// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiSyntaxViewModel
    {
        [YamlMember(Alias = "content")]
        [JsonProperty("content")]
        public string Content { get; set; }

        [YamlMember(Alias = "content.csharp")]
        [JsonProperty("content.csharp")]
        public string ContentForCSharp { get; set; }

        [YamlMember(Alias = "content.vb")]
        [JsonProperty("content.vb")]
        public string ContentForVB { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<ApiParameterViewModel> Parameters { get; set; }

        [YamlMember(Alias = "typeParameters")]
        [JsonProperty("typeParameters")]
        public List<ApiParameterViewModel> TypeParameters { get; set; }

        [YamlMember(Alias = "return")]
        [JsonProperty("return")]
        public ApiParameterViewModel Return { get; set; }

        public static ApiSyntaxViewModel FromModel(SyntaxDetailViewModel model, Dictionary<string, ReferenceViewModel> references)
        {
            if (model == null)
            {
                return null;
            }

            return new ApiSyntaxViewModel
            {
                Content = model.Content,
                ContentForCSharp = model.ContentForCSharp,
                ContentForVB = model.ContentForVB,
                Parameters = model.Parameters.Select(s => ApiParameterViewModel.FromModel(s, references)).ToList(),
                TypeParameters = model.TypeParameters.Select(s => ApiParameterViewModel.FromModel(s, references)).ToList(),
                Return = ApiParameterViewModel.FromModel(model.Return, references),
            };
        }

    }
}
