// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

internal class MetadataItem : ICloneable
{
    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsInvalid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.IsEii)]
    [JsonProperty(Constants.PropertyName.IsEii)]
    [JsonPropertyName(Constants.PropertyName.IsEii)]
    public bool IsExplicitInterfaceImplementation { get; set; }

    [YamlMember(Alias = "isExtensionMethod")]
    [JsonProperty("isExtensionMethod")]
    [JsonPropertyName("isExtensionMethod")]
    public bool IsExtensionMethod { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Id)]
    [JsonProperty(Constants.PropertyName.Id)]
    [JsonPropertyName(Constants.PropertyName.Id)]
    public string Name { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public SortedList<SyntaxLanguage, string> DisplayNames { get; set; }

    [YamlMember(Alias = "nameWithType")]
    [JsonProperty("nameWithType")]
    [JsonPropertyName("nameWithType")]
    public SortedList<SyntaxLanguage, string> DisplayNamesWithType { get; set; }

    [YamlMember(Alias = "qualifiedName")]
    [JsonProperty("qualifiedName")]
    [JsonPropertyName("qualifiedName")]
    public SortedList<SyntaxLanguage, string> DisplayQualifiedNames { get; set; }

    [YamlMember(Alias = "parent")]
    [JsonProperty("parent")]
    [JsonPropertyName("parent")]
    public MetadataItem Parent { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonProperty(Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public MemberType Type { get; set; }

    [YamlMember(Alias = "assemblies")]
    [JsonProperty("assemblies")]
    [JsonPropertyName("assemblies")]
    public List<string> AssemblyNameList { get; set; }

    [YamlMember(Alias = "namespace")]
    [JsonProperty("namespace")]
    [JsonPropertyName("namespace")]
    public string NamespaceName { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonProperty(Constants.PropertyName.Source)]
    [JsonPropertyName(Constants.PropertyName.Source)]
    public SourceDetail Source { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonProperty(Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

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
    public SyntaxDetail Syntax { get; set; }

    [YamlMember(Alias = "overload")]
    [JsonProperty("overload")]
    [JsonPropertyName("overload")]
    public string Overload { get; set; }

    [YamlMember(Alias = "overridden")]
    [JsonProperty("overridden")]
    [JsonPropertyName("overridden")]
    public string Overridden { get; set; }

    [YamlMember(Alias = "exceptions")]
    [JsonProperty("exceptions")]
    [JsonPropertyName("exceptions")]
    public List<ExceptionInfo> Exceptions { get; set; }

    [YamlMember(Alias = "seealso")]
    [JsonProperty("seealso")]
    [JsonPropertyName("seealso")]
    public List<LinkInfo> SeeAlsos { get; set; }

    [YamlMember(Alias = "inheritance")]
    [JsonProperty("inheritance")]
    [JsonPropertyName("inheritance")]
    public List<string> Inheritance { get; set; }

    [YamlMember(Alias = "derivedClasses")]
    [JsonProperty("derivedClasses")]
    [JsonPropertyName("derivedClasses")]
    public List<string> DerivedClasses { get; set; }

    [YamlMember(Alias = "implements")]
    [JsonProperty("implements")]
    [JsonPropertyName("implements")]
    public List<string> Implements { get; set; }

    [YamlMember(Alias = "inheritedMembers")]
    [JsonProperty("inheritedMembers")]
    [JsonPropertyName("inheritedMembers")]
    public List<string> InheritedMembers { get; set; }

    [YamlMember(Alias = "extensionMethods")]
    [JsonProperty("extensionMethods")]
    [JsonPropertyName("extensionMethods")]
    public List<string> ExtensionMethods { get; set; }

    [YamlMember(Alias = "attributes")]
    [JsonProperty("attributes")]
    [JsonPropertyName("attributes")]
    [MergeOption(MergeOption.Ignore)]
    public List<AttributeInfo> Attributes { get; set; }

    [YamlMember(Alias = "items")]
    [JsonProperty("items")]
    [JsonPropertyName("items")]
    public List<MetadataItem> Items { get; set; }

    [YamlMember(Alias = "references")]
    [JsonProperty("references")]
    [JsonPropertyName("references")]
    public Dictionary<string, ReferenceItem> References { get; set; }

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public XmlComment CommentModel { get; set; }

    public override string ToString()
    {
        return Type + ": " + Name;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}
