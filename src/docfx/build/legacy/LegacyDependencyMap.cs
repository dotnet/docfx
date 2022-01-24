// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal static class LegacyDependencyMap
{
    public static Dictionary<string, List<LegacyDependencyMapItem>> Convert(
        string docsetPath, LegacyContext context, DependencyMap dependencyMap)
    {
        using (Progress.Start("Convert Legacy Dependency Map"))
        {
            var legacyDependencyMap = new ListBuilder<LegacyDependencyMapItem>();

            Parallel.ForEach(dependencyMap, item =>
            {
                var (source, dependencies) = item;
                foreach (var dependencyItem in dependencies)
                {
                    if (source.Equals(dependencyItem.To))
                    {
                        continue;
                    }

                    legacyDependencyMap.Add(new LegacyDependencyMapItem(
                        source.Path,
                        dependencyItem.To.Path,
                        context.MonikerProvider.GetConfigMonikerRange(source),
                        dependencyItem.Type));
                }
            });

            var sorted = (
                from d in legacyDependencyMap.AsList()
                orderby d.From, d.To, d.Type
                select d).ToArray();

            var dependencyList =
                from dep in sorted
                select JsonUtility.Serialize(new
                {
                    dependency_type = dep.Type,
                    from_file_path = Path.GetFullPath(Path.Combine(docsetPath, dep.From)),
                    to_file_path = Path.GetFullPath(Path.Combine(docsetPath, dep.To)),
                    version = dep.Version,
                });

            // TODO: remove this silly duplicated output
            context.Output.WriteLines(new[] { "full-dependent-list.txt", "server-side-dependent-list.txt" }, dependencyList);

            return sorted.Select(x => new LegacyDependencyMapItem(x.From, x.To, x.Version, x.Type))
                         .GroupBy(x => x.From)
                         .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
