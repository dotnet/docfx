// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal static class LegacyManifest
{
    public static void Convert(string docsetPath, LegacyContext context, Dictionary<FilePath, PublishItem> fileManifests)
    {
        using (Progress.Start("Convert Legacy Manifest"))
        {
            var itemsToPublish = new List<LegacyItemToPublish>
                {
                    new LegacyItemToPublish { RelativePath = "filemap.json", Type = "filemap" },
                };

            var dictionaryBuilder = new DictionaryBuilder<string, MonikerList>();
            var listBuilder = new ListBuilder<(LegacyManifestItem manifestItem, FilePath doc, MonikerList monikers)>();
            Parallel.ForEach(fileManifests, fileManifest =>
            {
                ConvertDocumentToLegacyManifestItem(docsetPath, context, fileManifest, dictionaryBuilder, listBuilder);
            });

            var monikerGroups = dictionaryBuilder.AsDictionary();
            var convertedItems = listBuilder.AsList();
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
                        name = "",
                        version_folder = "",
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

    private static void ConvertDocumentToLegacyManifestItem(
        string docsetPath,
        LegacyContext context,
        KeyValuePair<FilePath, PublishItem> fileManifest,
        DictionaryBuilder<string, MonikerList> dictionaryBuilder,
        ListBuilder<(LegacyManifestItem manifestItem, FilePath doc, MonikerList monikers)> listBuilder)
    {
        var document = fileManifest.Key;
        var legacyOutputPathRelativeToBasePath = document.ToLegacyOutputPathRelativeToBasePath(context, fileManifest.Value);
        var legacySiteUrlRelativeToBasePath = document.ToLegacySiteUrlRelativeToBasePath(context);
        var contentType = context.DocumentProvider.GetContentType(document);
        var isContentRenderType = context.DocumentProvider.GetRenderType(document) == RenderType.Content;

        var output = new LegacyManifestOutput
        {
            MetadataOutput = (contentType == ContentType.Page && !isContentRenderType) || contentType == ContentType.Resource
            ? null
            : new LegacyManifestOutputItem
            {
                IsRawPage = false,
                RelativePath = contentType == ContentType.Resource
                ? legacyOutputPathRelativeToBasePath + ".mta.json"
                : LegacyUtility.ChangeExtension(legacyOutputPathRelativeToBasePath, ".mta.json"),
            },
        };

        if (contentType == ContentType.Resource)
        {
            var resourceOutput = new LegacyManifestOutputItem
            {
                RelativePath = legacyOutputPathRelativeToBasePath,
                IsRawPage = false,
            };
            if (!context.Config.SelfContained)
            {
                resourceOutput.LinkToPath = Path.GetFullPath(Path.Combine(docsetPath, document.Path));
            }
            output.ResourceOutput = resourceOutput;
        }

        if (contentType == ContentType.Toc)
        {
            output.TocOutput = new LegacyManifestOutputItem
            {
                IsRawPage = false,
                RelativePath = LegacyUtility.ChangeExtension(legacyOutputPathRelativeToBasePath, ".json"),
            };
        }

        if (contentType == ContentType.Page || contentType == ContentType.Redirection)
        {
            if (isContentRenderType)
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

        if (context.Config.OutputType == OutputType.Html)
        {
            output.HtmlOutput = new LegacyManifestOutputItem
            {
                IsRawPage = false,
                RelativePath = LegacyUtility.ChangeExtension(legacyOutputPathRelativeToBasePath, ".html"),
            };
        }

        var file = new LegacyManifestItem
        {
            AssetId = legacySiteUrlRelativeToBasePath,
            Original = fileManifest.Value.SourcePath,
            SourceRelativePath = context.SourceMap.GetOriginalFilePath(document)?.Path ?? document.Path,
            OriginalType = GetOriginalType(contentType, context.DocumentProvider.GetMime(document)),
            Type = GetType(contentType, isContentRenderType),
            Output = output,
            SkipNormalization = !(contentType == ContentType.Resource),
            SkipSchemaCheck = !(contentType == ContentType.Resource),
            Group = fileManifest.Value.MonikerGroup,
            Version = context.MonikerProvider.GetConfigMonikerRange(document),
            IsMonikerRange = true,
        };

        listBuilder.Add((file, document, fileManifest.Value.Monikers));
        if (fileManifest.Value.MonikerGroup != null)
        {
            dictionaryBuilder.TryAdd(fileManifest.Value.MonikerGroup, fileManifest.Value.Monikers);
        }
    }

    private static string GetOriginalType(ContentType type, string? mime) => type switch
    {
        ContentType.Page => mime ?? "Conceptual",
        ContentType.Redirection => "Conceptual", // todo: support reference redirection
        ContentType.Resource => "Resource",
        ContentType.Toc => "Toc",
        _ => "",
    };

    private static string GetType(ContentType type, bool isContentRenderType)
    {
        if (type == ContentType.Page && !isContentRenderType)
        {
            return "Toc";
        }

        return type switch
        {
            ContentType.Page or ContentType.Redirection => "Content",
            ContentType.Resource => "Resource",
            ContentType.Toc => "Toc",
            _ => "",
        };
    }
}
