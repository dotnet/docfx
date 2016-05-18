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
    public class ApiCrefInfoBuildOutput
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public ApiTypeAndSpec Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        private bool _needExpand = true;

        public static ApiCrefInfoBuildOutput FromModel(CrefInfo model)
        {
            if (model == null) return null;

            return new ApiCrefInfoBuildOutput
            {
                Type = new ApiTypeAndSpec { Uid = model.Type },
                Description = model.Description,
            };
        }

        public static ApiCrefInfoBuildOutput FromModel(CrefInfo model, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (model == null) return null;

            return new ApiCrefInfoBuildOutput
            {
                Type = new ApiTypeAndSpec
                {
                    Uid = model.Type,
                    Spec = ApiBuildOutputUtility.GetSpec(model.Type, references, supportedLanguages),
                },
                Description = model.Description,
                _needExpand = false,
            };
        }

        public void Expand(Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (_needExpand)
            {
                _needExpand = false;
                Type.Spec = ApiBuildOutputUtility.GetSpec(Type.Uid, references, supportedLanguages);
            }
        }
    }
}
