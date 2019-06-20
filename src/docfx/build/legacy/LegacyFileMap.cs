// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            List<Document> documents,
            Dictionary<string, List<LegacyDependencyMapItem>> dependencyMap,
            Dictionary<Document, PublishItem> fileManifests)
        {
            using (Progress.Start("Convert Legacy File Map"))
            {
                var listBuilder = new ListBuilder<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();

                var monikerRangeConfigs = new Dictionary<Func<string, bool>, string>();
                foreach (var (key, monikerRange) in docset.Config.MonikerRange)
                {
                    monikerRangeConfigs.Add(GlobUtility.CreateGlobMatcher(key), monikerRange);
                }
                monikerRangeConfigs.Reverse();

                Parallel.ForEach(
                    documents,
                    document =>
                    {
                        if (document.IsSchemaData)
                        {
                            return;
                        }
                        fileManifests.TryGetValue(document, out var publishItem);
                        var legacyOutputFilePathRelativeToSiteBasePath = document.ToLegacyOutputPathRelativeToSiteBasePath(docset, publishItem);
                        var legacySiteUrlRelativeToSiteBasePath = document.ToLegacySiteUrlRelativeToSiteBasePath(docset);

                        var monikerRange = GetMonikerRangeConfig(document);
                        var fileItem = LegacyFileMapItem.Instance(legacyOutputFilePathRelativeToSiteBasePath, legacySiteUrlRelativeToSiteBasePath, document.ContentType, monikerRange);
                        if (fileItem != null)
                        {
                            listBuilder.Add((PathUtility.NormalizeFile(document.ToLegacyPathRelativeToBasePath(docset)), fileItem));
                        }
                    });

                var fileMapItems = listBuilder.ToList();
                Convert(docset, context, fileMapItems);
                LegacyAggregatedFileMap.Convert(docset, context, fileMapItems, dependencyMap);

                string GetMonikerRangeConfig(Document file)
                {
                    foreach (var (glob, monikerRange) in monikerRangeConfigs)
                    {
                        if (glob(file.FilePath))
                        {
                            return monikerRange;
                        }
                    }
                    return default;
                }
            }
        }

        public static void Convert(Docset docset, Context context, IEnumerable<(string path, LegacyFileMapItem fileMapItem)> items)
        {
            context.Output.WriteJson(
                new
                {
                    host = docset.HostName,
                    locale = docset.Locale,
                    base_path = $"/{docset.SiteBasePath}",
                    source_base_path = docset.Config.DocumentId.SourceBasePath,
                    version_info = new { },
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
                Path.Combine(docset.SiteBasePath, "filemap.json"));
        }
    }
}
