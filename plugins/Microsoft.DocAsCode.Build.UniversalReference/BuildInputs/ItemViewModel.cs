// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ItemViewModel : IOverwriteDocumentViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        [MergeOption(MergeOption.MergeKey)]
        public string Uid { get; set; }

        [YamlMember(Alias = Constants.PropertyName.CommentId)]
        [JsonProperty(Constants.PropertyName.CommentId)]
        public string CommentId { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Id)]
        [JsonProperty(Constants.PropertyName.Id)]
        public string Id { get; set; }

        [YamlMember(Alias = "isEii")]
        [JsonProperty("isEii")]
        public bool IsExplicitInterfaceImplementation { get; set; }

        [YamlMember(Alias = "isExtensionMethod")]
        [JsonProperty("isExtensionMethod")]
        public bool IsExtensionMethod { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Parent)]
        [JsonProperty(Constants.PropertyName.Parent)]
        [UniqueIdentityReference]
        public string Parent { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Parent)]
        [JsonIgnore]
        public SortedList<string, string> ParentInDevLangs { get; set; } = new SortedList<string, string>();

        [YamlMember(Alias = Constants.PropertyName.Children)]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty(Constants.PropertyName.Children)]
        [UniqueIdentityReference]
        public List<string> Children { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Children)]
        [JsonIgnore]
        public SortedList<string, List<string>> ChildrenInDevLangs { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = Constants.PropertyName.Href)]
        [JsonProperty(Constants.PropertyName.Href)]
        public string Href { get; set; }

        [YamlMember(Alias = "langs")]
        [JsonProperty("langs")]
        public string[] SupportedLanguages { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Name)]
        [JsonProperty(Constants.PropertyName.Name)]
        public string Name { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
        [JsonIgnore]
        public SortedList<string, string> Names { get; set; } = new SortedList<string, string>();

        [YamlMember(Alias = Constants.PropertyName.NameWithType)]
        [JsonProperty(Constants.PropertyName.NameWithType)]
        public string NameWithType { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
        [JsonIgnore]
        public SortedList<string, string> NamesWithType { get; set; } = new SortedList<string, string>();

        [YamlMember(Alias = Constants.PropertyName.FullName)]
        [JsonProperty(Constants.PropertyName.FullName)]
        public string FullName { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
        [JsonIgnore]
        public SortedList<string, string> FullNames { get; set; } = new SortedList<string, string>();

        [YamlMember(Alias = Constants.PropertyName.Type)]
        [JsonProperty(Constants.PropertyName.Type)]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Source)]
        [JsonProperty(Constants.PropertyName.Source)]
        public SourceDetail Source { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Source)]
        [JsonIgnore]
        public SortedList<string, SourceDetail> SourceInDevLangs { get; set; } = new SortedList<string, SourceDetail>();

        [YamlMember(Alias = Constants.PropertyName.Documentation)]
        [JsonProperty(Constants.PropertyName.Documentation)]
        public SourceDetail Documentation { get; set; }

        [YamlMember(Alias = UniversalReferenceConstants.ExtensionMemberPrefix.Assemblies)]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty(UniversalReferenceConstants.ExtensionMemberPrefix.Assemblies)]
        public List<string> AssemblyNameList { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Assemblies)]
        [JsonIgnore]
        public SortedList<string, List<string>> AssemblyNameListInDevLangs { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = Constants.PropertyName.Namespace)]
        [JsonProperty(Constants.PropertyName.Namespace)]
        [UniqueIdentityReference]
        public string NamespaceName { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Namespace)]
        [JsonIgnore]
        public SortedList<string, string> NamespaceNameInDevLangs { get; set; } = new SortedList<string, string>();

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        [MarkdownContent]
        public string Summary { get; set; }

        [YamlMember(Alias = "remarks")]
        [JsonProperty("remarks")]
        [MarkdownContent]
        public string Remarks { get; set; }

        [YamlMember(Alias = "example")]
        [JsonProperty("example")]
        [MergeOption(MergeOption.Replace)]
        [MarkdownContent]
        public List<string> Examples { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public SyntaxDetailViewModel Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        [JsonProperty("overridden")]
        [UniqueIdentityReference]
        public string Overridden { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Overridden)]
        [JsonIgnore]
        public SortedList<string, string> OverriddenInDevLangs { get; set; } = new SortedList<string, string>();

        [YamlMember(Alias = "overload")]
        [JsonProperty("overload")]
        [UniqueIdentityReference]
        public string Overload { get; set; }

        [YamlMember(Alias = "exceptions")]
        [JsonProperty("exceptions")]
        public List<ExceptionInfo> Exceptions { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Exceptions)]
        [JsonIgnore]
        public SortedList<string, List<ExceptionInfo>> ExceptionsInDevLangs { get; set; } = new SortedList<string, List<ExceptionInfo>>();

        [YamlMember(Alias = "seealso")]
        [JsonProperty("seealso")]
        public List<LinkInfo> SeeAlsos { get; set; }

        [YamlMember(Alias = "see")]
        [JsonProperty("see")]
        public List<LinkInfo> Sees { get; set; }

        [JsonIgnore]
        [YamlIgnore]
        [UniqueIdentityReference]
        public List<string> SeeAlsosUidReference => SeeAlsos?.Where(s => s.LinkType == LinkType.CRef).Select(s => s.LinkId).ToList();

        [JsonIgnore]
        [YamlIgnore]
        [UniqueIdentityReference]
        public List<string> SeesUidReference => Sees?.Where(s => s.LinkType == LinkType.CRef).Select(s => s.LinkId).ToList();

        /// <summary>
        /// item's inheritance
        /// multiple inheritance is allowed in languages like Python
        /// </summary>
        [YamlMember(Alias = Constants.PropertyName.Inheritance)]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty(Constants.PropertyName.Inheritance)]
        [UniqueIdentityReference]
        public InheritanceTree Inheritance { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Inheritance)]
        [JsonIgnore]
        public SortedList<string, InheritanceTree> InheritanceInDevLangs { get; set; } = new SortedList<string, InheritanceTree>();

        [YamlMember(Alias = Constants.PropertyName.DerivedClasses)]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty(Constants.PropertyName.DerivedClasses)]
        [UniqueIdentityReference]
        public List<string> DerivedClasses { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.DerivedClasses)]
        [JsonIgnore]
        public SortedList<string, List<string>> DerivedClassesInDevLangs { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = Constants.PropertyName.Implements)]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty(Constants.PropertyName.Implements)]
        [UniqueIdentityReference]
        public List<string> Implements { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Implements)]
        [JsonIgnore]
        public SortedList<string, List<string>> ImplementsInDevLangs { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = Constants.PropertyName.InheritedMembers)]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty(Constants.PropertyName.InheritedMembers)]
        [UniqueIdentityReference]
        public List<string> InheritedMembers { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.InheritedMembers)]
        [JsonIgnore]
        public SortedList<string, List<string>> InheritedMembersInDevLangs { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = Constants.PropertyName.ExtensionMethods)]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonProperty(Constants.PropertyName.ExtensionMethods)]
        [UniqueIdentityReference]
        public List<string> ExtensionMethods { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.ExtensionMethods)]
        [JsonIgnore]
        public SortedList<string, List<string>> ExtensionMethodsInDevLangs { get; set; } = new SortedList<string, List<string>>();

        [ExtensibleMember(Constants.ExtensionMemberPrefix.Modifiers)]
        [MergeOption(MergeOption.Ignore)] // todo : merge more children
        [JsonIgnore]
        public SortedList<string, List<string>> Modifiers { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = Constants.PropertyName.Conceptual)]
        [JsonProperty(Constants.PropertyName.Conceptual)]
        [MarkdownContent]
        public string Conceptual { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Platform)]
        [JsonProperty(Constants.PropertyName.Platform)]
        [MergeOption(MergeOption.Replace)]
        public List<string> Platform { get; set; }

        [ExtensibleMember(UniversalReferenceConstants.ExtensionMemberPrefix.Platform)]
        [JsonIgnore]
        public SortedList<string, List<string>> PlatformInDevLangs { get; set; } = new SortedList<string, List<string>>();

        [YamlMember(Alias = "attributes")]
        [JsonProperty("attributes")]
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
        public CompositeDictionary ExtensionData =>
            CompositeDictionary
                .CreateBuilder()
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Parent, ParentInDevLangs, JTokenConverter.Convert<string>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Children, ChildrenInDevLangs, JTokenConverter.Convert<List<string>>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Source, SourceInDevLangs, JTokenConverter.Convert<SourceDetail>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Namespace, NamespaceNameInDevLangs, JTokenConverter.Convert<string>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Assemblies, AssemblyNameListInDevLangs, JTokenConverter.Convert<List<string>>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Overridden, OverriddenInDevLangs, JTokenConverter.Convert<string>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Exceptions, ExceptionsInDevLangs, JTokenConverter.Convert<List<ExceptionInfo>>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Inheritance, InheritanceInDevLangs, JTokenConverter.Convert<InheritanceTree>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.DerivedClasses, DerivedClassesInDevLangs, JTokenConverter.Convert<List<string>>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Implements, ImplementsInDevLangs, JTokenConverter.Convert<List<string>>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.InheritedMembers, InheritedMembersInDevLangs, JTokenConverter.Convert<List<string>>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.ExtensionMethods, ExtensionMethodsInDevLangs, JTokenConverter.Convert<List<string>>)
                .Add(UniversalReferenceConstants.ExtensionMemberPrefix.Platform, PlatformInDevLangs, JTokenConverter.Convert<List<string>>)
                .Add(Constants.ExtensionMemberPrefix.Name, Names, JTokenConverter.Convert<string>)
                .Add(Constants.ExtensionMemberPrefix.NameWithType, NamesWithType, JTokenConverter.Convert<string>)
                .Add(Constants.ExtensionMemberPrefix.FullName, FullNames, JTokenConverter.Convert<string>)
                .Add(Constants.ExtensionMemberPrefix.Modifiers, Modifiers, JTokenConverter.Convert<List<string>>)
                .Add(string.Empty, Metadata)
                .Create();
    }
}
