// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common;

public static class ErrorCodes
{
    public static class Build
    {
        public const string ViolateSchema = "ViolateSchema";
        public const string InvalidPropertyFormat = "InvalidPropertyFormat";
        public const string InvalidInputFile = "InvalidInputFile";
        public const string InvalidRelativePath = "InvalidRelativePath";
        public const string InvalidHref = "InvalidHref";
        public const string InternalUidNotFound = "InternalUidNotFound";
        public const string InvalidMarkdown = "InvalidMarkdown";
        public const string FileNamesMaxLengthExceeded = "FileNamesMaxLengthExceeded";
        public const string UidFoundInMultipleArticles = "UidFoundInMultipleArticles";
        public const string UnsupportedTocHrefType = "UnsupportedTocHrefType";
        public const string TopicHrefNotset = "TopicHrefNotset";
        public const string InvalidYamlFile = "InvalidYamlFile";
        public const string FatalError = "FatalError";
    }

    public static class Toc
    {
        public const string InvalidMarkdownToc = "InvalidMarkdownToc";
        public const string InvalidTocLink = "InvalidTocLink";
        public const string InvalidTocFile = "InvalidTocFile";
        public const string CircularTocInclusion = "CircularTocInclusion";
    }

    public static class Template
    {
        public const string ApplyTemplatePreprocessorError = "ApplyTemplatePreprocessorError";
        public const string ApplyTemplateRendererError = "ApplyTemplateRendererError";
    }

    public static class Overwrite
    {
        public const string InvalidOverwriteDocument = "InvalidOverwriteDocument";
        public const string OverwriteDocumentMergeError = "OverwriteDocumentMergeError";
    }
}
