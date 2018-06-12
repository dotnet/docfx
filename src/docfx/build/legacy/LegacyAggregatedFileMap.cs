// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal static class LegacyAggregatedFileMap
    {
        public static void Convert(Docset docset, Context context, IEnumerable<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)> items)
        {
            var aggregatedFileMapItems = new Dictionary<string, object>();

            foreach (var (legacyFilePathRelativeToBaseFolder, fileMapItem) in items)
            {
                var aggregatedFileMapItem = new
                {
                    aggregated_monikers = Array.Empty<string>(), // todo
                    docset_names = new[] { docset.Name },
                    has_non_moniker_url = true, // todo
                    type = fileMapItem.Type,
                };

                aggregatedFileMapItems.Add(legacyFilePathRelativeToBaseFolder, aggregatedFileMapItem);
            }

            context.WriteJson(new { aggregated_file_map_items = aggregatedFileMapItems }, "op_aggregated_file_map_info.json");
        }
    }
}
