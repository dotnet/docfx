﻿// Copyright (c) Microsoft. All rights reserved.
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
            public const string InvalidInternalBookmark = "InvalidInternalBookmark";
            public const string InvalidExternalBookmark = "InvalidExternalBookmark";
            public const string InvalidFileLink = "InvalidFileLink";
            public const string DuplicateUids = "DuplicateUids";
            public const string DuplicateOutputFiles = "DuplicateOutputFiles";
            public const string UnknownUriTemplatePipeline = "UnknownUriTemplatePipeline";
            public const string EmptyTocItemName = "EmptyTocItemName";
            public const string EmptyInputFiles = "EmptyInputFiles";
            public const string InvalidTagParametersConfig = "InvalidTagParametersConfig";
            public const string InvalidTaggedPropertyType = "InvalidTaggedPropertyType";
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
        }

        public static class Overwrite
        {
            public const string InvalidYamlCodeBlockLanguage = "InvalidYamlCodeBlockLanguage";
            public const string InvalidOPaths = "InvalidOPaths";
            public const string DuplicateOPaths = "DuplicateOPaths";
            public const string InvalidMarkdownFragments = "InvalidMarkdownFragments";
        }
    }
}
