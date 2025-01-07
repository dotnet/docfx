// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiBuildOutput
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Parent)]
    [JsonProperty(Constants.PropertyName.Parent)]
    [JsonPropertyName(Constants.PropertyName.Parent)]
    public List<ApiLanguageValuePair<ApiNames>> Parent { get; set; }

    [YamlMember(Alias = "package")]
    [JsonProperty("package")]
    [JsonPropertyName("package")]
    public List<ApiLanguageValuePair<ApiNames>> Package { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Children)]
    [JsonProperty(Constants.PropertyName.Children)]
    [JsonPropertyName(Constants.PropertyName.Children)]
    public List<ApiLanguageValuePair<List<ApiBuildOutput>>> Children { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonProperty(Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }

    [YamlMember(Alias = "langs")]
    [JsonProperty("langs")]
    [JsonPropertyName("langs")]
    public string[] SupportedLanguages { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonProperty(Constants.PropertyName.Name)]
    [JsonPropertyName(Constants.PropertyName.Name)]
    public List<ApiLanguageValuePair<string>> Name { get; set; }

    [YamlMember(Alias = Constants.PropertyName.NameWithType)]
    [JsonProperty(Constants.PropertyName.NameWithType)]
    [JsonPropertyName(Constants.PropertyName.NameWithType)]
    public List<ApiLanguageValuePair<string>> NameWithType { get; set; }

    [YamlMember(Alias = Constants.PropertyName.FullName)]
    [JsonProperty(Constants.PropertyName.FullName)]
    [JsonPropertyName(Constants.PropertyName.FullName)]
    public List<ApiLanguageValuePair<string>> FullName { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonProperty(Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public string Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonProperty(Constants.PropertyName.Source)]
    [JsonPropertyName(Constants.PropertyName.Source)]
    public List<ApiLanguageValuePair<SourceDetail>> Source { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonProperty(Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Assemblies)]
    [JsonProperty(Constants.PropertyName.Assemblies)]
    [JsonPropertyName(Constants.PropertyName.Assemblies)]
    public List<ApiLanguageValuePair<List<string>>> AssemblyNameList { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Namespace)]
    [JsonProperty(Constants.PropertyName.Namespace)]
    [JsonPropertyName(Constants.PropertyName.Namespace)]
    public List<ApiLanguageValuePair<ApiNames>> NamespaceName { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = null;

    [YamlMember(Alias = "remarks")]
    [JsonProperty("remarks")]
    [JsonPropertyName("remarks")]
    public string Remarks { get; set; }

    [YamlMember(Alias = "example")]
    [JsonProperty("example")]
    [JsonPropertyName("example")]
    public List<string> Examples { get; set; }

    [YamlMember(Alias = "syntax")]
    [JsonProperty("syntax")]
    [JsonPropertyName("syntax")]
    public ApiSyntaxBuildOutput Syntax { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Overridden)]
    [JsonProperty(Constants.PropertyName.Overridden)]
    [JsonPropertyName(Constants.PropertyName.Overridden)]
    public List<ApiLanguageValuePair<ApiNames>> Overridden { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Overload)]
    [JsonProperty(Constants.PropertyName.Overload)]
    [JsonPropertyName(Constants.PropertyName.Overload)]
    public List<ApiLanguageValuePair<ApiNames>> Overload { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Exceptions)]
    [JsonProperty(Constants.PropertyName.Exceptions)]
    [JsonPropertyName(Constants.PropertyName.Exceptions)]
    public List<ApiLanguageValuePair<List<ApiExceptionInfoBuildOutput>>> Exceptions { get; set; }

    [YamlMember(Alias = "seealso")]
    [JsonProperty("seealso")]
    [JsonPropertyName("seealso")]
    public List<ApiLinkInfoBuildOutput> SeeAlsos { get; set; }

    [YamlMember(Alias = Constants.PropertyName.SeeAlsoContent)]
    [JsonProperty(Constants.PropertyName.SeeAlsoContent)]
    [JsonPropertyName(Constants.PropertyName.SeeAlsoContent)]
    public string SeeAlsoContent { get; set; }

    [YamlMember(Alias = "see")]
    [JsonProperty("see")]
    [JsonPropertyName("see")]
    public List<ApiLinkInfoBuildOutput> Sees { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Inheritance)]
    [JsonProperty(Constants.PropertyName.Inheritance)]
    [JsonPropertyName(Constants.PropertyName.Inheritance)]
    public List<ApiLanguageValuePairWithLevel<List<ApiInheritanceTreeBuildOutput>>> Inheritance { get; set; }

    [YamlMember(Alias = Constants.PropertyName.DerivedClasses)]
    [JsonProperty(Constants.PropertyName.DerivedClasses)]
    [JsonPropertyName(Constants.PropertyName.DerivedClasses)]
    public List<ApiLanguageValuePair<List<ApiNames>>> DerivedClasses { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Implements)]
    [JsonProperty(Constants.PropertyName.Implements)]
    [JsonPropertyName(Constants.PropertyName.Implements)]
    public List<ApiLanguageValuePair<List<ApiNames>>> Implements { get; set; }

    [YamlMember(Alias = Constants.PropertyName.InheritedMembers)]
    [JsonProperty(Constants.PropertyName.InheritedMembers)]
    [JsonPropertyName(Constants.PropertyName.InheritedMembers)]
    public List<ApiLanguageValuePair<List<ApiNames>>> InheritedMembers { get; set; }

    [YamlMember(Alias = Constants.PropertyName.ExtensionMethods)]
    [JsonProperty(Constants.PropertyName.ExtensionMethods)]
    [JsonPropertyName(Constants.PropertyName.ExtensionMethods)]
    public List<ApiLanguageValuePair<List<ApiNames>>> ExtensionMethods { get; set; }

    [YamlMember(Alias = "conceptual")]
    [JsonProperty("conceptual")]
    [JsonPropertyName("conceptual")]
    public string Conceptual { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Platform)]
    [JsonProperty(Constants.PropertyName.Platform)]
    [JsonPropertyName(Constants.PropertyName.Platform)]
    public List<ApiLanguageValuePair<List<string>>> Platform { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
