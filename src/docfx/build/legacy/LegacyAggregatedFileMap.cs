// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyAggregatedFileMap
    {
        public static void Convert(Docset docset, Context context, IEnumerable<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)> items)
        {
            var aggregatedFileMapItems = new Dictionary<string, object>();

            foreach (var (legacyFilePathRelativeToBaseFolder, fileMapItem) in items)
            {
                if (fileMapItem.Type == "Resource")
                {
                    continue;
                }

                var aggregatedFileMapItem = new
                {
                    aggregated_monikers = Array.Empty<string>(), // todo
                    docset_names = new[] { docset.Config.Name },
                    has_non_moniker_url = true, // todo
                    type = fileMapItem.Type,
                };

                aggregatedFileMapItems.Add(PathUtility.NormalizeFile(Path.Combine(docset.Config.SourceBasePath, legacyFilePathRelativeToBaseFolder)), aggregatedFileMapItem);
            }

            context.WriteJson(
                new
                {
                    aggregated_file_map_items = aggregatedFileMapItems,
                    docset_infos = new Dictionary<string, object>
                    {
                        [docset.Config.Name] = new
                        {
                            docset_name = docset.Config.Name,
                            docset_path_to_root = docset.Config.SourceBasePath,
                        },
                    },
                }, "op_aggregated_file_map_info.json");
        }
    }
}
