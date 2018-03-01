// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Web;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ApiReferenceBuildOutput
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

        [YamlMember(Alias = Constants.PropertyName.AdditionalNotes)]
        [JsonProperty(Constants.PropertyName.AdditionalNotes)]
        public AdditionalNotes AdditionalNotes { get; set; }

        [YamlMember(Alias = "example")]
        [JsonProperty("example")]
        public List<string> Examples { get; set; }

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
        [JsonProperty("inheritance")]
        public List<ApiReferenceBuildOutput> Inheritance { get; set; }

        [YamlMember(Alias = "level")]
        [JsonProperty("level")]
        public int Level => Inheritance != null ? Inheritance.Count : 0;

        [YamlMember(Alias = "implements")]
        [JsonProperty("implements")]
        public List<ApiNames> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        [JsonProperty("inheritedMembers")]
        public List<string> InheritedMembers { get; set; }

        [YamlMember(Alias = "extensionMethods")]
        [JsonProperty("extensionMethods")]
        public List<string> ExtensionMethods { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.Modifiers)]
        [JsonIgnore]
        public SortedList<string, List<string>> Modifiers { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = "conceptual")]
        [JsonProperty("conceptual")]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "attributes")]
        [JsonProperty("attributes")]
        public List<AttributeInfo> Attributes { get; set; }

        [YamlMember(Alias = "index")]
        [JsonProperty("index")]
        public int? Index { get; set; }

        [ExtensibleMember]
        [JsonIgnore]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData]
        public CompositeDictionary MetadataJson =>
            CompositeDictionary
                .CreateBuilder()
                .Add(Constants.ExtensionMemberPrefix.Modifiers, Modifiers, JTokenConverter.Convert<List<string>>)
                .Add(string.Empty, Metadata)
                .Create();

        private bool _needExpand = true;

        public static ApiReferenceBuildOutput FromUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return null;
            }
            return new ApiReferenceBuildOutput
            {
                Uid = uid,
            };
        }

        public static ApiReferenceBuildOutput FromModel(ReferenceViewModel vm, string[] supportedLanguages)
        {
            if (vm == null)
            {
                return null;
            }
            // TODO: may lead to potential problems with have vm.Additional["syntax"] as SyntaxDetailViewModel
            // It is now working as syntax is set only in FillReferenceInformation and not from YAML deserialization
            var result = new ApiReferenceBuildOutput
            {
                Uid = vm.Uid,
                Parent = vm.Parent,
                Definition = vm.Definition,
                IsExternal = vm.IsExternal,
                Href = vm.Href,
                Name = ApiBuildOutputUtility.TransformToLanguagePairList(vm.Name, vm.NameInDevLangs, supportedLanguages),
                NameWithType = ApiBuildOutputUtility.TransformToLanguagePairList(vm.NameWithType, vm.NameWithTypeInDevLangs, supportedLanguages),
                FullName = ApiBuildOutputUtility.TransformToLanguagePairList(vm.FullName, vm.FullNameInDevLangs, supportedLanguages),
                Spec = GetSpecNames(ApiBuildOutputUtility.GetXref(vm.Uid, vm.Name, vm.FullName), supportedLanguages, vm.Specs),
                Metadata = vm.Additional,
            };
            if (result.Metadata.TryGetValue("syntax", out object syntax))
            {
                result.Syntax = ApiSyntaxBuildOutput.FromModel(syntax as SyntaxDetailViewModel, supportedLanguages);
                result.Metadata.Remove("syntax");
            }
            return result;
        }

        public static ApiReferenceBuildOutput FromModel(ItemViewModel vm)
        {
            if (vm == null)
            {
                return null;
            }
            var output = new ApiReferenceBuildOutput
            {
                Uid = vm.Uid,
                IsExplicitInterfaceImplementation = vm.IsExplicitInterfaceImplementation,
                IsExtensionMethod = vm.IsExtensionMethod,
                Parent = vm.Parent,
                IsExternal = false,
                Href = vm.Href,
                Name = ApiBuildOutputUtility.TransformToLanguagePairList(vm.Name, vm.Names, vm.SupportedLanguages),
                NameWithType = ApiBuildOutputUtility.TransformToLanguagePairList(vm.NameWithType, vm.NamesWithType, vm.SupportedLanguages),
                FullName = ApiBuildOutputUtility.TransformToLanguagePairList(vm.FullName, vm.FullNames, vm.SupportedLanguages),
                Spec = GetSpecNames(ApiBuildOutputUtility.GetXref(vm.Uid, vm.Name, vm.FullName), vm.SupportedLanguages),
                Source = vm.Source,
                Documentation = vm.Documentation,
                AssemblyNameList = vm.AssemblyNameList,
                NamespaceName = vm.NamespaceName,
                Remarks = vm.Remarks,
                AdditionalNotes = vm.AdditionalNotes,
                Examples = vm.Examples,
                Overridden = ApiNames.FromUid(vm.Overridden),
                Overload = ApiNames.FromUid(vm.Overload),
                SeeAlsos = vm.SeeAlsos?.Select(ApiLinkInfoBuildOutput.FromModel).ToList(),
                Sees = vm.Sees?.Select(ApiLinkInfoBuildOutput.FromModel).ToList(),
                Inheritance = vm.Inheritance?.Select(FromUid).ToList(),
                Implements = vm.Implements?.Select(ApiNames.FromUid).ToList(),
                InheritedMembers = vm.InheritedMembers,
                ExtensionMethods = vm.ExtensionMethods,
                Modifiers = vm.Modifiers,
                Conceptual = vm.Conceptual,
                Metadata = vm.Metadata,
                Attributes = vm.Attributes,
                Syntax = ApiSyntaxBuildOutput.FromModel(vm.Syntax, vm.SupportedLanguages),
                Exceptions = vm.Exceptions?.Select(ApiExceptionInfoBuildOutput.FromModel).ToList(),
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
                Overload = ApiBuildOutputUtility.GetApiNames(Overload?.Uid, references, supportedLanguages);
            }
        }

        public static List<ApiLanguageValuePair> GetSpecNames(string xref, string[] supportedLanguages, SortedList<string, List<SpecViewModel>> specs = null)
        {
            if (specs != null && specs.Count > 0)
            {
                return (from spec in specs
                        where supportedLanguages.Contains(spec.Key)
                        select new ApiLanguageValuePair
                        {
                            Language = spec.Key,
                            Value = GetSpecName(spec.Value),
                        }).ToList();
            }
            if (!string.IsNullOrEmpty(xref))
            {
                return (from lang in supportedLanguages
                        select new ApiLanguageValuePair
                        {
                            Language = lang,
                            Value = xref,
                        }).ToList();
            }
            return null;
        }

        private static string GetSpecName(List<SpecViewModel> spec)
        {
            if (spec == null)
            {
                return null;
            }
            return string.Concat(spec.Select(GetCompositeName));
        }

        private static string GetCompositeName(SpecViewModel svm)
        {
            // If href does not exists, return full name
            if (string.IsNullOrEmpty(svm.Uid))
            {
                return HttpUtility.HtmlEncode(svm.FullName);
            }

            // If href exists, return name with href
            return ApiBuildOutputUtility.GetXref(svm.Uid, svm.Name, svm.FullName);
        }
    }
}
