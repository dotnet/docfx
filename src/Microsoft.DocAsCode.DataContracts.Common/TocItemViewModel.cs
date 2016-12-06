// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
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
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        public string Uid { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "tags")]
        [JsonProperty("tags")]
        public HashSet<string> Tags { get; set; }

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

        [YamlMember(Alias = Constants.PropertyName.Href)]
        [JsonProperty(Constants.PropertyName.Href)]
        public string Href { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public string OriginalHref { get; set; }

        [YamlMember(Alias = "tocHref")]
        [JsonProperty("tocHref")]
        public string TocHref { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public string OriginalTocHref { get; set; }

        [YamlMember(Alias = "topicHref")]
        [JsonProperty("topicHref")]
        public string TopicHref { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public string OriginalTopicHref { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public string AggregatedHref { get; set; }

        [YamlMember(Alias = "homepage")]
        [JsonProperty("homepage")]
        public string Homepage { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public string OriginalHomepage { get; set; }

        [YamlMember(Alias = "homepageUid")]
        [JsonProperty("homepageUid")]
        public string HomepageUid { get; set; }

        [YamlMember(Alias = "topicUid")]
        [JsonProperty("topicUid")]
        public string TopicUid { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public string AggregatedUid { get; set; }

        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public TocViewModel Items { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public bool IsHrefUpdated { get; set; }

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
    }
}
