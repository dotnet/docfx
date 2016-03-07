// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ApiPageViewModel
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<ReferenceViewModel> Children { get; set; }

        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public string[] SupportedLanguages { get; set; } = new string[] { "csharp", "vb" };

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "name.csharp")]
        [JsonProperty("name.csharp")]
        public string NameForCSharp { get; set; }

        [YamlMember(Alias = "name.vb")]
        [JsonProperty("name.vb")]
        public string NameForVB { get; set; }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [YamlMember(Alias = "fullName.csharp")]
        [JsonProperty("fullName.csharp")]
        public string FullNameForCSharp { get; set; }

        [YamlMember(Alias = "fullName.vb")]
        [JsonProperty("fullName.vb")]
        public string FullNameForVB { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = "source")]
        [JsonProperty("source")]
        public SourceDetail Source { get; set; }

        [YamlMember(Alias = "documentation")]
        [JsonProperty("documentation")]
        public SourceDetail Documentation { get; set; }

        [YamlMember(Alias = "assemblies")]
        [JsonProperty("assemblies")]
        public List<string> AssemblyNameList { get; set; }

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
        public ApiSyntaxViewModel Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        public string Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        [JsonProperty("exceptions")]
        public List<CrefInfoViewModel> Exceptions { get; set; }

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<CrefInfoViewModel> SeeAlsos { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<CrefInfoViewModel> Sees { get; set; }

        [YamlMember(Alias = "inheritance")]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty("inheritance")]
        public List<ReferenceViewModel> Inheritance { get; set; }

        [YamlMember(Alias = "implements")]
        [JsonProperty("implements")]
        public List<ReferenceViewModel> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        [JsonProperty("inheritedMembers")]
        public List<ReferenceViewModel> InheritedMembers { get; set; }

        [YamlMember(Alias = "conceptual")]
        [JsonProperty("conceptual")]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "platform")]
        [JsonProperty("platform")]
        public List<string> Platform { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public static ApiPageViewModel FromModel(PageViewModel model)
        {
            if (model == null || model.Items == null || model.Items.Count == 0)
            {
                return null;
            }

            var metadata = model.Metadata;
            var references = new Dictionary<string, ReferenceViewModel>();

            foreach(var item in model.References)
            {
                references[item.Uid] = item;
            }

            // Add other items to reference, override the one in reference with same uid
            foreach (var item in model.Items.Skip(1))
            {
                references[item.Uid] = FromItemViewModel(item);
            }

            return FromModel(model.Items[0], references);
        }

        public static ApiPageViewModel FromModel(ItemViewModel model, Dictionary<string, ReferenceViewModel> references)
        {
            return new ApiPageViewModel
            {
                Uid = model.Uid,
                Id = model.Id,
                Parent = model.Parent,
                Children = model.Children.Select(s => GetReferenceViewModel(s, references)).ToList(),
                Href = model.Href,
                SupportedLanguages = model.SupportedLanguages,
                Name = model.Name,
                NameForCSharp = model.NameForCSharp,
                NameForVB = model.NameForVB,
                FullName = model.FullName,
                FullNameForCSharp = model.FullNameForCSharp,
                FullNameForVB = model.FullNameForVB,
                Type = model.Type,
                Source = model.Source,
                Documentation = model.Documentation,
                AssemblyNameList = model.AssemblyNameList,
                NamespaceName = model.NamespaceName,
                Summary = model.Summary,
                Remarks = model.Remarks,
                Examples = model.Examples,
                Syntax = ApiSyntaxViewModel.FromModel(model.Syntax, references),
                Overridden = model.Overridden,
                Exceptions = model.Exceptions?.Select(s => CrefInfoViewModel.FromModel(s, references)).ToList(),
                SeeAlsos = model.SeeAlsos?.Select(s => CrefInfoViewModel.FromModel(s, references)).ToList(),
                Sees = model.Sees?.Select(s => CrefInfoViewModel.FromModel(s, references)).ToList(),
                Inheritance = model.Inheritance?.Select(s => GetReferenceViewModel(s, references)).ToList(),
                Implements = model.Implements?.Select(s => GetReferenceViewModel(s, references)).ToList(),
                InheritedMembers = model.InheritedMembers?.Select(s => GetReferenceViewModel(s, references)).ToList(),
                Conceptual = model.Conceptual,
                Platform = model.Platform,
                Metadata = model.Metadata,
            };
        }

        private static ReferenceViewModel FromItemViewModel(ItemViewModel vm)
        {
            return new ReferenceViewModel
            {
                Uid = vm.Uid,
                Parent = vm.Parent,
                IsExternal = false,
                Href = vm.Href,
                Name = vm.Name,
                NameForCSharp = vm.NameForCSharp,
                NameForVB = vm.NameForVB,
                FullName = vm.FullName,
                FullNameForCSharp = vm.FullNameForCSharp,
                FullNameForVB = vm.FullNameForVB,
                Summary = vm.Summary,
                Syntax = vm.Syntax,
                Platform = vm.Platform,
            };
        }

        private static ReferenceViewModel GetReferenceViewModel(string key, Dictionary<string, ReferenceViewModel> references)
        {
            ReferenceViewModel rvm;
            if (!references.TryGetValue(key, out rvm))
            {
                rvm = new ReferenceViewModel
                {
                    Uid = key
                };
            }

            return rvm;
        }
    }
}
