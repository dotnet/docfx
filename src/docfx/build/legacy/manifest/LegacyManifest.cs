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
                    new LegacyItemToPublish { RelativePath = ".dependency-map.json", Type = "unknown" },
                };

                var dictionaryBuilder = new DictionaryBuilder<string, List<string>>();
                var listBuilder = new ListBuilder<(LegacyManifestItem manifestItem, Document doc, List<string> monikers)>();
                Parallel.ForEach(
                    fileManifests,
                    fileManifest =>
                    {
                        var document = fileManifest.Key;
                        var legacyOutputPathRelativeToBaseSitePath = document.ToLegacyOutputPathRelativeToBaseSitePath(docset, fileManifest.Value.Path);
                        var legacySiteUrlRelativeToBaseSitePath = document.ToLegacySiteUrlRelativeToBaseSitePath(docset);

                        var output = new LegacyManifestOutput
                        {
                            MetadataOutput = document.IsSchemaData || document.ContentType == ContentType.Resource
                            ? null
                            : new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                RelativePath = document.ContentType == ContentType.Resource
                                ? legacyOutputPathRelativeToBaseSitePath + ".mta.json"
                                : Path.ChangeExtension(legacyOutputPathRelativeToBaseSitePath, ".mta.json"),
                            },
                        };

                        if (document.ContentType == ContentType.Resource)
                        {
                            var resourceOutput = new LegacyManifestOutputItem
                            {
                                RelativePath = legacyOutputPathRelativeToBaseSitePath,
                                IsRawPage = false,
                            };
                            if (!docset.Config.Output.CopyResources)
                            {
                                resourceOutput.LinkToPath = Path.GetFullPath(Path.Combine(docset.DocsetPath, document.FilePath));
                            }
                            output.ResourceOutput = resourceOutput;
                        }

                        if (document.ContentType == ContentType.TableOfContents)
                        {
                            output.TocOutput = new LegacyManifestOutputItem
                            {
                                IsRawPage = false,
                                RelativePath = legacyOutputPathRelativeToBaseSitePath,
                            };
                        }

                        if (document.ContentType == ContentType.Page ||
                            document.ContentType == ContentType.Redirection)
                        {
                            if (document.IsSchemaData)
                            {
                                output.TocOutput = new LegacyManifestOutputItem
                                {
                                    IsRawPage = false,
                                    RelativePath = legacyOutputPathRelativeToBaseSitePath,
                                };
                            }
                            else
                            {
                                output.PageOutput = new LegacyManifestOutputItem
                                {
                                    IsRawPage = false,
                                    RelativePath = Path.ChangeExtension(legacyOutputPathRelativeToBaseSitePath, ".raw.page.json"),
                                };
                            }
                        }

                        string groupId = null;
                        if (fileManifest.Value.Monikers.Count > 0)
                        {
                            groupId = HashUtility.GetMd5HashShort(fileManifest.Value.Monikers);
                        }
                        var file = new LegacyManifestItem
                        {
                            AssetId = legacySiteUrlRelativeToBaseSitePath,
                            Original = document.FilePath,
                            SourceRelativePath = document.ToLegacyPathRelativeToBasePath(docset),
                            OriginalType = GetOriginalType(document.ContentType),
                            Type = GetType(document.ContentType, document.Schema),
                            Output = output,
                            SkipNormalization = !(document.ContentType == ContentType.Resource),
                            SkipSchemaCheck = !(document.ContentType == ContentType.Resource),
                            Group = groupId,
                        };

                        listBuilder.Add((file, document, fileManifest.Value.Monikers));
                        if (groupId != null)
                        {
                            dictionaryBuilder.TryAdd(groupId, fileManifest.Value.Monikers);
                        }
                    });

                var monikerGroups = dictionaryBuilder.ToDictionary();
                var convertedItems = listBuilder.ToList();
                context.Output.WriteJson(
                new
                {
                    groups = monikerGroups.Any() ? monikerGroups.Select(item => new
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
                    files = convertedItems.OrderBy(f => f.manifestItem.AssetId).Select(f => f.manifestItem),
                    is_already_processed = true,
                    source_base_path = docset.Config.DocumentId.SourceBasePath,
                    version_info = new { },
                    items_to_publish = itemsToPublish,
                },
                Path.Combine(docset.SiteBasePath, ".manifest.json"));
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

        private static string GetType(ContentType type, Schema schema)
        {
            if (type == ContentType.Page && schema?.Type == typeof(ContextObject))
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
