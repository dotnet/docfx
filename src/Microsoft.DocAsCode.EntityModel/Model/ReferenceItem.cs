// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using YamlDotNet.Serialization;

    public class ReferenceItem
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public SortedList<SyntaxLanguage, List<LinkItem>> Parts { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "isDefinition")]
        [JsonProperty("isDefinition")]
        public bool? IsDefinition { get; set; }

        [YamlMember(Alias = "definition")]
        [JsonProperty("definition")]
        public string Definition { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public string Parent { get; set; }

        public ReferenceItem Clone()
        {
            var result = (ReferenceItem)MemberwiseClone();
            if (Parts != null)
            {
                var dict = new SortedList<SyntaxLanguage, List<LinkItem>>(Parts.Count);
                foreach (var item in Parts)
                {
                    dict.Add(item.Key, (from x in item.Value select x.Clone()).ToList());
                }
                result.Parts = dict;
            }

            return result;
        }
    }

    public class LinkItem
    {
        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Name { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string DisplayName { get; set; }

        [YamlMember(Alias = "qualifiedName")]
        [JsonProperty("qualifiedName")]
        public string DisplayQualifiedNames { get; set; }

        /// <summary>
        /// The external path for current source if it is not locally available
        /// </summary>
        [YamlMember(Alias = "isExternal")]
        [JsonProperty("isExternal")]
        public bool IsExternalPath { get; set; }

        /// <summary>
        /// The url path for current source, should be resolved at some late stage
        /// </summary>
        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        public LinkItem Clone()
        {
            return (LinkItem)MemberwiseClone();
        }
    }
}
