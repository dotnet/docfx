// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal static class LegacyAggregatedFileMap
    {
        public static void Convert(
            Docset docset,
            Context context,
            IEnumerable<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)> items,
            Dictionary<string, List<LegacyDependencyMapItem>> dependencyMap)
        {
            var aggregatedFileMapItems = new List<(string path, object item)>();

            foreach (var (legacyFilePathRelativeToBaseFolder, fileMapItem) in items)
            {
                if (fileMapItem.Type == "Resource")
                {
                    continue;
                }

                var aggregatedFileMapItem = new
                {
                    dependencies = dependencyMap.ContainsKey(legacyFilePathRelativeToBaseFolder)
                                    ? dependencyMap[legacyFilePathRelativeToBaseFolder].Select(
                                        x => new DependencyItem
                                        {
                                            FromFilePath = x.From,
                                            ToFilePath = x.To,
                                            DependencyType = x.Type,
                                            Version = x.Version,
                                        })
                                    : new List<DependencyItem>(),
                    aggregated_monikers = fileMapItem.Monikers,
                    docset_names = new[] { docset.Config.Name },
                    has_non_moniker_url = fileMapItem.Monikers.Count == 0,
                    type = fileMapItem.Type,
                };

                aggregatedFileMapItems.Add((legacyFilePathRelativeToBaseFolder,aggregatedFileMapItem));
            }

            context.Output.WriteJson(
                new
                {
                    aggregated_file_map_items = aggregatedFileMapItems
                        .OrderBy(item => item.path).ToDictionary(item => item.path, item => item.item),
                    docset_infos = new Dictionary<string, object>
                    {
                        [docset.Config.Name] = new
                        {
                            docset_name = docset.Config.Name,
                            docset_path_to_root = string.Empty,
                        },
                    },
                }, "op_aggregated_file_map_info.json");
        }

        [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
        private class DependencyItem
        {
            public string FromFilePath { get; set; }

            public string ToFilePath { get; set; }

            public string Version { get; set; }

            public LegacyDependencyMapType DependencyType { get; set; }
        }
    }
}
