// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal static class LegacyFileMap
{
    public static void Convert(
        LegacyContext context,
        Dictionary<string, List<LegacyDependencyMapItem>> dependencyMap,
        Dictionary<FilePath, PublishItem> fileManifests)
    {
        using (Progress.Start("Convert Legacy File Map"))
        {
            var listBuilder = new ListBuilder<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();
            var filemapBuilder = new ListBuilder<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();

            Parallel.ForEach(
                fileManifests,
                fileManifest =>
                {
                    var document = fileManifest.Key;
                    var contentType = context.DocumentProvider.GetContentType(document);

                    switch (contentType)
                    {
                        case ContentType.Unknown:
                        case ContentType.Page when context.DocumentProvider.GetRenderType(document) == RenderType.Component:
                            return;
                    }

                    var legacyOutputFilePathRelativeToBasePath = document.ToLegacyOutputPathRelativeToBasePath(context, fileManifest.Value);
                    var legacySiteUrlRelativeToBasePath = document.ToLegacySiteUrlRelativeToBasePath(context);

                    var fileItem = LegacyFileMapItem.Instance(
                        legacyOutputFilePathRelativeToBasePath,
                        legacySiteUrlRelativeToBasePath,
                        contentType,
                        fileManifest.Value.ConfigMonikerRange,
                        fileManifest.Value.Monikers);

                    listBuilder.Add((context.SourceMap.GetOriginalFilePath(document)?.Path ?? document.Path, fileItem));
                    filemapBuilder.Add((document.Path, fileItem));
                });

            Convert(context, filemapBuilder.AsList());
            LegacyAggregatedFileMap.Convert(context, listBuilder.AsList(), dependencyMap);
        }
    }

    public static void Convert(LegacyContext context, IEnumerable<(string path, LegacyFileMapItem fileMapItem)> items)
    {
        var fileMapping = new Dictionary<string, LegacyFileMapItem>();
        foreach (var (path, fileMapItem) in items.OrderBy(item => item.path))
        {
            if (!string.IsNullOrEmpty(fileMapItem.Version))
            {
                var key = $"{path}:{fileMapItem.Version}";
                if (!fileMapping.ContainsKey(key))
                {
                    fileMapping.Add(key, fileMapItem);
                }
            }
            else if (fileMapItem.Monikers.HasMonikers)
            {
                fileMapping.Add($"{path}:{fileMapItem.Monikers.MonikerGroup}", fileMapItem);
            }
            else
            {
                fileMapping.Add(path, fileMapItem);
            }
        }
        context.Output.WriteJson(
            Path.Combine(context.Config.BasePath, "filemap.json"),
            new
            {
                host = $"https://{context.Config.HostName}",
                locale = context.BuildOptions.Locale,
                base_path = context.Config.BasePath.ValueWithLeadingSlash,
                source_base_path = ".",
                version_info = new { },
                from_docfx_v3 = true,
                file_mapping = fileMapping,
            });
    }
}
