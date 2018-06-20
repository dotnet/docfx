// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyManifest
    {
        public static List<(LegacyManifestItem manifestItem, Document doc)> Convert(Docset docset, Context context, List<Document> documents)
        {
            var convertedItems = new List<(LegacyManifestItem manifestItem, Document doc)>();
            foreach (var document in documents)
            {
                var legacyOutputPathRelativeToBaseSitePath = document.ToLegacyOutputPathRelativeToBaseSitePath(docset);
                var legacySitePathRelativeToBaseSitePath = document.ToLegacySiteUrlRelativeToBaseSitePath(docset);

                var output = new LegacyManifestOutput
                {
                    MetadataOutput = new LegacyManifestOutputItem
                    {
                        IsRawPage = false,
                        OutputPathRelativeToSiteBasePath = document.ContentType == ContentType.Asset
                        ? legacyOutputPathRelativeToBaseSitePath + ".mta.json"
                        : Path.ChangeExtension(legacyOutputPathRelativeToBaseSitePath, ".mta.json"),
                    },
                };

                if (document.ContentType == ContentType.Asset)
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
                    SiteUrlRelativeToSiteBasePath = legacySitePathRelativeToBaseSitePath,
                    FilePath = document.FilePath,
                    FilePathRelativeToSourceBasePath = document.ToLegacyPathRelativeToBasePath(docset),
                    OriginalType = GetOriginalType(document.ContentType),
                    Type = GetType(document.ContentType),
                    Output = output,
                    SkipNormalization = !(document.ContentType == ContentType.Asset),
                };

                convertedItems.Add((file, document));
            }

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

            return convertedItems;
        }

        private static string GetOriginalType(ContentType type)
        {
            switch (type)
            {
                case ContentType.Markdown:
                case ContentType.Redirection: // todo: support reference redirection
                    return "Conceptual";
                case ContentType.Asset:
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
                case ContentType.Asset:
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
