// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ApiBuildOutput
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "isEii")]
        [JsonProperty("isEii")]
        public bool IsExplicitInterfaceImplementation { get; set; }

        [YamlMember(Alias = "isExtensionMethod")]
        [JsonProperty("isExtensionMethod")]
        public bool IsExtensionMethod { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public ApiReferenceBuildOutput Parent { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<ApiReferenceBuildOutput> Children { get; set; }

        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public string[] SupportedLanguages { get; set; } = new string[] { "csharp", "vb" };

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

        [YamlMember(Alias = "assemblies")]
        [JsonProperty("assemblies")]
        public List<string> AssemblyNameList { get; set; }

        [YamlMember(Alias = "namespace")]
        [JsonProperty("namespace")]
        public ApiReferenceBuildOutput NamespaceName { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; } = null;

        [YamlMember(Alias = "remarks")]
        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = Constants.PropertyName.AdditionalNotes)]
        [JsonProperty(Constants.PropertyName.AdditionalNotes)]
        public AdditionalNotes AdditionalNotes { get; set; }

        [YamlMember(Alias = "example")]
        [JsonProperty("example")]
        public List<string> Examples { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public ApiSyntaxBuildOutput Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        public ApiNames Overridden { get; set; }

        [YamlMember(Alias = "overload")]
        [JsonProperty("overload")]
        public ApiNames Overload { get; set; }

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
        public int Level { get { return Inheritance != null ? Inheritance.Count : 0; } }

        [YamlMember(Alias = "implements")]
        [JsonProperty("implements")]
        public List<ApiNames> Implements { get; set; }

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

        [YamlMember(Alias = "attributes")]
        [JsonProperty("attributes")]
        public List<AttributeInfo> Attributes { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public static ApiBuildOutput FromModel(PageViewModel model)
        {
            if (model == null || model.Items == null || model.Items.Count == 0)
            {
                return null;
            }

            if (model.Metadata.TryGetValue("_displayLangs", out object displayLangs))
            {
                if (displayLangs is object[] langsObj)
                {
                    var langs = langsObj.Select(x => x?.ToString()).ToArray();
                    model.Items.ForEach(item => item.SupportedLanguages = IntersectLangs(item.SupportedLanguages, langs));
                }
                else
                {
                    Logger.LogWarning("Invalid displayLangs setting in docfx.json. Ignored.");
                }
            }

            var metadata = model.Metadata;
            var references = new Dictionary<string, ApiReferenceBuildOutput>();

            foreach (var item in model.References)
            {
                if (!string.IsNullOrEmpty(item.Uid))
                {
                    references[item.Uid] = ApiReferenceBuildOutput.FromModel(item, model.Items[0].SupportedLanguages);
                }
            }

            // Add other items to reference, override the one in reference with same uid
            foreach (var item in model.Items.Skip(1))
            {
                if (!string.IsNullOrEmpty(item.Uid))
                {
                    references[item.Uid] = ApiReferenceBuildOutput.FromModel(item);
                }
            }

            return FromModel(model.Items[0], references, metadata);
        }

        private static ApiBuildOutput FromModel(ItemViewModel model, Dictionary<string, ApiReferenceBuildOutput> references, Dictionary<string, object> metadata)
        {
            if (model == null) return null;

            var output = new ApiBuildOutput
            {
                Uid = model.Uid,
                IsExplicitInterfaceImplementation = model.IsExplicitInterfaceImplementation,
                IsExtensionMethod = model.IsExtensionMethod,
                Parent = ApiBuildOutputUtility.GetReferenceViewModel(model.Parent, references, model.SupportedLanguages),
                Children = GetReferenceList(model.Children, references, model.SupportedLanguages),
                Href = model.Href,
                SupportedLanguages = model.SupportedLanguages,
                Name = ApiBuildOutputUtility.TransformToLanguagePairList(model.Name, model.Names, model.SupportedLanguages),
                NameWithType = ApiBuildOutputUtility.TransformToLanguagePairList(model.NameWithType, model.NamesWithType, model.SupportedLanguages),
                FullName = ApiBuildOutputUtility.TransformToLanguagePairList(model.FullName, model.FullNames, model.SupportedLanguages),
                Type = model.Type,
                Source = model.Source,
                Documentation = model.Documentation,
                AssemblyNameList = model.AssemblyNameList,
                NamespaceName = ApiBuildOutputUtility.GetReferenceViewModel(model.NamespaceName, references, model.SupportedLanguages),
                Summary = model.Summary,
                AdditionalNotes = model.AdditionalNotes,
                Remarks = model.Remarks,
                Examples = model.Examples,
                Syntax = ApiSyntaxBuildOutput.FromModel(model.Syntax, references, model.SupportedLanguages),
                Overridden = ApiBuildOutputUtility.GetApiNames(model.Overridden, references, model.SupportedLanguages),
                Overload = ApiBuildOutputUtility.GetApiNames(model.Overload, references, model.SupportedLanguages),
                Exceptions = GetCrefInfoList(model.Exceptions, references, model.SupportedLanguages),
                SeeAlsos = GetLinkInfoList(model.SeeAlsos, references, model.SupportedLanguages),
                Sees = GetLinkInfoList(model.Sees, references, model.SupportedLanguages),
                Inheritance = GetReferenceList(model.Inheritance, references, model.SupportedLanguages, true),
                Implements = model.Implements?.Select(u => ApiBuildOutputUtility.GetApiNames(u, references, model.SupportedLanguages)).ToList(),
                InheritedMembers = GetReferenceList(model.InheritedMembers, references, model.SupportedLanguages),
                ExtensionMethods = GetReferenceList(model.ExtensionMethods, references, model.SupportedLanguages),
                Conceptual = model.Conceptual,
                Platform = model.Platform,
                Attributes = model.Attributes,
                Metadata = model.Metadata.Concat(metadata.Where(p => !model.Metadata.Keys.Contains(p.Key))).ToDictionary(p => p.Key, p => p.Value),
            };
            output.DerivedClasses = GetReferenceList(model.DerivedClasses, references, model.SupportedLanguages, true, output.Level + 1);
            return output;
        }

        private static List<ApiReferenceBuildOutput> GetReferenceList(List<string> uids,
                                                                      Dictionary<string, ApiReferenceBuildOutput> references,
                                                                      string[] supportedLanguages,
                                                                      bool extractIndex = false,
                                                                      int level = -1)
        {
            if (extractIndex)
            {
                if (level != -1)
                {
                    return uids?.Select(u => ApiBuildOutputUtility.GetReferenceViewModel(u, references, supportedLanguages, level)).ToList();
                }
                return uids?.Select((u, i) => ApiBuildOutputUtility.GetReferenceViewModel(u, references, supportedLanguages, i)).ToList();
            }
            else
            {
                return uids?.Select(u => ApiBuildOutputUtility.GetReferenceViewModel(u, references, supportedLanguages)).ToList();
            }
        }

        private static List<ApiExceptionInfoBuildOutput> GetCrefInfoList(List<ExceptionInfo> crefs,
                                                                    Dictionary<string, ApiReferenceBuildOutput> references,
                                                                    string[] supportedLanguages)
        {
            return crefs?.Select(c => ApiExceptionInfoBuildOutput.FromModel(c, references, supportedLanguages)).ToList();
        }

        private static List<ApiLinkInfoBuildOutput> GetLinkInfoList(List<LinkInfo> links,
                                                                    Dictionary<string, ApiReferenceBuildOutput> references,
                                                                    string[] supportedLanguages)
        {
            return links?.Select(l => ApiLinkInfoBuildOutput.FromModel(l, references, supportedLanguages)).ToList();
        }

        private static string[] IntersectLangs(string[] defaultLangs, string[] displayLangs)
        {
            if (displayLangs.Length == 0)
            {
                return defaultLangs;
            }

            return defaultLangs?.Intersect(displayLangs, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
