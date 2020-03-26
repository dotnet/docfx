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
        public static void Convert(Docset docset, Context context, Dictionary<Document, PublishItem> fileManifests)
        {
            using (Progress.Start("Convert Legacy Manifest"))
            {
                var itemsToPublish = new List<LegacyItemToPublish>
                {
                    new LegacyItemToPublish { RelativePath = "filemap.json", Type = "filemap" },
                };

                var dictionaryBuilder = new DictionaryBuilder<string, IReadOnlyList<string>>();
                var listBuilder = new ListBuilder<(LegacyManifestItem manifestItem, Document doc, IReadOnlyList<string> monikers)>();
                Parallel.ForEach(fileManifests, fileManifest =>
                    {
                        var document = fileManifest.Key;
                        var legacyOutputPathRelativeToBasePath = document.ToLegacyOutputPathRelativeToBasePath(context, fileManifest.Value);
                        var legacySiteUrlRelativeToBasePath = document.ToLegacySiteUrlRelativeToBasePath(context);

                        var output = new LegacyManifestOutput
                        {
                            MetadataOutput = (document.ContentType == ContentType.Page && !document.IsPage) || document.ContentType == ContentType.Resource
                            ? null
                            : new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                RelativePath = document.ContentType == ContentType.Resource
                                ? legacyOutputPathRelativeToBasePath + ".mta.json"
                                : LegacyUtility.ChangeExtension(legacyOutputPathRelativeToBasePath, ".mta.json"),
                            },
                        };

                        if (document.ContentType == ContentType.Resource)
                        {
                            var resourceOutput = new LegacyManifestOutputItem
                            {
                                RelativePath = legacyOutputPathRelativeToBasePath,
                                IsRawPage = false,
                            };
                            if (!context.Config.CopyResources)
                            {
                                resourceOutput.LinkToPath = Path.GetFullPath(Path.Combine(docset.DocsetPath, document.FilePath.Path));
                            }
                            output.ResourceOutput = resourceOutput;
                        }

                        if (document.ContentType == ContentType.TableOfContents)
                        {
                            output.TocOutput = new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                RelativePath = legacyOutputPathRelativeToBasePath,
                            };
                        }

                        if (document.ContentType == ContentType.Page || document.ContentType == ContentType.Redirection)
                        {
                            if (document.IsPage)
                            {
                                output.PageOutput = new LegacyManifestOutputItem
                                {
                                    IsRawPage = false,
                                    RelativePath = LegacyUtility.ChangeExtension(legacyOutputPathRelativeToBasePath, ".raw.page.json"),
                                };
                            }
                            else
                            {
                                output.TocOutput = new LegacyManifestOutputItem
                                {
                                    IsRawPage = false,
                                    RelativePath = legacyOutputPathRelativeToBasePath,
                                };
                            }
                        }

                        var file = new LegacyManifestItem
                        {
                            AssetId = legacySiteUrlRelativeToBasePath,
                            Original = fileManifest.Value.SourcePath,
                            SourceRelativePath = document.FilePath.Path,
                            OriginalType = GetOriginalType(document.ContentType),
                            Type = GetType(document.ContentType, document),
                            Output = output,
                            SkipNormalization = !(document.ContentType == ContentType.Resource),
                            SkipSchemaCheck = !(document.ContentType == ContentType.Resource),
                            Group = fileManifest.Value.MonikerGroup,
                        };

                        listBuilder.Add((file, document, fileManifest.Value.Monikers));
                        if (fileManifest.Value.MonikerGroup != null)
                        {
                            dictionaryBuilder.TryAdd(fileManifest.Value.MonikerGroup, fileManifest.Value.Monikers);
                        }
                    });

                var monikerGroups = dictionaryBuilder.ToDictionary();
                var convertedItems = listBuilder.ToList();
                context.Output.WriteJson(
                    Path.Combine(context.Config.BasePath, ".manifest.json"),
                    new
                    {
                        groups = monikerGroups.Any() ? monikerGroups.OrderBy(item => item.Key).Select(item => new
                        {
                            group = item.Key,
                            monikers = item.Value,
                        }) : null,
                        default_version_info = new
                        {
                            name = string.Empty,
                            version_folder = string.Empty,
                            xref_map = "xrefmap.yml",
                        },
                        files = convertedItems
                            .OrderBy(f => f.manifestItem.AssetId + f.manifestItem.SourceRelativePath).Select(f => f.manifestItem),
                        is_already_processed = true,
                        source_base_path = ".",
                        version_info = new { },
                        items_to_publish = itemsToPublish,
                    });
            }
        }

        private static string GetOriginalType(ContentType type)
        {
            switch (type)
            {
                case ContentType.Page:
                case ContentType.Redirection: // todo: support reference redirection
                    return "Conceptual";
                case ContentType.Resource:
                    return "Resource";
                case ContentType.TableOfContents:
                    return "Toc";
                default:
                    return string.Empty;
            }
        }

        private static string GetType(ContentType type, Document doc)
        {
            if (type == ContentType.Page && !doc.IsPage)
            {
                return "Toc";
            }

            switch (type)
            {
                case ContentType.Page:
                case ContentType.Redirection: // todo: support reference redirection
                    return "Content";
                case ContentType.Resource:
                    return "Resource";
                case ContentType.TableOfContents:
                    return "Toc";
                default:
                    return string.Empty;
            }
        }
    }
}
