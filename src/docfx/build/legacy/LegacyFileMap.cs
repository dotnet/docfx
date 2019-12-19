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
            Docset docset,
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
                        var legacyOutputFilePathRelativeToBasePath = document.ToLegacyOutputPathRelativeToBasePath(
                            context, docset, fileManifest.Value);
                        var legacySiteUrlRelativeToBasePath = document.ToLegacySiteUrlRelativeToBasePath(docset);

                        var version = context.MonikerProvider.GetFileLevelMonikerRange(document.FilePath);
                        var fileItem = LegacyFileMapItem.Instance(
                            legacyOutputFilePathRelativeToBasePath,
                            legacySiteUrlRelativeToBasePath,
                            document.ContentType,
                            version,
                            fileManifest.Value.Monikers);
                        if (fileItem != null)
                        {
                            listBuilder.Add((document.FilePath.Path, fileItem));
                        }
                    });

                var fileMapItems = listBuilder.ToList();
                Convert(docset, context, fileMapItems);
                LegacyAggregatedFileMap.Convert(docset, context, fileMapItems, dependencyMap);
            }
        }

        public static void Convert(Docset docset, Context context, IEnumerable<(string path, LegacyFileMapItem fileMapItem)> items)
        {
            context.Output.WriteJson(
                new
                {
                    host = $"https://{docset.Config.HostName}",
                    locale = docset.Locale,
                    base_path = docset.Config.BasePath.Original,
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
                Path.Combine(docset.Config.BasePath, "filemap.json"));
        }
    }
}
