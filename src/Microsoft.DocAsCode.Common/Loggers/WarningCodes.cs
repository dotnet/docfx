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
            public const string InvalidInternalBookmark = "InvalidInternalBookmark";
            public const string InvalidExternalBookmark = "InvalidExternalBookmark";
            public const string InvalidFileLink = "InvalidFileLink";
            public const string DuplicateUids = "DuplicateUids";
            public const string DuplicateOutputFiles = "DuplicateOutputFiles";
        }

        public static class Markdown
        {
            public const string InvalidInclude = "InvalidInclude";
            public const string InvalidCodeSnippet = "InvalidCodeSnippet";
            public const string InvalidInlineCodeSnippet = "InvalidInlineCodeSnippet";
            public const string NoVisibleTab = "NoVisibleTab";
        }
    }
}
