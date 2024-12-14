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

namespace Docfx.DataContracts.UniversalReference;

public class ItemViewModel : IOverwriteDocumentViewModel
{
    /// <summary>
    /// item's unique identifier
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    [MergeOption(MergeOption.MergeKey)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    /// <summary>
    /// item's identifier
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Id)]
    [JsonProperty(Constants.PropertyName.Id)]
    [JsonPropertyName(Constants.PropertyName.Id)]
    public string Id { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Parent)]
    [JsonProperty(Constants.PropertyName.Parent)]
    [JsonPropertyName(Constants.PropertyName.Parent)]
    [UniqueIdentityReference]
    public string Parent { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Parent)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> ParentInDevLangs { get; set; } = [];

    [YamlMember(Alias = "package")]
    [JsonProperty("package")]
    [JsonPropertyName("package")]
    [UniqueIdentityReference]
    public string Package { get; set; }

    [ExtensibleMember("package" + Constants.PrefixSeparator)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> PackageInDevLangs { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.Children)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.Children)]
    [JsonPropertyName(Constants.PropertyName.Children)]
    [UniqueIdentityReference]
    public List<string> Children { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Children)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<string>> ChildrenInDevLangs { get; set; } = [];

    /// <summary>
    /// item's link URL
    /// As an item(uid) can be resolved to only one link in cross reference, HrefInDevLangs is not supported
    /// </summary>
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
    public string Name { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> Names { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.NameWithType)]
    [JsonProperty(Constants.PropertyName.NameWithType)]
    [JsonPropertyName(Constants.PropertyName.NameWithType)]
    public string NameWithType { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> NamesWithType { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.FullName)]
    [JsonProperty(Constants.PropertyName.FullName)]
    [JsonPropertyName(Constants.PropertyName.FullName)]
    public string FullName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> FullNames { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonProperty(Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public string Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonProperty(Constants.PropertyName.Source)]
    [JsonPropertyName(Constants.PropertyName.Source)]
    public SourceDetail Source { get; set; }

    /// <summary>
    /// item's source code's source detail in different dev langs
    /// </summary>
    [ExtensibleMember(Constants.ExtensionMemberPrefix.Source)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, SourceDetail> SourceInDevLangs { get; set; } = [];

    /// <summary>
    /// item's documentation's source detail
    /// as overwrite document targets uid, DocumentationInDevLangs is not supported
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonProperty(Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = Constants.ExtensionMemberPrefix.Assemblies)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.ExtensionMemberPrefix.Assemblies)]
    [JsonPropertyName(Constants.ExtensionMemberPrefix.Assemblies)]
    public List<string> AssemblyNameList { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Assemblies)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<string>> AssemblyNameListInDevLangs { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.Namespace)]
    [JsonProperty(Constants.PropertyName.Namespace)]
    [JsonPropertyName(Constants.PropertyName.Namespace)]
    [UniqueIdentityReference]
    public string NamespaceName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Namespace)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> NamespaceNameInDevLangs { get; set; } = [];

    /// <summary>
    /// item's summary
    /// content in different dev langs can be put in this property all together
    /// </summary>
    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    [MarkdownContent]
    public string Summary { get; set; }

    /// <summary>
    /// item's remarks
    /// content in different dev langs can be put in this property all together
    /// </summary>
    [YamlMember(Alias = "remarks")]
    [JsonProperty("remarks")]
    [JsonPropertyName("remarks")]
    [MarkdownContent]
    public string Remarks { get; set; }

    /// <summary>
    /// item's examples
    /// content in different dev langs can be put in this property all together
    /// </summary>
    [YamlMember(Alias = "example")]
    [JsonProperty("example")]
    [JsonPropertyName("example")]
    [MergeOption(MergeOption.Replace)]
    [MarkdownContent]
    public List<string> Examples { get; set; }

    /// <summary>
    /// item's syntax
    /// as <see cref="SyntaxDetailViewModel"/> support different dev langs, SyntaxInDevLangs is not necessary
    /// </summary>
    [YamlMember(Alias = "syntax")]
    [JsonProperty("syntax")]
    [JsonPropertyName("syntax")]
    public SyntaxDetailViewModel Syntax { get; set; }

    [YamlMember(Alias = "overridden")]
    [JsonProperty("overridden")]
    [JsonPropertyName("overridden")]
    [UniqueIdentityReference]
    public string Overridden { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Overridden)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> OverriddenInDevLangs { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.Overload)]
    [JsonProperty(Constants.PropertyName.Overload)]
    [JsonPropertyName(Constants.PropertyName.Overload)]
    [UniqueIdentityReference]
    public string Overload { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Overload)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> OverloadInDevLangs { get; set; } = [];

    [YamlMember(Alias = "exceptions")]
    [JsonProperty("exceptions")]
    [JsonPropertyName("exceptions")]
    public List<ExceptionInfo> Exceptions { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Exceptions)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<ExceptionInfo>> ExceptionsInDevLangs { get; set; } = [];

    [YamlMember(Alias = "seealso")]
    [JsonProperty("seealso")]
    [JsonPropertyName("seealso")]
    public List<LinkInfo> SeeAlsos { get; set; }

    [YamlMember(Alias = Constants.PropertyName.SeeAlsoContent)]
    [JsonProperty(Constants.PropertyName.SeeAlsoContent)]
    [JsonPropertyName(Constants.PropertyName.SeeAlsoContent)]
    [MarkdownContent]
    public string SeeAlsoContent { get; set; }

    [YamlMember(Alias = "see")]
    [JsonProperty("see")]
    [JsonPropertyName("see")]
    public List<LinkInfo> Sees { get; set; }

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [UniqueIdentityReference]
    public List<string> SeeAlsosUidReference => SeeAlsos?.Where(s => s.LinkType == LinkType.CRef).Select(s => s.LinkId).ToList();

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [UniqueIdentityReference]
    public List<string> SeesUidReference => Sees?.Where(s => s.LinkType == LinkType.CRef).Select(s => s.LinkId).ToList();

    /// <summary>
    /// item's inheritance
    /// use tree as multiple inheritance is allowed in languages like Python
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Inheritance)]
    [MergeOption(MergeOption.Ignore)]
    [JsonProperty(Constants.PropertyName.Inheritance)]
    [JsonPropertyName(Constants.PropertyName.Inheritance)]
    public List<InheritanceTree> Inheritance { get; set; }

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [UniqueIdentityReference]
    public List<string> InheritanceUidReference => GetInheritanceUidReference(Inheritance)?.ToList() ?? [];

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Inheritance)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<InheritanceTree>> InheritanceInDevLangs { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.DerivedClasses)]
    [MergeOption(MergeOption.Ignore)]
    [JsonProperty(Constants.PropertyName.DerivedClasses)]
    [JsonPropertyName(Constants.PropertyName.DerivedClasses)]
    [UniqueIdentityReference]
    public List<string> DerivedClasses { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.DerivedClasses)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<string>> DerivedClassesInDevLangs { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.Implements)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.Implements)]
    [JsonPropertyName(Constants.PropertyName.Implements)]
    [UniqueIdentityReference]
    public List<string> Implements { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Implements)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<string>> ImplementsInDevLangs { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.InheritedMembers)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.InheritedMembers)]
    [JsonPropertyName(Constants.PropertyName.InheritedMembers)]
    [UniqueIdentityReference]
    public List<string> InheritedMembers { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.InheritedMembers)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<string>> InheritedMembersInDevLangs { get; set; } = [];

    [YamlMember(Alias = Constants.PropertyName.ExtensionMethods)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonProperty(Constants.PropertyName.ExtensionMethods)]
    [JsonPropertyName(Constants.PropertyName.ExtensionMethods)]
    [UniqueIdentityReference]
    public List<string> ExtensionMethods { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.ExtensionMethods)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<string>> ExtensionMethodsInDevLangs { get; set; } = [];

    /// <summary>
    /// item's conceptual
    /// content in different dev langs can be put in this property all together
    /// </summary>
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

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Platform)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<string>> PlatformInDevLangs { get; set; } = [];

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
    public CompositeDictionary ExtensionData
    {
        get
        {
            return CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Parent, ParentInDevLangs, JTokenConverter.Convert<string>)
            .Add("package" + Constants.PrefixSeparator, PackageInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.Children, ChildrenInDevLangs, JTokenConverter.Convert<List<string>>)
            .Add(Constants.ExtensionMemberPrefix.Source, SourceInDevLangs, JTokenConverter.Convert<SourceDetail>)
            .Add(Constants.ExtensionMemberPrefix.Namespace, NamespaceNameInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.Assemblies, AssemblyNameListInDevLangs, JTokenConverter.Convert<List<string>>)
            .Add(Constants.ExtensionMemberPrefix.Overridden, OverriddenInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.Exceptions, ExceptionsInDevLangs, JTokenConverter.Convert<List<ExceptionInfo>>)
            .Add(Constants.ExtensionMemberPrefix.Inheritance, InheritanceInDevLangs, JTokenConverter.Convert<List<InheritanceTree>>)
            .Add(Constants.ExtensionMemberPrefix.DerivedClasses, DerivedClassesInDevLangs, JTokenConverter.Convert<List<string>>)
            .Add(Constants.ExtensionMemberPrefix.Implements, ImplementsInDevLangs, JTokenConverter.Convert<List<string>>)
            .Add(Constants.ExtensionMemberPrefix.InheritedMembers, InheritedMembersInDevLangs, JTokenConverter.Convert<List<string>>)
            .Add(Constants.ExtensionMemberPrefix.ExtensionMethods, ExtensionMethodsInDevLangs, JTokenConverter.Convert<List<string>>)
            .Add(Constants.ExtensionMemberPrefix.Platform, PlatformInDevLangs, JTokenConverter.Convert<List<string>>)
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

    private IEnumerable<string> GetInheritanceUidReference(List<InheritanceTree> items)
    {
        return items
            ?.Select(GetInheritanceUidReference)
            .SelectMany(s => s);
    }

    private IEnumerable<string> GetInheritanceUidReference(InheritanceTree item)
    {
        if (item == null)
        {
            yield break;
        }

        if (item.Inheritance != null)
        {
            foreach (var i in GetInheritanceUidReference(item.Inheritance))
            {
                yield return i;
            }
        }
        if (!string.IsNullOrEmpty(item.Type))
        {
            yield return item.Type;
        }
    }
}
