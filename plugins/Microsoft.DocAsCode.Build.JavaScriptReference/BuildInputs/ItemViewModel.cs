// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.DataContracts.Common;
    using CommonConstants = Microsoft.DocAsCode.DataContracts.Common.Constants;
    using Microsoft.DocAsCode.YamlSerialization;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ItemViewModel : IOverwriteDocumentViewModel
    {
        [YamlMember(Alias = CommonConstants.PropertyName.Uid)]
        [JsonProperty(CommonConstants.PropertyName.Uid)]
        [MergeOption(MergeOption.MergeKey)]
        public string Uid { get; set; }

        [YamlMember(Alias = CommonConstants.PropertyName.Id)]
        [JsonProperty(CommonConstants.PropertyName.Id)]
        public string Id { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "children")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("children")]
        public List<string> Children { get; set; }

        [YamlMember(Alias = CommonConstants.PropertyName.Href)]
        [JsonProperty(CommonConstants.PropertyName.Href)]
        public string Href { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public string[] SupportedLanguages { get; set; } = { Constants.JavaScriptDevLang};

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "nameWithType")]
        [JsonProperty("nameWithType")]
        public string NameWithType { get; set; }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [YamlMember(Alias = CommonConstants.PropertyName.Type)]
        [JsonProperty(CommonConstants.PropertyName.Type)]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = CommonConstants.PropertyName.Source)]
        [JsonProperty(CommonConstants.PropertyName.Source)]
        public SourceDetail Source { get; set; }

        [YamlMember(Alias = CommonConstants.PropertyName.Documentation)]
        [JsonProperty(CommonConstants.PropertyName.Documentation)]
        public SourceDetail Documentation { get; set; }

        // Can be used to save npm package name
        [YamlMember(Alias = "packages")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
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
        [MergeOption(MergeOption.Replace)]
        public List<string> Examples { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public SyntaxDetailViewModel Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        public string Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        [JsonProperty("exceptions")]
        public List<ExceptionInfo> Exceptions { get; set; }

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<LinkInfo> SeeAlsos { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<LinkInfo> Sees { get; set; }

        [YamlMember(Alias = "inheritance")]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty("inheritance")]
        public List<string> Inheritance { get; set; }

        [YamlMember(Alias = "derivedClasses")]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty("derivedClasses")]
        public List<string> DerivedClasses { get; set; }

        [YamlMember(Alias = "implements")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("implements")]
        public List<string> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("inheritedMembers")]
        public List<string> InheritedMembers { get; set; }

        // TODO: should methods attatched to prototype be put here?
        [YamlMember(Alias = "extensionMethods")]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty("extensionMethods")]
        public List<string> ExtensionMethods { get; set; }

        [YamlMember(Alias = CommonConstants.PropertyName.Conceptual)]
        [JsonProperty(CommonConstants.PropertyName.Conceptual)]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "platform")]
        [JsonProperty("platform")]
        [MergeOption(MergeOption.Replace)]
        public List<string> Platform { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
