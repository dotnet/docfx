// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins.ViewModels
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class CrefInfoViewModel
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public ReferenceViewModel Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        public static CrefInfoViewModel FromModel(CrefInfo model, Dictionary<string, ReferenceViewModel> references)
        {
            ReferenceViewModel rvm;
            if (!references.TryGetValue(model.Type, out rvm))
            {
                rvm = new ReferenceViewModel
                {
                    Uid = model.Type
                };
            }

            return new CrefInfoViewModel
            {
                Type = rvm,
                Description = model.Description
            };
        }
    }
}
