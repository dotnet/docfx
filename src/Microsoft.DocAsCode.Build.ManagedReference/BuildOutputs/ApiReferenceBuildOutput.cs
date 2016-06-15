// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ApiReferenceBuildOutput
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [YamlMember(Alias = "isEii")]
        [JsonProperty("isEii")]
        public bool IsExplicitInterfaceImplementation { get; set; }

        [YamlMember(Alias = "isExtensionMethod")]
        [JsonProperty("isExtensionMethod")]
        public bool IsExtensionMethod { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "definition")]
        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("isExternal")]
        [YamlMember(Alias = "isExternal")]
        public bool? IsExternal { get; set; }

        [YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public List<ApiLanguageValuePair> Name { get; set; }

        [YamlMember(Alias = "nameWithType")]
        [JsonProperty("nameWithType")]
        public List<ApiLanguageValuePair> NameWithType { get; set; }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public List<ApiLanguageValuePair> FullName { get; set; }

        [YamlMember(Alias = "specName")]
        [JsonProperty("specName")]
        public List<ApiLanguageValuePair> Spec { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public ApiSyntaxBuildOutput Syntax { get; set; }

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

        [YamlMember(Alias = "remarks")]
        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "example")]
        [JsonProperty("example")]
        public List<string> Examples { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        public ApiNames Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        [JsonProperty("exceptions")]
        public List<ApiCrefInfoBuildOutput> Exceptions { get; set; }

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<ApiCrefInfoBuildOutput> SeeAlsos { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<ApiCrefInfoBuildOutput> Sees { get; set; }

        [YamlMember(Alias = "inheritance")]
        [JsonProperty("inheritance")]
        public List<ApiReferenceBuildOutput> Inheritance { get; set; }

        [YamlMember(Alias = "level")]
        [JsonProperty("level")]
        public int Level { get { return Inheritance != null ? Inheritance.Count : 0; } }

        [YamlMember(Alias = "implements")]
        [JsonProperty("implements")]
        public List<ApiNames> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        [JsonProperty("inheritedMembers")]
        public List<string> InheritedMembers { get; set; }

        [YamlMember(Alias = "extensionMethods")]
        [JsonProperty("extensionMethods")]
        public List<string> ExtensionMethods { get; set; }

        [ExtensibleMember("modifiers.")]
        [JsonIgnore]
        public SortedList<string, List<string>> Modifiers { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = "conceptual")]
        [JsonProperty("conceptual")]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "index")]
        [JsonProperty("index")]
        public int? Index { get; set; }

        [ExtensibleMember]
        [JsonIgnore]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData(ReadData = false, WriteData = true)]
        public Dictionary<string, object> MetadataJson
        {
            get
            {
                var dict = new Dictionary<string, object>();
                foreach (var item in Modifiers)
                {
                    dict["modifiers." + item.Key] = item.Value;
                }
                foreach (var item in Metadata)
                {
                    dict[item.Key] = item.Value;
                }
                return dict;
            }
            set { }
        }

        private bool _needExpand = true;

        public static ApiReferenceBuildOutput FromUid(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;

            return new ApiReferenceBuildOutput
            {
                Uid = uid,
            };
        }

        public static ApiReferenceBuildOutput FromModel(ReferenceViewModel vm, string[] supportedLanguages)
        {
            if (vm == null) return null;

            // TODO: may lead to potential problems with have vm.Additional["syntax"] as SyntaxDetailViewModel
            // It is now working as syntax is set only in FillReferenceInformation and not from YAML deserialization
            var result = new ApiReferenceBuildOutput
            {
                Uid = vm.Uid,
                Id = Utility.GetHtmlId(vm.Uid),
                Parent = vm.Parent,
                Definition = vm.Definition,
                IsExternal = vm.IsExternal,
                Href = vm.Href,
                Name = ApiBuildOutputUtility.TransformToLanguagePairList(vm.Name, vm.NameInDevLangs, supportedLanguages),
                NameWithType = ApiBuildOutputUtility.TransformToLanguagePairList(vm.NameWithType, vm.NameWithTypeInDevLangs, supportedLanguages),
                FullName = ApiBuildOutputUtility.TransformToLanguagePairList(vm.FullName, vm.FullNameInDevLangs, supportedLanguages),
                Spec = GetSpecNames(ApiBuildOutputUtility.GetXref(vm.Uid, vm.FullName, vm.Name), supportedLanguages, vm.Specs),
                Metadata = vm.Additional,
            };
            object syntax;
            if (result.Metadata.TryGetValue("syntax", out syntax))
            {
                result.Syntax = ApiSyntaxBuildOutput.FromModel(syntax as SyntaxDetailViewModel, supportedLanguages);
                result.Metadata.Remove("syntax");
            }
            return result;
        }

        public static ApiReferenceBuildOutput FromModel(ItemViewModel vm)
        {
            if (vm == null) return null;

            var output = new ApiReferenceBuildOutput
            {
                Uid = vm.Uid,
                Id = Utility.GetHtmlId(vm.Uid),
                IsExplicitInterfaceImplementation = vm.IsExplicitInterfaceImplementation,
                IsExtensionMethod = vm.IsExtensionMethod,
                Parent = vm.Parent,
                IsExternal = false,
                Href = vm.Href,
                Name = ApiBuildOutputUtility.TransformToLanguagePairList(vm.Name, vm.Names, vm.SupportedLanguages),
                NameWithType = ApiBuildOutputUtility.TransformToLanguagePairList(vm.NameWithType, vm.NamesWithType, vm.SupportedLanguages),
                FullName = ApiBuildOutputUtility.TransformToLanguagePairList(vm.FullName, vm.FullNames, vm.SupportedLanguages),
                Spec = GetSpecNames(ApiBuildOutputUtility.GetXref(vm.Uid, vm.FullName, vm.Name), vm.SupportedLanguages),
                Source = vm.Source,
                Documentation = vm.Documentation,
                AssemblyNameList = vm.AssemblyNameList,
                NamespaceName = vm.NamespaceName,
                Remarks = vm.Remarks,
                Examples = vm.Examples,
                Overridden = ApiNames.FromUid(vm.Overridden),
                SeeAlsos = vm.SeeAlsos?.Select(s => ApiCrefInfoBuildOutput.FromModel(s)).ToList(),
                Sees = vm.Sees?.Select(s => ApiCrefInfoBuildOutput.FromModel(s)).ToList(),
                Inheritance = vm.Inheritance?.Select(i => FromUid(i)).ToList(),
                Implements = vm.Implements?.Select(i => ApiNames.FromUid(i)).ToList(),
                InheritedMembers = vm.InheritedMembers,
                ExtensionMethods = vm.ExtensionMethods,
                Modifiers = vm.Modifiers,
                Conceptual = vm.Conceptual,
                Metadata = vm.Metadata,
                Syntax = ApiSyntaxBuildOutput.FromModel(vm.Syntax, vm.SupportedLanguages),
                Exceptions = vm.Exceptions?.Select(s => ApiCrefInfoBuildOutput.FromModel(s)).ToList(),
            };
            output.Metadata["type"] = vm.Type;
            output.Metadata["summary"] = vm.Summary;
            output.Metadata["platform"] = vm.Platform;
            return output;
        }

        public void Expand(Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
        {
            if (_needExpand)
            {
                _needExpand = false;
                Inheritance = Inheritance?.Select(i => ApiBuildOutputUtility.GetReferenceViewModel(i.Uid, references, supportedLanguages)).ToList();
                Implements = Implements?.Select(i => ApiBuildOutputUtility.GetApiNames(i.Uid, references, supportedLanguages)).ToList();
                Syntax?.Expand(references, supportedLanguages);
                Overridden = ApiBuildOutputUtility.GetApiNames(Overridden?.Uid, references, supportedLanguages);
                SeeAlsos?.ForEach(e => e.Expand(references, supportedLanguages));
                Sees?.ForEach(e => e.Expand(references, supportedLanguages));
                Exceptions?.ForEach(e => e.Expand(references, supportedLanguages));
            }
        }

        public static List<ApiLanguageValuePair> GetSpecNames(string xref, string[] supportedLanguages, SortedList<string, List<SpecViewModel>> specs = null)
        {
            if (specs != null && specs.Count > 0)
            {
                return specs.Select(kv => new ApiLanguageValuePair() { Language = kv.Key, Value = GetSpecName(kv.Value) }).ToList();
            }
            if (!string.IsNullOrEmpty(xref))
            {
                return supportedLanguages.Select(s => new ApiLanguageValuePair() { Language = s, Value = xref }).ToList();
            }
            return null;
        }

        private static string GetSpecName(List<SpecViewModel> spec)
        {
            if (spec == null) return null;
            return string.Concat(spec.Select(s => GetCompositeName(s)));
        }

        private static string GetCompositeName(SpecViewModel svm)
        {
            // If href does not exists, return full name
            if (string.IsNullOrEmpty(svm.Uid)) { return System.Web.HttpUtility.HtmlEncode(svm.FullName); }

            // If href exists, return name with href
            return ApiBuildOutputUtility.GetXref(svm.Uid, svm.FullName, svm.Name);
        }
    }
}
