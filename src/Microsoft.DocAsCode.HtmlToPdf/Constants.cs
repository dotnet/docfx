// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    internal static class Constants
    {
        public const string PdfCommandName = "wkhtmltopdf";
        public const string PdfCommandNotExistMessage = "wkhtmltopdf is a prerequisite when generating PDF. Please install it from https://wkhtmltopdf.org/downloads.html and save the executable folder to %PATH% first. Alternatively you can install it from https://chocolatey.org with `choco install wkhtmltopdf`.";
    }

    internal static class BuildToolConstants
    {
        public static class OutputFileExtensions
        {
            /// <summary>
            /// The file extension of Toc output file
            /// </summary>
            public const string TocFileExtension = ".json";

            /// <summary>
            /// The file extension of content output file in HTML
            /// </summary>
            public const string ContentHtmlExtension = ".html";

            /// <summary>
            /// The file extension of content output file in raw page
            /// </summary>
            public const string ContentRawPageExtension = ".raw.page.json";

            /// <summary>
            /// The file extension of metadata file
            /// </summary>
            public const string MetadataExtension = ".mta.json";
        }
    }

    internal static class FileExtensions
    {
        public const string JsonExtension = ".json";
        public const string PdfExtension = ".pdf";
        public const string MdExtension = ".md";
    }

    internal static class ManifestConstants
    {
        public const string ManifestFileName = "manifest.json";

        public static class BuildManifestItem
        {
            public const string Type = "type";
            public const string Original = "original";
            public const string OriginalType = "original_type";
            public const string SourceRelativePath = "source_relative_path";
            public const string Output = "output";
            public const string Files = "files";

            public const string OutputHtml = BuildToolConstants.OutputFileExtensions.ContentHtmlExtension;
            public const string OutputMtaJson = BuildToolConstants.OutputFileExtensions.MetadataExtension;
            public const string OutputRawPageJson = BuildToolConstants.OutputFileExtensions.ContentRawPageExtension;
            public const string OutputResource = "resource";
            public const string OutputJson = BuildToolConstants.OutputFileExtensions.TocFileExtension;
            public const string OutputOriginalHtml = ".html.original";

            public const string OutputRelativePath = "relative_path";
            public const string OutputLinkToPath = "link_to_path";
            public const string OutputHash = "hash";
            public const string IsRawPage = "is_raw_page";

            public const string SkipPublish = "skip_publish";
            public const string SkipSchemaCheck = "skip_schema_check";
            public const string SkipNormalization = "skip_normalization";
            public const string IsThemeResource = "is_theme_resource";
            public const string AssetId = "asset_id";
            public const string Monikers = "monikers";
            public const string Version = "version";
            public const string IsIncremental = "is_incremental";
            public const string PdfName = "pdf_name";
        }

        public static class BuildManifest
        {
            public const string SourceBasePath = "source_base_path";
            public const string VersionInfo = "version_info";
            public const string TypeMapping = "type_mapping";
            public const string PublishOnlyMetadata = "publish_only_metadata";
            public const string Files = "files";
            public const string IncrementalInfo = "incremental_info";
        }
    }
}
