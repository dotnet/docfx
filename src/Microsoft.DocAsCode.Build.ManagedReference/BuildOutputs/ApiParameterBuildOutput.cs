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
        public ApiNames Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
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
}
