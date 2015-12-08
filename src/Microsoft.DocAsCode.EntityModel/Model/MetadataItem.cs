// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class MetadataItem : ICloneable
    {
        [YamlIgnore]
        [JsonIgnore]
        public bool IsInvalid { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public string RawComment { get; set; }

        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Name { get; set; }

        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        [YamlMember(Alias = "language")]
        [JsonProperty("language")]
        public SyntaxLanguage Language { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public SortedList<SyntaxLanguage, string> DisplayNames { get; set; }

        [YamlMember(Alias = "qualifiedName")]
        [JsonProperty("qualifiedName")]
        public SortedList<SyntaxLanguage, string> DisplayQualifiedNames { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public MetadataItem Parent { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public MemberType Type { get; set; }

        [YamlMember(Alias = "assemblies")]
        [JsonProperty("assemblies")]
        public List<string> AssemblyNameList { get; set; }

        [YamlMember(Alias = "namespace")]
        [JsonProperty("namespace")]
        public string NamespaceName { get; set; }

        [YamlMember(Alias = "source")]
        [JsonProperty("source")]
        public SourceDetail Source { get; set; }

        [YamlMember(Alias = "documentation")]
        [JsonProperty("documentation")]
        public SourceDetail Documentation { get; set; }

        public List<LayoutItem> Layout { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "remarks")]
        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "example")]
        [JsonProperty("example")]
        public List<string> Examples { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public SyntaxDetail Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        public string Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        [JsonProperty("exceptions")]
        public List<CrefInfo> Exceptions { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<CrefInfo> Sees { get; set; }

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<CrefInfo> SeeAlsos { get; set; }

        [YamlMember(Alias = "inheritance")]
        [JsonProperty("inheritance")]
        public List<string> Inheritance { get; set; }

        [YamlMember(Alias = "implements")]
        [JsonProperty("implements")]
        public List<string> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        [JsonProperty("inheritedMembers")]
        public List<string> InheritedMembers { get; set; }

        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public List<MetadataItem> Items { get; set; }

        [YamlMember(Alias = "references")]
        [JsonProperty("references")]
        public Dictionary<string, ReferenceItem> References { get; set; }

        [YamlIgnore]
        [JsonIgnore]
        public TripleSlashCommentModel CommentModel { get; set; }

        public override string ToString()
        {
            return Type + ": " + Name;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
