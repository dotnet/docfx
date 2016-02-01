// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class TocItemViewModel
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [ExtensibleMember("name.")]
        [JsonIgnore]
        public SortedList<string, string> NameInDevLangs { get; } = new SortedList<string, string>();

        [YamlIgnore]
        [JsonIgnore]
        public string NameForCSharp
        {
            get
            {
                string result;
                NameInDevLangs.TryGetValue("csharp", out result);
                return result;
            }
            set { NameInDevLangs["csharp"] = value; }
        }

        [YamlIgnore]
        [JsonIgnore]
        public string NameForVB
        {
            get
            {
                string result;
                NameInDevLangs.TryGetValue("vb", out result);
                return result;
            }
            set { NameInDevLangs["vb"] = value; }
        }

        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        [YamlMember(Alias = "originalHref")]
        [JsonProperty("originalHref")]
        public string OriginalHref { get; set; }

        [YamlMember(Alias = "homepage")]
        [JsonProperty("homepage")]
        public string Homepage { get; set; }

        [YamlMember(Alias = "homepageUid")]
        [JsonProperty("homepageUid")]
        public string HomepageUid { get; set; }

        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public TocViewModel Items { get; set; }

        [ExtensibleMember]
        [JsonIgnore]
        public Dictionary<string, object> Additional { get; set; } = new Dictionary<string, object>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData(ReadData = false, WriteData = true)]
        public Dictionary<string, object> AdditionalJson
        {
            get
            {
                var dict = new Dictionary<string, object>();
                foreach (var item in NameInDevLangs)
                {
                    dict["name." + item.Key] = item.Value;
                }
                foreach (var item in Additional)
                {
                    dict[item.Key] = item.Value;
                }
                return dict;
            }
            set { }
        }

        public static TocItemViewModel FromModel(MetadataItem item)
        {
            var result = new TocItemViewModel
            {
                Uid = item.Name,
                Name = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default),
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
