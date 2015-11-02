// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using Newtonsoft.Json;
    using System;
    using YamlDotNet.Serialization;

    [Serializable]
    public class TocItemViewModel
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }
        [YamlMember(Alias = "name.csharp")]
        [JsonProperty("name.csharp")]
        public string NameForCSharp { get; set; }
        [YamlMember(Alias = "name.vb")]
        [JsonProperty("name.vb")]
        public string NameForVB { get; set; }
        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }
        [YamlMember(Alias = "homepage")]
        [JsonProperty("homepage")]
        public string Homepage { get; set; }
        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public TocViewModel Items { get; set; }

        public static TocItemViewModel FromModel(MetadataItem item)
        {
            var result = new TocItemViewModel
            {
                Uid = item.Name,
                Name = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default),
                Href = item.Href,
            };
            var nameForCSharp = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (nameForCSharp != result.Name)
            {
                result.NameForCSharp = nameForCSharp;
            }
            var nameForVB = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.VB);
            if (nameForVB != result.Name)
            {
                result.NameForVB = nameForVB;
            }
            if (item.Items != null)
            {
                result.Items = TocViewModel.FromModel(item);
            }
            return result;
        }
    }
}
