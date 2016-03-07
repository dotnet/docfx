// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins.ViewModels
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Exceptions;

    using Newtonsoft.Json;

    [Serializable]
    public class ApiParameterViewModel
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Name { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public ReferenceViewModel Type { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        public static ApiParameterViewModel FromModel(ApiParameter model, Dictionary<string, ReferenceViewModel> references)
        {
            var vm = new ApiParameterViewModel
            {
                Name = model.Name,
                Description = model.Description
            };

            ReferenceViewModel rvm;
            if (!references.TryGetValue(model.Type, out rvm))
            {
                rvm = new ReferenceViewModel
                {
                    Uid = model.Type
                };
            }

            vm.Type = rvm;
            return vm;
        }
    }
}
