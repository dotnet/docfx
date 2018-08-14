// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class LegacyManifest
    {
        public static List<(LegacyManifestItem manifestItem, Document doc)> Convert(Docset docset, Context context, List<Document> documents)
        {
            using (Progress.Start("Convert Legacy Manifest"))
            {
                var convertedItems = new ConcurrentBag<(LegacyManifestItem manifestItem, Document doc)>();
                Parallel.ForEach(
                    documents,
                    document =>
                    {
                        var legacyOutputPathRelativeToBaseSitePath = document.ToLegacyOutputPathRelativeToBaseSitePath(docset);
                        var legacySiteUrlRelativeToBaseSitePath = document.ToLegacySiteUrlRelativeToBaseSitePath(docset);

                        var output = new LegacyManifestOutput
                        {
                            MetadataOutput = new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                OutputPathRelativeToSiteBasePath = document.ContentType == ContentType.Resource
                                ? legacyOutputPathRelativeToBaseSitePath + ".mta.json"
                                : Path.ChangeExtension(legacyOutputPathRelativeToBaseSitePath, ".mta.json"),
                            },
                        };

                        if (document.ContentType == ContentType.Resource)
                        {
                            output.ResourceOutput = new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                OutputPathRelativeToSiteBasePath = legacyOutputPathRelativeToBaseSitePath,
                            };
                        }

                        if (document.ContentType == ContentType.TableOfContents)
                        {
                            output.TocOutput = new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                OutputPathRelativeToSiteBasePath = legacyOutputPathRelativeToBaseSitePath,
                            };
                        }

                        if (document.ContentType == ContentType.Markdown ||
                            document.ContentType == ContentType.SchemaDocument ||
                            document.ContentType == ContentType.Redirection)
                        {
                            output.PageOutput = new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                OutputPathRelativeToSiteBasePath = Path.ChangeExtension(legacyOutputPathRelativeToBaseSitePath, ".raw.page.json"),
                            };
                        }

                        var file = new LegacyManifestItem
                        {
                            SiteUrlRelativeToSiteBasePath = legacySiteUrlRelativeToBaseSitePath,
                            FilePath = document.FilePath,
                            FilePathRelativeToSourceBasePath = document.ToLegacyPathRelativeToBasePath(docset),
                            OriginalType = GetOriginalType(document.ContentType),
                            Type = GetType(document.ContentType),
                            Output = output,
                            SkipNormalization = !(document.ContentType == ContentType.Resource),
                        };

                        convertedItems.Add((file, document));
                    });

                context.WriteJson(
                new
                {
                    default_version_info = new
                    {
                        name = string.Empty,
                        version_folder = string.Empty,
                        xref_map = "xrefmap.yml",
                    },
                    files = convertedItems.Select(f => f.manifestItem),
                    is_already_processed = true,
                    source_base_path = docset.Config.SourceBasePath,
                    version_info = new { },

                    // todo: items to publish
                    // todo: type_mapping
                },
                Path.Combine(docset.Config.SiteBasePath, ".manifest.json"));

                return convertedItems.ToList();
            }
        }

        private static string GetOriginalType(ContentType type)
        {
            switch (type)
            {
                case ContentType.Markdown:
                case ContentType.Redirection: // todo: support reference redirection
                    return "Conceptual";
                case ContentType.Resource:
                    return "Resource";
                case ContentType.TableOfContents:
                    return "Toc";
                case ContentType.SchemaDocument:
                    return "Reference";
                default:
                    return string.Empty;
            }
        }

        private static string GetType(ContentType type)
        {
            switch (type)
            {
                case ContentType.Markdown:
                case ContentType.Redirection: // todo: support reference redirection
                    return "Content";
                case ContentType.Resource:
                    return "Resource";
                case ContentType.TableOfContents:
                    return "Toc";
                case ContentType.SchemaDocument:
                    return "Reference";
                default:
                    return string.Empty;
            }
        }
    }
}
