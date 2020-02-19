// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class LegacyFileMap
    {
        public static void Convert(
            Context context,
            Dictionary<string, List<LegacyDependencyMapItem>> dependencyMap,
            Dictionary<Document, PublishItem> fileManifests)
        {
            using (Progress.Start("Convert Legacy File Map"))
            {
                var listBuilder = new ListBuilder<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();

                Parallel.ForEach(
                    fileManifests,
                    fileManifest =>
                    {
                        var document = fileManifest.Key;
                        if (document.ContentType == ContentType.Page && !document.IsPage)
                        {
                            return;
                        }
                        var legacyOutputFilePathRelativeToBasePath = document.ToLegacyOutputPathRelativeToBasePath(context, fileManifest.Value);
                        var legacySiteUrlRelativeToBasePath = document.ToLegacySiteUrlRelativeToBasePath(context);

                        var fileItem = LegacyFileMapItem.Instance(
                            legacyOutputFilePathRelativeToBasePath,
                            legacySiteUrlRelativeToBasePath,
                            document.ContentType,
                            fileManifest.Value.ConfigMonikerRange,
                            fileManifest.Value.Monikers);
                        if (fileItem != null)
                        {
                            listBuilder.Add((document.FilePath.Path, fileItem));
                        }
                    });

                var fileMapItems = listBuilder.ToList();
                Convert(context, fileMapItems);
                LegacyAggregatedFileMap.Convert(context, fileMapItems, dependencyMap);
            }
        }

        public static void Convert(Context context, IEnumerable<(string path, LegacyFileMapItem fileMapItem)> items)
        {
            context.Output.WriteJson(
                new
                {
                    host = $"https://{context.Config.HostName}",
                    locale = context.LocalizationProvider.Locale,
                    base_path = context.Config.BasePath.Original,
                    source_base_path = ".",
                    version_info = new { },
                    from_docfx_v3 = true,
                    file_mapping = items.OrderBy(item => item.path).ToDictionary(
                        (item) =>
                        {
                            if (!string.IsNullOrEmpty(item.fileMapItem.Version))
                            {
                                return $"{item.path}:{item.fileMapItem.Version}";
                            }
                            else
                            {
                                return item.path;
                            }
                        },
                        item => item.fileMapItem),
                },
                Path.Combine(context.Config.BasePath.RelativePath, "filemap.json"));
        }
    }
}
