// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    public static class Constants
    {
        public const string YamlExtension = ".yml";
        public const string ContentPlaceholder = "*content";
        public const string PrefixSeparator = ".";

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

            public const string Name = "name";
            public const string DisplayName = "displayName";
            public const string NameWithType = "nameWithType";
            public const string FullName = "fullName";
            public const string TocHref = "tocHref";
            public const string TopicHref = "topicHref";
            public const string TopicUid = "topicUid";

            public const string SystemKeys = "_systemKeys";
        }

        public static class ExtensionMemberPrefix
        {
            public const string NameWithType = PropertyName.NameWithType + PrefixSeparator;
            public const string FullName = PropertyName.FullName + PrefixSeparator;
            public const string Name = PropertyName.Name + PrefixSeparator;
            public const string Modifiers = "modifiers" + PrefixSeparator;
            public const string Spec = "spec" + PrefixSeparator;
        }

        public static class DevLang
        {
            public const string CSharp = "csharp";
            public const string VB = "vb";
        }
    }
}
