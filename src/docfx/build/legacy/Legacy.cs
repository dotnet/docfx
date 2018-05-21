// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal static class Legacy
    {
        public static void ConvertToLegacyModel(Docset docset, Context context, IEnumerable<Document> documents)
        {
            var fileMapItems = new List<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();
            foreach (var document in documents)
            {
                var outputFileName = Path.GetFileName(document.OutputPath);
                var relativeOutputFilePath = document.OutputPath;
                var absoluteOutputFilePath = Path.Combine(docset.Config.Output.Path, relativeOutputFilePath);
                var legacyOutputFilePathRelativeToSiteBasePath = relativeOutputFilePath;
                if (relativeOutputFilePath.StartsWith(docset.Config.SiteBasePath, StringComparison.Ordinal))
                {
                    legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.SiteBasePath, relativeOutputFilePath);
                }

                var fileItem = LegacyFileMapItem.Instance(legacyOutputFilePathRelativeToSiteBasePath, document.ContentType);
                if (fileItem != null)
                {
                    fileMapItems.Add((Path.GetRelativePath(docset.Config.SourceBasePath, document.FilePath), fileItem));
                }

                var content = File.ReadAllText(absoluteOutputFilePath);
                if (document.ContentType == ContentType.TableOfContents)
                {
                    OutputTocModel(docset, context, content, relativeOutputFilePath, legacyOutputFilePathRelativeToSiteBasePath);
                }
            }

            OutputFileMap(docset, context, fileMapItems);
        }

        private static string ChangeExtension(this string path, string extension)
        {
            return path.Substring(0, path.LastIndexOf('.')) + extension;
        }

        private static string RemoveExtension(this string path)
        {
            return path.Substring(0, path.LastIndexOf('.'));
        }

        private static void OutputTocModel(Docset docset, Context context, string content, string relativeOutputFilePath, string legacyOutputFilePathRelativeToSiteBasePath)
        {
            var toc = JsonUtility.Deserialize<LegacyTableOfContentsModel>(content);
            var tocItemWithPath = toc?.Items?.FirstOrDefault();
            if (tocItemWithPath != null)
            {
                tocItemWithPath.PdfAbsolutePath = PathUtility.NormalizeFile($"/{docset.Config.SiteBasePath}/opbuildpdf/{legacyOutputFilePathRelativeToSiteBasePath.ChangeExtension(".pdf")}");
                tocItemWithPath.PdfName = PathUtility.NormalizeFile($"/{Path.GetDirectoryName(legacyOutputFilePathRelativeToSiteBasePath)}.pdf");
            }

            context.WriteJson(toc, relativeOutputFilePath);
        }

        private static void OutputFileMap(Docset docset, Context context, IEnumerable<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)> items)
        {
            context.WriteJson(
                new
                {
                    locale = docset.Config.Locale,
                    base_path = $"/{docset.Config.SiteBasePath}",
                    source_base_path = docset.Config.SourceBasePath,
                    version_info = new { },
                    file_mapping = items.ToDictionary(
                        key => PathUtility.NormalizeFile(key.legacyFilePathRelativeToBaseFolder), v => v.fileMapItem),
                },
                Path.Combine(docset.Config.SiteBasePath, "filemap.json"));
        }

        private class LegacyTableOfContentsItem : TableOfContentsItem
        {
            [JsonProperty(PropertyName = "pdf_absolute_path")]
            public string PdfAbsolutePath { get; set; }

            [JsonProperty(PropertyName = "pdf_name")]
            public string PdfName { get; set; }
        }

        private class LegacyTableOfContentsModel
        {
            [JsonProperty(PropertyName = "items")]
            public List<LegacyTableOfContentsItem> Items { get; set; }
        }

        private class LegacyFileMapItem
        {
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "output_relative_path")]
            public string OutputRelativePath { get; set; }

            [JsonProperty(PropertyName = "asset_id")]
            public string AssetId { get; set; }

            public LegacyFileMapItem(string legacyOutputFilePathRelativeToSiteBasePath, ContentType contentType)
            {
                switch (contentType)
                {
                    case ContentType.Markdown:
                    case ContentType.SchemaDocument:
                        Type = "Content";
                        OutputRelativePath = PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath.ChangeExtension(".html"));
                        AssetId = PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath.RemoveExtension());
                        break;
                    case ContentType.Asset:
                        Type = "Resource";
                        OutputRelativePath = AssetId = PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath);
                        break;
                    case ContentType.TableOfContents:
                    default:
                        throw new NotSupportedException($"{contentType} is not supported");
                }
            }

            public static LegacyFileMapItem Instance(string legacyOutputFilePathRelativeToSiteBasePath, ContentType contentType)
            {
                if (contentType == ContentType.TableOfContents || contentType == ContentType.Unknown)
                {
                    return null;
                }

                return new LegacyFileMapItem(legacyOutputFilePathRelativeToSiteBasePath, contentType);
            }
        }
    }
}
