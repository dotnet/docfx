// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using Newtonsoft.Json;
    using System;
    using YamlDotNet.Serialization;

    [Serializable]
    public class SpecViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        public string Uid { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [YamlMember(Alias = "isExternal")]
        [JsonProperty("isExternal")]
        public bool IsExternal { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Href)]
        [JsonProperty(Constants.PropertyName.Href)]
        public string Href { get; set; }

        public static SpecViewModel FromModel(LinkItem model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new SpecViewModel
            {
                Uid = model.Name,
                Name = model.DisplayName,
                FullName = model.DisplayQualifiedNames,
                IsExternal = model.IsExternalPath,
                Href = model.Href,
            };
            return result;
        }
    }
}
