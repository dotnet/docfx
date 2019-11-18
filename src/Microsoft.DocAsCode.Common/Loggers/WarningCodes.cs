// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    public static class WarningCodes
    {
        public static class Metadata
        {

        }

        public static class Build
        {
            public const string TooManyWarnings = "TooManyWarnings";
            public const string InvalidBookmark = "InvalidBookmark";
            public const string InvalidFileLink = "InvalidFileLink";
            public const string DuplicateUids = "DuplicateUids";
            public const string DuplicateOutputFiles = "DuplicateOutputFiles";
            public const string UnknownUriTemplatePipeline = "UnknownUriTemplatePipeline";
            public const string EmptyTocItemNode = "EmptyTocItemNode";
            public const string EmptyTocItemName = "EmptyTocItemName";
            public const string EmptyInputFiles = "EmptyInputFiles";
            public const string EmptyInputContents = "EmptyInputContents";
            public const string EmptyOutputFiles = "EmptyOutputFiles";
            public const string InvalidTagParametersConfig = "InvalidTagParametersConfig";
            public const string InvalidTaggedPropertyType = "InvalidTaggedPropertyType";
            // todo : add uid not found in SDP.
            public const string UidNotFound = "UidNotFound";
            public const string ReferencedXrefPropertyNotString = "ReferencedXrefPropertyNotString";
            public const string UnknownContentType = "UnknownContentType";
            public const string UnknownContentTypeForTemplate = "UnknownContentTypeForTemplate";
            public const string InvalidTocInclude = "InvalidTocInclude";
        }

        public static class Markdown
        {
            public const string InvalidInclude = "InvalidInclude";
            public const string InvalidCodeSnippet = "InvalidCodeSnippet";
            public const string InvalidInlineCodeSnippet = "InvalidInlineCodeSnippet";
            public const string InvalidYamlHeader = "InvalidYamlHeader";
            public const string NoVisibleTab = "NoVisibleTab";
            public const string DuplicateTabId = "DuplicateTabId";
            public const string DifferentTabIdSet = "DifferentTabIdSet";
            public const string MissingNewLineBelowSectionHeader = "MissingNewLineBelowSectionHeader";
            public const string InvalidTabGroup = "InvalidTabGroup";
        }

        public static class Overwrite
        {
            public const string InvalidYamlCodeBlockLanguage = "InvalidYamlCodeBlockLanguage";
            public const string InvalidOPaths = "InvalidOPaths";
            public const string DuplicateOPaths = "DuplicateOPaths";
            public const string InvalidMarkdownFragments = "InvalidMarkdownFragments";
        }

        public static class Yaml
        {
            public const string MissingYamlMime = "MissingYamlMime";
        }
    }
}
