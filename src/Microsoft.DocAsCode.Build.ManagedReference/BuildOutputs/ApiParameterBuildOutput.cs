// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    [Serializable]
    public class ApiParameterBuildOutput
    {
        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Name { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public ApiReferenceBuildOutput Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        ApiExpandStatus status = ApiExpandStatus.UnExpanded;

        public static ApiParameterBuildOutput FromModel(ApiParameter model, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (model == null) return null;

            return new ApiParameterBuildOutput
            {
                Name = model.Name,
                Type = ApiBuildOutputUtility.GetReferenceViewModel(model.Type, references, supportedLanguages),
                Description = model.Description,
            };
        }

        public static ApiParameterBuildOutput FromModel(ApiParameter model)
        {
            if (model == null) return null;

            return new ApiParameterBuildOutput
            {
                Name = model.Name,
                Type = ApiReferenceBuildOutput.FromUid(model.Type),
                Description = model.Description,
            };
        }

        public void Expand(Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (status == ApiExpandStatus.UnExpanded)
            {
                status = ApiExpandStatus.IsExpanding;
                Type = ApiBuildOutputUtility.GetReferenceViewModel(Type?.Uid, references, supportedLanguages);
                status = ApiExpandStatus.Expanded;
            }
        }
    }
}
