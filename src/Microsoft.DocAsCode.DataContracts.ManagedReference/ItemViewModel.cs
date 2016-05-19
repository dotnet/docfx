// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ItemViewModel : IOverwriteDocumentViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        [MergeOption(MergeOption.MergeKey)]
        public string Uid { get; set; }

        [YamlMember(Alias = Constants.PropertyName.CommentId)]
        [JsonProperty(Constants.PropertyName.CommentId)]
        public string CommentId { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Id)]
        [JsonProperty(Constants.PropertyName.Id)]
        public string Id { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "children")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("children")]
        public List<string> Children { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Href)]
        [JsonProperty(Constants.PropertyName.Href)]
        public string Href { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public string[] SupportedLanguages { get; set; } = new string[] { "csharp", "vb" };

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [ExtensibleMember("name.")]
        [JsonIgnore]
        public SortedList<string, string> Names { get; set; } = new SortedList<string, string>();

        [YamlIgnore]
        [JsonIgnore]
        public string NameForCSharp
        {
            get
            {
                string result;
                Names.TryGetValue("csharp", out result);
                return result;
            }
            set
            {
                if (value == null)
                {
                    Names.Remove("csharp");
                }
                else
                {
                    Names["csharp"] = value;
                }
            }
        }

        [YamlIgnore]
        [JsonIgnore]
        public string NameForVB
        {
            get
            {
                string result;
                Names.TryGetValue("vb", out result);
                return result;
            }
            set
            {
                if (value == null)
                {
                    Names.Remove("vb");
                }
                else
                {
                    Names["vb"] = value;
                }
            }
        }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [ExtensibleMember("fullName.")]
        [JsonIgnore]
        public SortedList<string, string> FullNames { get; set; } = new SortedList<string, string>();

        [YamlIgnore]
        [JsonIgnore]
        public string FullNameForCSharp
        {
            get
            {
                string result;
                FullNames.TryGetValue("csharp", out result);
                return result;
            }
            set
            {
                if (value == null)
                {
                    FullNames.Remove("csharp");
                }
                else
                {
                    FullNames["csharp"] = value;
                }
            }
        }

        [YamlIgnore]
        [JsonIgnore]
        public string FullNameForVB
        {
            get
            {
                string result;
                FullNames.TryGetValue("vb", out result);
                return result;
            }
            set
            {
                if (value == null)
                {
                    FullNames.Remove("vb");
                }
                else
                {
                    FullNames["vb"] = value;
                }
            }
        }

        [YamlMember(Alias = Constants.PropertyName.Type)]
        [JsonProperty(Constants.PropertyName.Type)]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Source)]
        [JsonProperty(Constants.PropertyName.Source)]
        public SourceDetail Source { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Documentation)]
        [JsonProperty(Constants.PropertyName.Documentation)]
        public SourceDetail Documentation { get; set; }

        [YamlMember(Alias = "assemblies")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("assemblies")]
        public List<string> AssemblyNameList { get; set; }

        [YamlMember(Alias = "packages")]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty("packages")]
        public List<string> PackageNameList { get; set; }

        [YamlMember(Alias = "namespace")]
        [JsonProperty("namespace")]
        public string NamespaceName { get; set; }

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
        public SyntaxDetailViewModel Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        public string Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        [JsonProperty("exceptions")]
        public List<CrefInfo> Exceptions { get; set; }

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<CrefInfo> SeeAlsos { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<CrefInfo> Sees { get; set; }

        [YamlMember(Alias = "inheritance")]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty("inheritance")]
        public List<string> Inheritance { get; set; }

        [YamlMember(Alias = "implements")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("implements")]
        public List<string> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("inheritedMembers")]
        public List<string> InheritedMembers { get; set; }

        [ExtensibleMember("modifiers.")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonIgnore]
        public SortedList<string, List<string>> Modifiers { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = Constants.PropertyName.Conceptual)]
        [JsonProperty(Constants.PropertyName.Conceptual)]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "platform")]
        [JsonProperty("platform")]
        public List<string> Platform { get; set; }

        [YamlMember(Alias = "attributes")]
        [JsonProperty("attributes")]
        [MergeOption(MergeOption.Ignore)]
        public List<AttributeInfo> Attributes { get; set; }

        [ExtensibleMember]
        [JsonIgnore]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData(WriteData = true, ReadData = false)]
        public Dictionary<string, object> ExtensionData
        {
            get
            {
                var result = new Dictionary<string, object>();
                foreach (var item in Names)
                {
                    result["name." + item.Key] = item.Value;
                }
                foreach (var item in FullNames)
                {
                    result["fullName." + item.Key] = item.Value;
                }
                foreach (var item in Modifiers)
                {
                    result["modifier." + item.Key] = item.Value;
                }
                foreach (var item in Metadata)
                {
                    result[item.Key] = item.Value;
                }
                return result;
            }
        }
    }
}
