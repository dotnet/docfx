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
    public class ReferenceViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        public string Uid { get; set; }

        [YamlMember(Alias = Constants.PropertyName.CommentId)]
        [JsonProperty(Constants.PropertyName.CommentId)]
        public string CommentId { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "definition")]
        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("isExternal")]
        [YamlMember(Alias = "isExternal")]
        public bool? IsExternal { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Href)]
        [JsonProperty(Constants.PropertyName.Href)]
        public string Href { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [ExtensibleMember("name.")]
        [JsonIgnore]
        public SortedList<string, string> NameInDevLangs { get; } = new SortedList<string, string>();

        [YamlMember(Alias = "nameWithType")]
        [JsonProperty("nameWithType")]
        public string NameWithType { get; set; }

        [ExtensibleMember("nameWithType.")]
        [JsonIgnore]
        public SortedList<string, string> NameWithTypeInDevLangs { get; } = new SortedList<string, string>();

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [ExtensibleMember("fullname.")]
        [JsonIgnore]
        public SortedList<string, string> FullNameInDevLangs { get; } = new SortedList<string, string>();

        [ExtensibleMember("spec.")]
        [JsonIgnore]
        public SortedList<string, List<SpecViewModel>> Specs { get; } = new SortedList<string, List<SpecViewModel>>();

        [ExtensibleMember]
        [JsonIgnore]
        public Dictionary<string, object> Additional { get; } = new Dictionary<string, object>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData(ReadData = false, WriteData = true)]
        [UniqueIdentityReferenceIgnore]
        [MarkdownContentIgnore]
        public Dictionary<string, object> AdditionalJson
        {
            get
            {
                var dict = new Dictionary<string, object>();
                foreach (var item in NameInDevLangs)
                {
                    dict["name." + item.Key] = item.Value;
                }
                foreach (var item in NameWithTypeInDevLangs)
                {
                    dict["nameWithType." + item.Key] = item.Value;
                }
                foreach (var item in FullNameInDevLangs)
                {
                    dict["fullname." + item.Key] = item.Value;
                }
                foreach (var item in Specs)
                {
                    dict["spec." + item.Key] = item.Value;
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
