// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    public static class ErrorCodes
    {
        public static class Build
        {
            public const string InvalidPropertyFormat = "InvalidPropertyFormat";
            public const string InvalidInputFile = "InvalidInputFile";
            public const string InvalidRelativePath = "InvalidRelativePath";
            public const string InvalidHref = "InvalidHref";
            public const string InvalidTocLink = "InvalidTocLink";
            public const string InvalidTocFile = "InvalidTocFile";
            public const string InternalUidNotFound = "InternalUidNotFound";
            public const string InvalidMarkdown = "InvalidMarkdown";
            public const string BuildSubCommandConfigNotFound = "BuildSubCommandConfigNotFound";
            public const string MetadataSubCommandConfigNotFound = "MetadataSubCommandConfigNotFound";
            public const string PdfSubCommandConfigNotFound = "PdfSubCommandConfigNotFound";
            public const string FileNamesMaxLengthExceeded = "FileNamesMaxLengthExceeded";
            public const string UidFoundInMultipleArticles = "UidFoundInMultipleArticles";
            public const string TemplateModelTransformError = "TemplateModelTransformError";
            public const string InvalidYamlHeader = "InvalidYamlHeader";
            public const string ApplyTemplateError = "ApplyTemplateError";
            public const string OverwriteDocumentMergeError = "OverwriteDocumentMergeError";
            public const string UnsupportedTocHrefType = "UnsupportedTocHrefType";
            public const string TopicHrefNotset = "TopicHrefNotset";
            public const string CircularReferenceFound = "CircularReferenceFound";
            public const string UnsupportedFileFormat = "UnsupportedFileFormat";
            public const string FatalError = "FatalError";
        }
    }
}
