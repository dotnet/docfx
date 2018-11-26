// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiBuildOutput
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        public string Uid { get; set; }

        [YamlMember(Alias = Constants.PropertyName.CommentId)]
        [JsonProperty(Constants.PropertyName.CommentId)]
        public string CommentId { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Parent)]
        [JsonProperty(Constants.PropertyName.Parent)]
        public List<ApiLanguageValuePair<ApiNames>> Parent { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Children)]
        [JsonProperty(Constants.PropertyName.Children)]
        public List<ApiLanguageValuePair<List<ApiBuildOutput>>> Children { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Href)]
        [JsonProperty(Constants.PropertyName.Href)]
        public string Href { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public string[] SupportedLanguages { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Name)]
        [JsonProperty(Constants.PropertyName.Name)]
        public List<ApiLanguageValuePair<string>> Name { get; set; }

        [YamlMember(Alias = Constants.PropertyName.NameWithType)]
        [JsonProperty(Constants.PropertyName.NameWithType)]
        public List<ApiLanguageValuePair<string>> NameWithType { get; set; }

        [YamlMember(Alias = Constants.PropertyName.FullName)]
        [JsonProperty(Constants.PropertyName.FullName)]
        public List<ApiLanguageValuePair<string>> FullName { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Type)]
        [JsonProperty(Constants.PropertyName.Type)]
        public string Type { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Source)]
        [JsonProperty(Constants.PropertyName.Source)]
        public List<ApiLanguageValuePair<SourceDetail>> Source { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Documentation)]
        [JsonProperty(Constants.PropertyName.Documentation)]
        public SourceDetail Documentation { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Assemblies)]
        [JsonProperty(Constants.PropertyName.Assemblies)]
        public List<ApiLanguageValuePair<List<string>>> AssemblyNameList { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Namespace)]
        [JsonProperty(Constants.PropertyName.Namespace)]
        public List<ApiLanguageValuePair<ApiNames>> NamespaceName { get; set; }

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

        [YamlMember(Alias = Constants.PropertyName.Overridden)]
        [JsonProperty(Constants.PropertyName.Overridden)]
        public List<ApiLanguageValuePair<ApiNames>> Overridden { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Overload)]
        [JsonProperty(Constants.PropertyName.Overload)]
        public List<ApiLanguageValuePair<ApiNames>> Overload { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Exceptions)]
        [JsonProperty(Constants.PropertyName.Exceptions)]
        public List<ApiLanguageValuePair<List<ApiExceptionInfoBuildOutput>>> Exceptions { get; set; }

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<ApiLinkInfoBuildOutput> SeeAlsos { get; set; }

        [YamlMember(Alias = Constants.PropertyName.SeeAlsoContent)]
        [JsonProperty(Constants.PropertyName.SeeAlsoContent)]
        public string SeeAlsoContent { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<ApiLinkInfoBuildOutput> Sees { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Inheritance)]
        [JsonProperty(Constants.PropertyName.Inheritance)]
        public List<ApiLanguageValuePair<List<ApiInheritanceTreeBuildOutput>>> Inheritance { get; set; }

        [YamlMember(Alias = Constants.PropertyName.DerivedClasses)]
        [JsonProperty(Constants.PropertyName.DerivedClasses)]
        public List<ApiLanguageValuePair<List<ApiNames>>> DerivedClasses { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Implements)]
        [JsonProperty(Constants.PropertyName.Implements)]
        public List<ApiLanguageValuePair<List<ApiNames>>> Implements { get; set; }

        [YamlMember(Alias = Constants.PropertyName.InheritedMembers)]
        [JsonProperty(Constants.PropertyName.InheritedMembers)]
        public List<ApiLanguageValuePair<List<ApiNames>>> InheritedMembers { get; set; }

        [YamlMember(Alias = Constants.PropertyName.ExtensionMethods)]
        [JsonProperty(Constants.PropertyName.ExtensionMethods)]
        public List<ApiLanguageValuePair<List<ApiNames>>> ExtensionMethods { get; set; }

        [YamlMember(Alias = "conceptual")]
        [JsonProperty("conceptual")]
        public string Conceptual { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Platform)]
        [JsonProperty(Constants.PropertyName.Platform)]
        public List<ApiLanguageValuePair<List<string>>> Platform { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
