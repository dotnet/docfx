// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

        private static T? Merge<T>(T? source, T? target) where T : struct
        {
            Debug.Assert(source == null || target == null || Nullable.Equals(source, target));
            return source ?? target;
        }

        private static T Merge<T>(T source, T target) where T : class
        {
            Debug.Assert(source == null || target == null || object.Equals(source, target));
            return source ?? target;
        }

        public void Merge(ReferenceItem other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            Type = Merge(other.Type, Type);
            Summary = Summary ?? other.Summary;
            IsDefinition = Merge(other.IsDefinition, IsDefinition);
            Definition = Merge(other.Definition, Definition);
            Parent = Merge(other.Parent, Parent);

            if (other.Parts != null && Parts != null)
            {
                foreach (var pair in other.Parts)
                {
                    var sourceParts = pair.Value;
                    List<LinkItem> targetParts;
                    if (Parts.TryGetValue(pair.Key, out targetParts))
                    {
                        Debug.Assert(sourceParts.Count == targetParts.Count);
                        for (int i = 0; i < sourceParts.Count; i++)
                        {
                            Debug.Assert(sourceParts[i].Name == targetParts[i].Name);
                            Debug.Assert(sourceParts[i].DisplayName == targetParts[i].DisplayName);
                            Debug.Assert(sourceParts[i].DisplayQualifiedNames == targetParts[i].DisplayQualifiedNames);
                            targetParts[i].IsExternalPath &= sourceParts[i].IsExternalPath;
                            targetParts[i].Href = targetParts[i].Href ?? sourceParts[i].Href;
                        }
                    }
                    else
                    {
                        Parts.Add(pair.Key, pair.Value);
                    }
                }
            }
            else
            {
                Parts = Parts ?? other.Parts;
            }
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
