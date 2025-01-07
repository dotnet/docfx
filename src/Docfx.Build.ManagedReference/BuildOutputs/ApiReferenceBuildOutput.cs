// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Web;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.ManagedReference.BuildOutputs;

public class ApiReferenceBuildOutput
{
    [YamlMember(Alias = "uid")]
    [JsonProperty("uid")]
    [JsonPropertyName("uid")]
    public string Uid { get; set; }

    [YamlMember(Alias = "isEii")]
    [JsonProperty("isEii")]
    [JsonPropertyName("isEii")]
    public bool IsExplicitInterfaceImplementation { get; set; }

    [YamlMember(Alias = "isExtensionMethod")]
    [JsonProperty("isExtensionMethod")]
    [JsonPropertyName("isExtensionMethod")]
    public bool IsExtensionMethod { get; set; }

    [YamlMember(Alias = "parent")]
    [JsonProperty("parent")]
    [JsonPropertyName("parent")]
    public string Parent { get; set; }

    [YamlMember(Alias = "definition")]
    [JsonProperty("definition")]
    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [YamlMember(Alias = "isExternal")]
    [JsonProperty("isExternal")]
    [JsonPropertyName("isExternal")]
    public bool? IsExternal { get; set; }

    [YamlMember(Alias = "href")]
    [JsonProperty("href")]
    [JsonPropertyName("href")]
    public string Href { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public List<ApiLanguageValuePair> Name { get; set; }

    [YamlMember(Alias = "nameWithType")]
    [JsonProperty("nameWithType")]
    [JsonPropertyName("nameWithType")]
    public List<ApiLanguageValuePair> NameWithType { get; set; }

    [YamlMember(Alias = "fullName")]
    [JsonProperty("fullName")]
    [JsonPropertyName("fullName")]
    public List<ApiLanguageValuePair> FullName { get; set; }

    [YamlMember(Alias = "specName")]
    [JsonProperty("specName")]
    [JsonPropertyName("specName")]
    public List<ApiLanguageValuePair> Spec { get; set; }

    [YamlMember(Alias = "syntax")]
    [JsonProperty("syntax")]
    [JsonPropertyName("syntax")]
    public ApiSyntaxBuildOutput Syntax { get; set; }

    [YamlMember(Alias = "source")]
    [JsonProperty("source")]
    [JsonPropertyName("source")]
    public SourceDetail Source { get; set; }

    [YamlMember(Alias = "documentation")]
    [JsonProperty("documentation")]
    [JsonPropertyName("documentation")]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = "assemblies")]
    [JsonProperty("assemblies")]
    [JsonPropertyName("assemblies")]
    public List<string> AssemblyNameList { get; set; }

    [YamlMember(Alias = "namespace")]
    [JsonProperty("namespace")]
    [JsonPropertyName("namespace")]
    public string NamespaceName { get; set; }

    [YamlMember(Alias = "remarks")]
    [JsonProperty("remarks")]
    [JsonPropertyName("remarks")]
    public string Remarks { get; set; }

    [YamlMember(Alias = Constants.PropertyName.AdditionalNotes)]
    [JsonProperty(Constants.PropertyName.AdditionalNotes)]
    [JsonPropertyName(Constants.PropertyName.AdditionalNotes)]
    public AdditionalNotes AdditionalNotes { get; set; }

    [YamlMember(Alias = "example")]
    [JsonProperty("example")]
    [JsonPropertyName("example")]
    public List<string> Examples { get; set; }

    [YamlMember(Alias = "overridden")]
    [JsonProperty("overridden")]
    [JsonPropertyName("overridden")]
    public ApiNames Overridden { get; set; }

    [YamlMember(Alias = "overload")]
    [JsonProperty("overload")]
    [JsonPropertyName("overload")]
    public ApiNames Overload { get; set; }

    [YamlMember(Alias = "exceptions")]
    [JsonProperty("exceptions")]
    [JsonPropertyName("exceptions")]
    public List<ApiExceptionInfoBuildOutput> Exceptions { get; set; }

    [YamlMember(Alias = "seealso")]
    [JsonProperty("seealso")]
    [JsonPropertyName("seealso")]
    public List<ApiLinkInfoBuildOutput> SeeAlsos { get; set; }

    [YamlMember(Alias = "inheritance")]
    [JsonProperty("inheritance")]
    [JsonPropertyName("inheritance")]
    public List<ApiReferenceBuildOutput> Inheritance { get; set; }

    [YamlMember(Alias = "level")]
    [JsonProperty("level")]
    [JsonPropertyName("level")]
    public int Level => Inheritance != null ? Inheritance.Count : 0;

    [YamlMember(Alias = "implements")]
    [JsonProperty("implements")]
    [JsonPropertyName("implements")]
    public List<ApiNames> Implements { get; set; }

    [YamlMember(Alias = "inheritedMembers")]
    [JsonProperty("inheritedMembers")]
    [JsonPropertyName("inheritedMembers")]
    public List<string> InheritedMembers { get; set; }

    [YamlMember(Alias = "extensionMethods")]
    [JsonProperty("extensionMethods")]
    [JsonPropertyName("extensionMethods")]
    public List<string> ExtensionMethods { get; set; }

    [YamlMember(Alias = "conceptual")]
    [JsonProperty("conceptual")]
    [JsonPropertyName("conceptual")]
    public string Conceptual { get; set; }

    [YamlMember(Alias = "attributes")]
    [JsonProperty("attributes")]
    [JsonPropertyName("attributes")]
    public List<AttributeInfo> Attributes { get; set; }

    [YamlMember(Alias = "index")]
    [JsonProperty("index")]
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Text.Json.Serialization.JsonPropertyName("__metadata__")]
    public Dictionary<string, object> Metadata { get; set; } = [];

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    [System.Text.Json.Serialization.JsonInclude]
    public CompositeDictionary MetadataJson
    {
        get
        {
            return CompositeDictionary
            .CreateBuilder()
            .Add(string.Empty, Metadata)
            .Create();
        }
        private init
        {
            // init or getter is required for deserialize data with System.Text.Json.
        }
    }

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
            Spec = GetSpecNames(ApiBuildOutputUtility.GetXref(vm.Uid, vm.Name), supportedLanguages, vm.Specs),
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
            Spec = GetSpecNames(ApiBuildOutputUtility.GetXref(vm.Uid, vm.Name), vm.SupportedLanguages),
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
            Inheritance = vm.Inheritance?.Select(FromUid).ToList(),
            Implements = vm.Implements?.Select(ApiNames.FromUid).ToList(),
            InheritedMembers = vm.InheritedMembers,
            ExtensionMethods = vm.ExtensionMethods,
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
            Exceptions?.ForEach(e => e.Expand(references, supportedLanguages));
            Overload = ApiBuildOutputUtility.GetApiNames(Overload?.Uid, references, supportedLanguages);
        }
    }

    public static List<ApiLanguageValuePair> GetSpecNames(string xref, string[] supportedLanguages, SortedList<string, List<SpecViewModel>> specs = null)
    {
        if (specs is { Count: > 0 })
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
        if (!string.IsNullOrEmpty(svm.Href))
        {
            return $"<a class=\"xref\" href=\"{HttpUtility.HtmlAttributeEncode(svm.Href)}\">{HttpUtility.HtmlEncode(svm.Name)}</a>";
        }

        // If href does not exists, return full name
        if (string.IsNullOrEmpty(svm.Uid))
        {
            return HttpUtility.HtmlEncode(svm.Name);
        }

        // If href exists, return name with href
        return ApiBuildOutputUtility.GetXref(svm.Uid, svm.Name);
    }
}
