// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class ItemViewModel : IOverwriteDocumentViewModel
{
    /// <summary>
    /// item's unique identifier
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    [MergeOption(MergeOption.MergeKey)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    /// <summary>
    /// item's identifier
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Id)]
    [JsonPropertyName(Constants.PropertyName.Id)]
    public string Id { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Parent)]
    [JsonPropertyName(Constants.PropertyName.Parent)]
    [UniqueIdentityReference]
    public string Parent { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Parent)]
    [JsonIgnore]
    public SortedList<string, string> ParentInDevLangs { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = "package")]
    [JsonPropertyName("package")]
    [UniqueIdentityReference]
    public string Package { get; set; }

    [ExtensibleMember("package" + Constants.PrefixSeparator)]
    [JsonIgnore]
    public SortedList<string, string> PackageInDevLangs { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = Constants.PropertyName.Children)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.Children)]
    [UniqueIdentityReference]
    public List<string> Children { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Children)]
    [JsonIgnore]
    public SortedList<string, List<string>> ChildrenInDevLangs { get; set; } = new SortedList<string, List<string>>();

    /// <summary>
    /// item's link URL
    /// As an item(uid) can be resolved to only one link in cross reference, HrefInDevLangs is not supported
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }

    [YamlMember(Alias = "langs")]
    [JsonPropertyName("langs")]
    public string[] SupportedLanguages { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonPropertyName(Constants.PropertyName.Name)]
    public string Name { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
    [JsonIgnore]
    public SortedList<string, string> Names { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = Constants.PropertyName.NameWithType)]
    [JsonPropertyName(Constants.PropertyName.NameWithType)]
    public string NameWithType { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
    [JsonIgnore]
    public SortedList<string, string> NamesWithType { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = Constants.PropertyName.FullName)]
    [JsonPropertyName(Constants.PropertyName.FullName)]
    public string FullName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
    [JsonIgnore]
    public SortedList<string, string> FullNames { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public string Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Source)]
    [JsonPropertyName(Constants.PropertyName.Source)]
    public SourceDetail Source { get; set; }

    /// <summary>
    /// item's source code's source detail in different dev langs
    /// </summary>
    [ExtensibleMember(Constants.ExtensionMemberPrefix.Source)]
    [JsonIgnore]
    public SortedList<string, SourceDetail> SourceInDevLangs { get; set; } = new SortedList<string, SourceDetail>();

    /// <summary>
    /// item's documentation's source detail
    /// as overwrite document targets uid, DocumentationInDevLangs is not supported
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = Constants.ExtensionMemberPrefix.Assemblies)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.ExtensionMemberPrefix.Assemblies)]
    public List<string> AssemblyNameList { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Assemblies)]
    [JsonIgnore]
    public SortedList<string, List<string>> AssemblyNameListInDevLangs { get; set; } = new SortedList<string, List<string>>();

    [YamlMember(Alias = Constants.PropertyName.Namespace)]
    [JsonPropertyName(Constants.PropertyName.Namespace)]
    [UniqueIdentityReference]
    public string NamespaceName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Namespace)]
    [JsonIgnore]
    public SortedList<string, string> NamespaceNameInDevLangs { get; set; } = new SortedList<string, string>();

    /// <summary>
    /// item's summary
    /// content in different dev langs can be put in this property all together
    /// </summary>
    [YamlMember(Alias = "summary")]
    [JsonPropertyName("summary")]
    [MarkdownContent]
    public string Summary { get; set; }

    /// <summary>
    /// item's remarks
    /// content in different dev langs can be put in this property all together
    /// </summary>
    [YamlMember(Alias = "remarks")]
    [JsonPropertyName("remarks")]
    [MarkdownContent]
    public string Remarks { get; set; }

    /// <summary>
    /// item's examples
    /// content in different dev langs can be put in this property all together
    /// </summary>
    [YamlMember(Alias = "example")]
    [JsonPropertyName("example")]
    [MergeOption(MergeOption.Replace)]
    [MarkdownContent]
    public List<string> Examples { get; set; }

    /// <summary>
    /// item's syntax
    /// as <see cref="SyntaxDetailViewModel"/> support different dev langs, SyntaxInDevLangs is not necessary
    /// </summary>
    [YamlMember(Alias = "syntax")]
    [JsonPropertyName("syntax")]
    public SyntaxDetailViewModel Syntax { get; set; }

    [YamlMember(Alias = "overridden")]
    [JsonPropertyName("overridden")]
    [UniqueIdentityReference]
    public string Overridden { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Overridden)]
    [JsonIgnore]
    public SortedList<string, string> OverriddenInDevLangs { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = Constants.PropertyName.Overload)]
    [JsonPropertyName(Constants.PropertyName.Overload)]
    [UniqueIdentityReference]
    public string Overload { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Overload)]
    [JsonIgnore]
    public SortedList<string, string> OverloadInDevLangs { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = "exceptions")]
    [JsonPropertyName("exceptions")]
    public List<ExceptionInfo> Exceptions { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Exceptions)]
    [JsonIgnore]
    public SortedList<string, List<ExceptionInfo>> ExceptionsInDevLangs { get; set; } = new SortedList<string, List<ExceptionInfo>>();

    [YamlMember(Alias = "seealso")]
    [JsonPropertyName("seealso")]
    public List<LinkInfo> SeeAlsos { get; set; }

    [YamlMember(Alias = Constants.PropertyName.SeeAlsoContent)]
    [JsonPropertyName(Constants.PropertyName.SeeAlsoContent)]
    [MarkdownContent]
    public string SeeAlsoContent { get; set; }

    [YamlMember(Alias = "see")]
    [JsonPropertyName("see")]
    public List<LinkInfo> Sees { get; set; }

    [YamlIgnore]
    [JsonIgnore]
    [UniqueIdentityReference]
    public List<string> SeeAlsosUidReference => SeeAlsos?.Where(s => s.LinkType == LinkType.CRef).Select(s => s.LinkId).ToList();

    [YamlIgnore]
    [JsonIgnore]
    [UniqueIdentityReference]
    public List<string> SeesUidReference => Sees?.Where(s => s.LinkType == LinkType.CRef).Select(s => s.LinkId).ToList();

    /// <summary>
    /// item's inheritance
    /// use tree as multiple inheritance is allowed in languages like Python
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Inheritance)]
    [MergeOption(MergeOption.Ignore)]
    [JsonPropertyName(Constants.PropertyName.Inheritance)]
    public List<InheritanceTree> Inheritance { get; set; }

    [YamlIgnore]
    [JsonIgnore]
    [UniqueIdentityReference]
    public List<string> InheritanceUidReference => GetInheritanceUidReference(Inheritance)?.ToList() ?? new List<string>();

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Inheritance)]
    [JsonIgnore]
    public SortedList<string, List<InheritanceTree>> InheritanceInDevLangs { get; set; } = new SortedList<string, List<InheritanceTree>>();

    [YamlMember(Alias = Constants.PropertyName.DerivedClasses)]
    [MergeOption(MergeOption.Ignore)]
    [JsonPropertyName(Constants.PropertyName.DerivedClasses)]
    [UniqueIdentityReference]
    public List<string> DerivedClasses { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.DerivedClasses)]
    [JsonIgnore]
    public SortedList<string, List<string>> DerivedClassesInDevLangs { get; set; } = new SortedList<string, List<string>>();

    [YamlMember(Alias = Constants.PropertyName.Implements)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.Implements)]
    [UniqueIdentityReference]
    public List<string> Implements { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Implements)]
    [JsonIgnore]
    public SortedList<string, List<string>> ImplementsInDevLangs { get; set; } = new SortedList<string, List<string>>();

    [YamlMember(Alias = Constants.PropertyName.InheritedMembers)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.InheritedMembers)]
    [UniqueIdentityReference]
    public List<string> InheritedMembers { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.InheritedMembers)]
    [JsonIgnore]
    public SortedList<string, List<string>> InheritedMembersInDevLangs { get; set; } = new SortedList<string, List<string>>();

    [YamlMember(Alias = Constants.PropertyName.ExtensionMethods)]
    [MergeOption(MergeOption.Ignore)] // todo : merge more children
    [JsonPropertyName(Constants.PropertyName.ExtensionMethods)]
    [UniqueIdentityReference]
    public List<string> ExtensionMethods { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.ExtensionMethods)]
    [JsonIgnore]
    public SortedList<string, List<string>> ExtensionMethodsInDevLangs { get; set; } = new SortedList<string, List<string>>();

    /// <summary>
    /// item's conceptual
    /// content in different dev langs can be put in this property all together
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Conceptual)]
    [JsonPropertyName(Constants.PropertyName.Conceptual)]
    [MarkdownContent]
    public string Conceptual { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Platform)]
    [JsonPropertyName(Constants.PropertyName.Platform)]
    [MergeOption(MergeOption.Replace)]
    public List<string> Platform { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Platform)]
    [JsonIgnore]
    public SortedList<string, List<string>> PlatformInDevLangs { get; set; } = new SortedList<string, List<string>>();

    [ExtensibleMember]
    [JsonIgnore]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [JsonExtensionData]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public CompositeDictionary ExtensionData =>
        CompositeDictionary
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
