// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Microsoft.DocAsCode.Common.EntityMergers;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Dotnet;

internal class MetadataItem : ICloneable
{
    [YamlIgnore]
    [JsonIgnore]
    public bool IsInvalid { get; set; }

    [JsonProperty(Constants.PropertyName.IsEii)]
    [YamlMember(Alias = Constants.PropertyName.IsEii)]
    public bool IsExplicitInterfaceImplementation { get; set; }

    [YamlMember(Alias = "isExtensionMethod")]
    [JsonProperty("isExtensionMethod")]
    public bool IsExtensionMethod { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Id)]
    [JsonProperty(Constants.PropertyName.Id)]
    public string Name { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    public SortedList<SyntaxLanguage, string> DisplayNames { get; set; }

    [YamlMember(Alias = "nameWithType")]
    [JsonProperty("nameWithType")]
    public SortedList<SyntaxLanguage, string> DisplayNamesWithType { get; set; }

    [YamlMember(Alias = "qualifiedName")]
    [JsonProperty("qualifiedName")]
    public SortedList<SyntaxLanguage, string> DisplayQualifiedNames { get; set; }

    [YamlMember(Alias = "parent")]
    [JsonProperty("parent")]
    public MetadataItem Parent { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonProperty(Constants.PropertyName.Type)]
    public MemberType Type { get; set; }

    [YamlMember(Alias = "assemblies")]
    [JsonProperty("assemblies")]
    public List<string> AssemblyNameList { get; set; }

    [YamlMember(Alias = "namespace")]
    [JsonProperty("namespace")]
    public string NamespaceName { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonProperty(Constants.PropertyName.Source)]
    public SourceDetail Source { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonProperty(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

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
    public SyntaxDetail Syntax { get; set; }

    [YamlMember(Alias = "overload")]
    [JsonProperty("overload")]
    public string Overload { get; set; }

    [YamlMember(Alias = "overridden")]
    [JsonProperty("overridden")]
    public string Overridden { get; set; }

    [YamlMember(Alias = "exceptions")]
    [JsonProperty("exceptions")]
    public List<ExceptionInfo> Exceptions { get; set; }

    [YamlMember(Alias = "seealso")]
    [JsonProperty("seealso")]
    public List<LinkInfo> SeeAlsos { get; set; }

    [YamlMember(Alias = "inheritance")]
    [JsonProperty("inheritance")]
    public List<string> Inheritance { get; set; }

    [YamlMember(Alias = "derivedClasses")]
    [JsonProperty("derivedClasses")]
    public List<string> DerivedClasses { get; set; }

    [YamlMember(Alias = "implements")]
    [JsonProperty("implements")]
    public List<string> Implements { get; set; }

    [YamlMember(Alias = "inheritedMembers")]
    [JsonProperty("inheritedMembers")]
    public List<string> InheritedMembers { get; set; }

    [YamlMember(Alias = "extensionMethods")]
    [JsonProperty("extensionMethods")]
    public List<string> ExtensionMethods { get; set; }

    [YamlMember(Alias = "attributes")]
    [JsonProperty("attributes")]
    [MergeOption(MergeOption.Ignore)]
    public List<AttributeInfo> Attributes { get; set; }

    [YamlMember(Alias = "items")]
    [JsonProperty("items")]
    public List<MetadataItem> Items { get; set; }

    [YamlMember(Alias = "references")]
    [JsonProperty("references")]
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
