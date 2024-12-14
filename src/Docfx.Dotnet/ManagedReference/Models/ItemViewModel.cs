// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class ItemViewModel : IOverwriteDocumentViewModel, IItemWithMetadata
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    [MergeOption(MergeOption.MergeKey)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Id)]
    [JsonProperty(Constants.PropertyName.Id)]
    [JsonPropertyName(Constants.PropertyName.Id)]
    public string Id { get; set; }

    [YamlMember(Alias = "isEii")]
    [JsonProperty("isEii")]
    [JsonPropertyName("isEii")]
    public bool IsExplicitInterfaceImplementation { get; set; }

    [YamlMember(Alias = "isExtensionMethod")]
    [JsonProperty("isExtensionMethod")]
    [JsonPropertyName("isExtensionMethod")]
    public bool IsExtensionMethod { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Parent)]
    [JsonProperty(Constants.PropertyName.Parent)]
    [JsonPropertyName(Constants.PropertyName.Parent)]
    [UniqueIdentityReference]
    public string Parent { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Children)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.Children)]
    [JsonPropertyName(Constants.PropertyName.Children)]
    [UniqueIdentityReference]
    public List<string> Children { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonProperty(Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }

    [YamlMember(Alias = "langs")]
    [JsonProperty("langs")]
    [JsonPropertyName("langs")]
    public string[] SupportedLanguages { get; set; } = ["csharp", "vb"];

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonProperty(Constants.PropertyName.Name)]
    [JsonPropertyName(Constants.PropertyName.Name)]
    public string Name { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> Names { get; set; } = [];

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
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
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
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
    [JsonProperty(Constants.PropertyName.NameWithType)]
    [JsonPropertyName(Constants.PropertyName.NameWithType)]
    public string NameWithType { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> NamesWithType { get; set; } = [];

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
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
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
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
    [JsonProperty(Constants.PropertyName.FullName)]
    [JsonPropertyName(Constants.PropertyName.FullName)]
    public string FullName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> FullNames { get; set; } = [];

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
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
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
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
    [JsonProperty(Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public MemberType? Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonProperty(Constants.PropertyName.Source)]
    [JsonPropertyName(Constants.PropertyName.Source)]
    public SourceDetail Source { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonProperty(Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Assemblies)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.Assemblies)]
    [JsonPropertyName(Constants.PropertyName.Assemblies)]
    public List<string> AssemblyNameList { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Namespace)]
    [JsonProperty(Constants.PropertyName.Namespace)]
    [JsonPropertyName(Constants.PropertyName.Namespace)]
    [UniqueIdentityReference]
    public string NamespaceName { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    [MarkdownContent]
    public string Summary { get; set; }

    [YamlMember(Alias = Constants.PropertyName.AdditionalNotes)]
    [JsonProperty(Constants.PropertyName.AdditionalNotes)]
    [JsonPropertyName(Constants.PropertyName.AdditionalNotes)]
    public AdditionalNotes AdditionalNotes { get; set; }

    [YamlMember(Alias = "remarks")]
    [JsonProperty("remarks")]
    [JsonPropertyName("remarks")]
    [MarkdownContent]
    public string Remarks { get; set; }

    [YamlMember(Alias = "example")]
    [JsonProperty("example")]
    [JsonPropertyName("example")]
    [MergeOption(MergeOption.Replace)]
    [MarkdownContent]
    public List<string> Examples { get; set; }

    [YamlMember(Alias = "syntax")]
    [JsonProperty("syntax")]
    [JsonPropertyName("syntax")]
    public SyntaxDetailViewModel Syntax { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Overridden)]
    [JsonProperty(Constants.PropertyName.Overridden)]
    [JsonPropertyName(Constants.PropertyName.Overridden)]
    [UniqueIdentityReference]
    public string Overridden { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Overload)]
    [JsonProperty(Constants.PropertyName.Overload)]
    [JsonPropertyName(Constants.PropertyName.Overload)]
    [UniqueIdentityReference]
    public string Overload { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Exceptions)]
    [JsonProperty(Constants.PropertyName.Exceptions)]
    [JsonPropertyName(Constants.PropertyName.Exceptions)]
    public List<ExceptionInfo> Exceptions { get; set; }

    [YamlMember(Alias = "seealso")]
    [JsonProperty("seealso")]
    [JsonPropertyName("seealso")]
    public List<LinkInfo> SeeAlsos { get; set; }

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [UniqueIdentityReference]
    public List<string> SeeAlsosUidReference => SeeAlsos?.Where(s => s.LinkType == LinkType.CRef).Select(s => s.LinkId).ToList();

    [YamlMember(Alias = Constants.PropertyName.Inheritance)]
    [MergeOption(MergeOption.Ignore)]
    [JsonProperty(Constants.PropertyName.Inheritance)]
    [JsonPropertyName(Constants.PropertyName.Inheritance)]
    [UniqueIdentityReference]
    public List<string> Inheritance { get; set; }

    [YamlMember(Alias = Constants.PropertyName.DerivedClasses)]
    [MergeOption(MergeOption.Ignore)]
    [JsonProperty(Constants.PropertyName.DerivedClasses)]
    [JsonPropertyName(Constants.PropertyName.DerivedClasses)]
    [UniqueIdentityReference]
    public List<string> DerivedClasses { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Implements)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.Implements)]
    [JsonPropertyName(Constants.PropertyName.Implements)]
    [UniqueIdentityReference]
    public List<string> Implements { get; set; }

    [YamlMember(Alias = Constants.PropertyName.InheritedMembers)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.InheritedMembers)]
    [JsonPropertyName(Constants.PropertyName.InheritedMembers)]
    [UniqueIdentityReference]
    public List<string> InheritedMembers { get; set; }

    [YamlMember(Alias = Constants.PropertyName.ExtensionMethods)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.ExtensionMethods)]
    [JsonPropertyName(Constants.PropertyName.ExtensionMethods)]
    [UniqueIdentityReference]
    public List<string> ExtensionMethods { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Conceptual)]
    [JsonProperty(Constants.PropertyName.Conceptual)]
    [JsonPropertyName(Constants.PropertyName.Conceptual)]
    [MarkdownContent]
    public string Conceptual { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Platform)]
    [JsonProperty(Constants.PropertyName.Platform)]
    [JsonPropertyName(Constants.PropertyName.Platform)]
    [MergeOption(MergeOption.Replace)]
    public List<string> Platform { get; set; }

    [YamlMember(Alias = "attributes")]
    [JsonProperty("attributes")]
    [JsonPropertyName("attributes")]
    [MergeOption(MergeOption.Ignore)]
    public List<AttributeInfo> Attributes { get; set; }

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
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public IDictionary<string, object> ExtensionData
    {
        get
        {
            return CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Name, Names, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.NameWithType, NamesWithType, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.FullName, FullNames, JTokenConverter.Convert<string>)
            .Add(string.Empty, Metadata)
            .Create();
        }
        private init
        {
            // init or getter is required for deserialize data with System.Text.Json.
        }
    }
}
