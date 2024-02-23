// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

internal class MetadataItem : ICloneable
{
    [YamlIgnore]
    [JsonIgnore]
    public bool IsInvalid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.IsEii)]
    [JsonPropertyName(Constants.PropertyName.IsEii)]
    public bool IsExplicitInterfaceImplementation { get; set; }

    [YamlMember(Alias = "isExtensionMethod")]
    [JsonPropertyName("isExtensionMethod")]
    public bool IsExtensionMethod { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Id)]
    [JsonPropertyName(Constants.PropertyName.Id)]
    public string Name { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public SortedList<SyntaxLanguage, string> DisplayNames { get; set; }

    [YamlMember(Alias = "nameWithType")]
    [JsonPropertyName("nameWithType")]
    public SortedList<SyntaxLanguage, string> DisplayNamesWithType { get; set; }

    [YamlMember(Alias = "qualifiedName")]
    [JsonPropertyName("qualifiedName")]
    public SortedList<SyntaxLanguage, string> DisplayQualifiedNames { get; set; }

    [YamlMember(Alias = "parent")]
    [JsonPropertyName("parent")]
    public MetadataItem Parent { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public MemberType Type { get; set; }

    [YamlMember(Alias = "assemblies")]
    [JsonPropertyName("assemblies")]
    public List<string> AssemblyNameList { get; set; }

    [YamlMember(Alias = "namespace")]
    [JsonPropertyName("namespace")]
    public string NamespaceName { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonPropertyName(Constants.PropertyName.Source)]
    public SourceDetail Source { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = "remarks")]
    [JsonPropertyName("remarks")]
    public string Remarks { get; set; }

    [YamlMember(Alias = "example")]
    [JsonPropertyName("example")]
    public List<string> Examples { get; set; }

    [YamlMember(Alias = "syntax")]
    [JsonPropertyName("syntax")]
    public SyntaxDetail Syntax { get; set; }

    [YamlMember(Alias = "overload")]
    [JsonPropertyName("overload")]
    public string Overload { get; set; }

    [YamlMember(Alias = "overridden")]
    [JsonPropertyName("overridden")]
    public string Overridden { get; set; }

    [YamlMember(Alias = "exceptions")]
    [JsonPropertyName("exceptions")]
    public List<ExceptionInfo> Exceptions { get; set; }

    [YamlMember(Alias = "seealso")]
    [JsonPropertyName("seealso")]
    public List<LinkInfo> SeeAlsos { get; set; }

    [YamlMember(Alias = "inheritance")]
    [JsonPropertyName("inheritance")]
    public List<string> Inheritance { get; set; }

    [YamlMember(Alias = "derivedClasses")]
    [JsonPropertyName("derivedClasses")]
    public List<string> DerivedClasses { get; set; }

    [YamlMember(Alias = "implements")]
    [JsonPropertyName("implements")]
    public List<string> Implements { get; set; }

    [YamlMember(Alias = "inheritedMembers")]
    [JsonPropertyName("inheritedMembers")]
    public List<string> InheritedMembers { get; set; }

    [YamlMember(Alias = "extensionMethods")]
    [JsonPropertyName("extensionMethods")]
    public List<string> ExtensionMethods { get; set; }

    [YamlMember(Alias = "attributes")]
    [JsonPropertyName("attributes")]
    [MergeOption(MergeOption.Ignore)]
    public List<AttributeInfo> Attributes { get; set; }

    [YamlMember(Alias = "items")]
    [JsonPropertyName("items")]
    public List<MetadataItem> Items { get; set; }

    [YamlMember(Alias = "references")]
    [JsonPropertyName("references")]
    public Dictionary<string, ReferenceItem> References { get; set; }

    [YamlIgnore]
    [JsonIgnore]
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
