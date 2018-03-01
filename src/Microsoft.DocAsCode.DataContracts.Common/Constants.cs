// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    public static class Constants
    {
        public const string YamlExtension = ".yml";
        public const string ContentPlaceholder = "*content";
        public const string PrefixSeparator = ".";
        public const string TocYamlFileName = "toc.yml";

        public static class DocumentType
        {
            public const string Toc = "Toc";
        }

        /// <summary>
        /// TODO: add other property name const
        /// </summary>
        public static class PropertyName
        {
            public const string Uid = "uid";
            public const string CommentId = "commentId";
            public const string Id = "id";
            public const string Href = "href";
            public const string Type = "type";
            public const string Source = "source";
            public const string Path = "path";
            public const string DocumentType = "documentType";
            public const string Title = "title";
            public const string Conceptual = "conceptual";
            public const string Documentation = "documentation";
            public const string Summary = "summary";
            public const string IsEii = "isEii";

            public const string Name = "name";
            public const string DisplayName = "displayName";
            public const string NameWithType = "nameWithType";
            public const string FullName = "fullName";
            public const string Content = "content";
            public const string TocHref = "tocHref";
            public const string TopicHref = "topicHref";
            public const string TopicUid = "topicUid";
            public const string Platform = "platform";
            public const string Parent = "parent";
            public const string Children = "children";
            public const string Namespace = "namespace";
            public const string Assemblies = "assemblies";
            public const string Overridden = "overridden";
            public const string Exceptions = "exceptions";
            public const string Inheritance = "inheritance";
            public const string DerivedClasses = "derivedClasses";
            public const string Implements = "implements";
            public const string InheritedMembers = "inheritedMembers";
            public const string ExtensionMethods = "extensionMethods";
            public const string Overload = "overload";
            public const string Return = "return";
            public const string SeeAlsoContent = "seealsoContent";
            public const string Syntax = "syntax";
            public const string AdditionalNotes = "additionalNotes";
            public const string SystemKeys = "_systemKeys";

            public const string OutputFileName = "outputFileName";
        }

        public static class MetadataName
        {
            public const string Version = "version";
        }

        public static class ExtensionMemberPrefix
        {
            public const string NameWithType = PropertyName.NameWithType + PrefixSeparator;
            public const string FullName = PropertyName.FullName + PrefixSeparator;
            public const string Name = PropertyName.Name + PrefixSeparator;
            public const string Modifiers = "modifiers" + PrefixSeparator;
            public const string Spec = "spec" + PrefixSeparator;
            public const string Content = PropertyName.Content + PrefixSeparator;
            public const string Parent = PropertyName.Parent + PrefixSeparator;
            public const string Children = PropertyName.Children + PrefixSeparator;
            public const string Source = PropertyName.Source + PrefixSeparator;
            public const string Namespace = PropertyName.Namespace + PrefixSeparator;
            public const string Assemblies = PropertyName.Assemblies + PrefixSeparator;
            public const string Overridden = PropertyName.Overridden + PrefixSeparator;
            public const string Exceptions = PropertyName.Exceptions + PrefixSeparator;
            public const string Inheritance = PropertyName.Inheritance + PrefixSeparator;
            public const string DerivedClasses = PropertyName.DerivedClasses + PrefixSeparator;
            public const string Implements = PropertyName.Implements + PrefixSeparator;
            public const string InheritedMembers = PropertyName.InheritedMembers + PrefixSeparator;
            public const string ExtensionMethods = PropertyName.ExtensionMethods + PrefixSeparator;
            public const string Platform = PropertyName.Platform + PrefixSeparator;
            public const string Return = PropertyName.Return + PrefixSeparator;
            public const string Overload = PropertyName.Overload + PrefixSeparator;
        }

        public static class DevLang
        {
            public const string CSharp = "csharp";
            public const string VB = "vb";
        }

        public static class TableOfContents
        {
            public const string MarkdownTocFileName = "toc.md";
            public const string YamlTocFileName = "toc.yml";
        }
    }
}
