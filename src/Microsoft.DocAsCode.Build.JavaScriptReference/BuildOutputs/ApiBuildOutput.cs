// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ApiBuildOutput
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public ApiReferenceBuildOutput Parent { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<ApiBuildOutput> Children { get; set; }

        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public string[] SupportedLanguages { get; set; } = { Constants.DevLang.JavaScript };

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public List<ApiLanguageValuePair> Name { get; set; }

        [YamlMember(Alias = "nameWithType")]
        [JsonProperty("nameWithType")]
        public List<ApiLanguageValuePair> NameWithType { get; set; }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public List<ApiLanguageValuePair> FullName { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = "source")]
        [JsonProperty("source")]
        public SourceDetail Source { get; set; }

        [YamlMember(Alias = "documentation")]
        [JsonProperty("documentation")]
        public SourceDetail Documentation { get; set; }

        [YamlMember(Alias = "packages")]
        [JsonProperty("packages")]
        public List<string> PackageNameList { get; set; }

        [YamlMember(Alias = "namespace")]
        [JsonProperty("namespace")]
        public ApiReferenceBuildOutput NamespaceName { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; } = null;

        [YamlMember(Alias = "remarks")]
        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "example")]
        [JsonProperty("example")]
        public List<string> Examples { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public ApiSyntaxBuildOutput Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        public ApiReferenceBuildOutput Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        [JsonProperty("exceptions")]
        public List<ApiExceptionInfoBuildOutput> Exceptions { get; set; }

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<ApiLinkInfoBuildOutput> SeeAlsos { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<ApiLinkInfoBuildOutput> Sees { get; set; }

        [YamlMember(Alias = "inheritance")]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty("inheritance")]
        public List<ApiReferenceBuildOutput> Inheritance { get; set; }

        [YamlMember(Alias = "derivedClasses")]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty("derivedClasses")]
        public List<ApiReferenceBuildOutput> DerivedClasses { get; set; }

        [YamlMember(Alias = "level")]
        [JsonProperty("level")]
        public int Level => Inheritance?.Count ?? 0;

        [YamlMember(Alias = "implements")]
        [JsonProperty("implements")]
        public List<ApiReferenceBuildOutput> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        [JsonProperty("inheritedMembers")]
        public List<ApiReferenceBuildOutput> InheritedMembers { get; set; }

        [YamlMember(Alias = "extensionMethods")]
        [JsonProperty("extensionMethods")]
        public List<ApiReferenceBuildOutput> ExtensionMethods { get; set; }

        [YamlMember(Alias = "conceptual")]
        [JsonProperty("conceptual")]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "platform")]
        [JsonProperty("platform")]
        public List<string> Platform { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
