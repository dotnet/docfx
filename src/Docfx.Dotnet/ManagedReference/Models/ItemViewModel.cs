// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class ItemViewModel : IOverwriteDocumentViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    [MergeOption(MergeOption.MergeKey)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Id)]
    [JsonPropertyName(Constants.PropertyName.Id)]
    public string Id { get; set; }

    [YamlMember(Alias = "isEii")]
    [JsonPropertyName("isEii")]
    public bool IsExplicitInterfaceImplementation { get; set; }

    [YamlMember(Alias = "isExtensionMethod")]
    [JsonPropertyName("isExtensionMethod")]
    public bool IsExtensionMethod { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Parent)]
    [JsonPropertyName(Constants.PropertyName.Parent)]
    [UniqueIdentityReference]
    public string Parent { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Children)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.Children)]
    [UniqueIdentityReference]
    public List<string> Children { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }

    [YamlMember(Alias = "langs")]
    [JsonPropertyName("langs")]
    public string[] SupportedLanguages { get; set; } = new string[] { "csharp", "vb" };

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonPropertyName(Constants.PropertyName.Name)]
    public string Name { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
    [JsonIgnore]
    public SortedList<string, string> Names { get; set; } = new SortedList<string, string>();

    [YamlIgnore]
    [JsonIgnore]
    public string NameForCSharp
    {
        get
        {
            Names.TryGetValue("csharp", out string result);
            return result;
        }
        set
        {
            if (value == null)
            {
                Names.Remove("csharp");
            }
            else
            {
                Names["csharp"] = value;
            }
        }
    }

    [YamlIgnore]
    [JsonIgnore]
    public string NameForVB
    {
        get
        {
            Names.TryGetValue("vb", out string result);
            return result;
        }
        set
        {
            if (value == null)
            {
                Names.Remove("vb");
            }
            else
            {
                Names["vb"] = value;
            }
        }
    }

    [YamlMember(Alias = Constants.PropertyName.NameWithType)]
    [JsonPropertyName(Constants.PropertyName.NameWithType)]
    public string NameWithType { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
    [JsonIgnore]
    public SortedList<string, string> NamesWithType { get; set; } = new SortedList<string, string>();

    [YamlIgnore]
    [JsonIgnore]
    public string NameWithTypeForCSharp
    {
        get
        {
            Names.TryGetValue("csharp", out string result);
            return result;
        }
        set
        {
            if (value == null)
            {
                NamesWithType.Remove("csharp");
            }
            else
            {
                NamesWithType["csharp"] = value;
            }
        }
    }

    [YamlIgnore]
    [JsonIgnore]
    public string NameWithTypeForVB
    {
        get
        {
            Names.TryGetValue("vb", out string result);
            return result;
        }
        set
        {
            if (value == null)
            {
                NamesWithType.Remove("vb");
            }
            else
            {
                NamesWithType["vb"] = value;
            }
        }
    }

    [YamlMember(Alias = Constants.PropertyName.FullName)]
    [JsonPropertyName(Constants.PropertyName.FullName)]
    public string FullName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
    [JsonIgnore]
    public SortedList<string, string> FullNames { get; set; } = new SortedList<string, string>();

    [YamlIgnore]
    [JsonIgnore]
    public string FullNameForCSharp
    {
        get
        {
            FullNames.TryGetValue("csharp", out string result);
            return result;
        }
        set
        {
            if (value == null)
            {
                FullNames.Remove("csharp");
            }
            else
            {
                FullNames["csharp"] = value;
            }
        }
    }

    [YamlIgnore]
    [JsonIgnore]
    public string FullNameForVB
    {
        get
        {
            FullNames.TryGetValue("vb", out string result);
            return result;
        }
        set
        {
            if (value == null)
            {
                FullNames.Remove("vb");
            }
            else
            {
                FullNames["vb"] = value;
            }
        }
    }

    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public MemberType? Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonPropertyName(Constants.PropertyName.Source)]
    public SourceDetail Source { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Assemblies)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.Assemblies)]
    public List<string> AssemblyNameList { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Namespace)]
    [JsonPropertyName(Constants.PropertyName.Namespace)]
    [UniqueIdentityReference]
    public string NamespaceName { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonPropertyName("summary")]
    [MarkdownContent]
    public string Summary { get; set; }

    [YamlMember(Alias = Constants.PropertyName.AdditionalNotes)]
    [JsonPropertyName(Constants.PropertyName.AdditionalNotes)]
    public AdditionalNotes AdditionalNotes { get; set; }

    [YamlMember(Alias = "remarks")]
    [JsonPropertyName("remarks")]
    [MarkdownContent]
    public string Remarks { get; set; }

    [YamlMember(Alias = "example")]
    [JsonPropertyName("example")]
    [MergeOption(MergeOption.Replace)]
    [MarkdownContent]
    public List<string> Examples { get; set; }

    [YamlMember(Alias = "syntax")]
    [JsonPropertyName("syntax")]
    public SyntaxDetailViewModel Syntax { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Overridden)]
    [JsonPropertyName(Constants.PropertyName.Overridden)]
    [UniqueIdentityReference]
    public string Overridden { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Overload)]
    [JsonPropertyName(Constants.PropertyName.Overload)]
    [UniqueIdentityReference]
    public string Overload { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Exceptions)]
    [JsonPropertyName(Constants.PropertyName.Exceptions)]
    public List<ExceptionInfo> Exceptions { get; set; }

    [YamlMember(Alias = "seealso")]
    [JsonPropertyName("seealso")]
    public List<LinkInfo> SeeAlsos { get; set; }

    [YamlIgnore]
    [JsonIgnore]
    [UniqueIdentityReference]
    public List<string> SeeAlsosUidReference => SeeAlsos?.Where(s => s.LinkType == LinkType.CRef)?.Select(s => s.LinkId).ToList();

    [YamlMember(Alias = Constants.PropertyName.Inheritance)]
    [MergeOption(MergeOption.Ignore)]
    [JsonPropertyName(Constants.PropertyName.Inheritance)]
    [UniqueIdentityReference]
    public List<string> Inheritance { get; set; }

    [YamlMember(Alias = Constants.PropertyName.DerivedClasses)]
    [MergeOption(MergeOption.Ignore)]
    [JsonPropertyName(Constants.PropertyName.DerivedClasses)]
    [UniqueIdentityReference]
    public List<string> DerivedClasses { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Implements)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.Implements)]
    [UniqueIdentityReference]
    public List<string> Implements { get; set; }

    [YamlMember(Alias = Constants.PropertyName.InheritedMembers)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.InheritedMembers)]
    [UniqueIdentityReference]
    public List<string> InheritedMembers { get; set; }

    [YamlMember(Alias = Constants.PropertyName.ExtensionMethods)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.ExtensionMethods)]
    [UniqueIdentityReference]
    public List<string> ExtensionMethods { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Conceptual)]
    [JsonPropertyName(Constants.PropertyName.Conceptual)]
    [MarkdownContent]
    public string Conceptual { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Platform)]
    [JsonPropertyName(Constants.PropertyName.Platform)]
    [MergeOption(MergeOption.Replace)]
    public List<string> Platform { get; set; }

    [YamlMember(Alias = "attributes")]
    [JsonPropertyName("attributes")]
    [MergeOption(MergeOption.Ignore)]
    public List<AttributeInfo> Attributes { get; set; }

    [ExtensibleMember]
    [JsonIgnore]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [JsonExtensionData]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public IDictionary<string, object> ExtensionData =>
        CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Name, Names, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.NameWithType, NamesWithType, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.FullName, FullNames, JTokenConverter.Convert<string>)
            .Add(string.Empty, Metadata)
            .Create();
}
