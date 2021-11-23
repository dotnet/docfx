// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

internal static class LegacyAggregatedFileMap
{
    public static void Convert(
        LegacyContext context,
        IEnumerable<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)> items,
        Dictionary<string, List<LegacyDependencyMapItem>> dependencyMap)
    {
        var aggregatedFileMapItems = new List<(string path, object item)>();

        foreach (var (path, (fileMapItem, monikers))
            in items.GroupBy(x => x.legacyFilePathRelativeToBaseFolder)
                    .ToDictionary(
                      g => g.Key,
                      g => (g.First().fileMapItem, g.SelectMany(x => x.fileMapItem.Monikers).Distinct().OrderBy(_ => _, StringComparer.Ordinal).ToList())))
        {
            if (fileMapItem.Type == LegacyItemType.Resource)
            {
                continue;
            }

            var aggregatedFileMapItem = new
            {
                dependencies = dependencyMap.ContainsKey(path)
                                ? dependencyMap[path].Select(
                                    x => new DependencyItem
                                    {
                                        FromFilePath = x.From,
                                        ToFilePath = x.To,
                                        DependencyType = x.Type,
                                        Version = x.Version,
                                    })
                                : new List<DependencyItem>(),
                aggregated_monikers = monikers,
                docset_names = new[] { context.Config.Name },
                has_non_moniker_url = fileMapItem.Monikers.Count == 0,
                type = fileMapItem.Type,
            };

            aggregatedFileMapItems.Add((path, aggregatedFileMapItem));
        }

        context.Output.WriteJson(
            "op_aggregated_file_map_info.json",
            new
            {
                aggregated_file_map_items = aggregatedFileMapItems
                .OrderBy(item => item.path).ToDictionary(item => item.path, item => item.item),
                docset_infos = new Dictionary<string, object>
                {
                    [context.Config.Name] = new
                    {
                        docset_name = context.Config.Name,
                        docset_path_to_root = "",
                    },
                },
            });
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    private class DependencyItem
    {
        public string? FromFilePath { get; set; }

        public string? ToFilePath { get; set; }

        public string? Version { get; set; }

        public DependencyType DependencyType { get; set; }
    }
}
