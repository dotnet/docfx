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
        public static async Task Convert(Docset docset, Context context, List<Document> documents)
        {
            using (Progress.Start("Convert Legacy File Map"))
            {
                var fileMapItems = new ConcurrentBag<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();
                await ParallelUtility.ForEach(
                    documents,
                    document =>
                    {
                        var legacyOutputFilePathRelativeToSiteBasePath = document.ToLegacyOutputPathRelativeToBaseSitePath(docset);

                        var fileItem = LegacyFileMapItem.Instance(legacyOutputFilePathRelativeToSiteBasePath, document.ContentType);
                        if (fileItem != null)
                        {
                            fileMapItems.Add((document.ToLegacyPathRelativeToBasePath(docset), fileItem));
                        }

                        return Task.CompletedTask;
                    },
                    Progress.Update);

                Convert(docset, context, fileMapItems);
                LegacyAggregatedFileMap.Convert(docset, context, fileMapItems);
            }
        }

        public static void Convert(Docset docset, Context context, IEnumerable<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)> items)
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
    }
}
