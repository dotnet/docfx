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
    internal static class LegacyDependencyMap
    {
        public static Dictionary<string, List<LegacyDependencyMapItem>> Convert(
            string docsetPath,
            Context context,
            List<Document> documents,
            DependencyMap dependencyMap)
        {
            using (Progress.Start("Convert Legacy Dependency Map"))
            {
                var legacyDependencyMap = new ListBuilder<LegacyDependencyMapItem>();

                // process toc map
                Parallel.ForEach(
                    documents,
                    document =>
                    {
                        if (document.ContentType == ContentType.Resource ||
                            document.ContentType == ContentType.TableOfContents ||
                            document.ContentType == ContentType.Redirection ||
                            document.ContentType == ContentType.Unknown)
                        {
                            return;
                        }
                        var toc = context.TocMap.FindNearestToc(document);
                        if (toc != null)
                        {
                            var monikerRange = context.MonikerProvider.GetConfigMonikerRange(document.FilePath);
                            legacyDependencyMap.Add(new LegacyDependencyMapItem(
                                $"~/{document.FilePath.Path}",
                                $"~/{toc.FilePath.Path}",
                                monikerRange,
                                LegacyDependencyMapType.Metadata));
                        }
                    });

                foreach (var (source, dependencies) in dependencyMap)
                {
                    foreach (var dependencyItem in dependencies)
                    {
                        if (source.Equals(dependencyItem.To))
                        {
                            continue;
                        }

                        var monikerRange = context.MonikerProvider.GetConfigMonikerRange(source.FilePath);
                        legacyDependencyMap.Add(new LegacyDependencyMapItem(
                            $"~/{source.FilePath.Path}",
                            $"~/{dependencyItem.To.FilePath.Path}",
                            monikerRange,
                            dependencyItem.Type.ToLegacyDependencyMapType()));
                    }
                }

                var sorted = (
                    from d in legacyDependencyMap.ToList()
                    orderby d.From, d.To, d.Type
                    select d).ToArray();

                var dependencyList =
                    from dep in sorted
                    select JsonUtility.Serialize(new
                    {
                        dependency_type = dep.Type,
                        from_file_path = Path.GetFullPath(Path.Combine(docsetPath, dep.From.Substring(2))),
                        to_file_path = Path.GetFullPath(Path.Combine(docsetPath, dep.To.Substring(2))),
                        version = dep.Version,
                    });

                var dependencyListText = string.Join('\n', dependencyList);

                context.Output.WriteText("full-dependent-list.txt", dependencyListText);
                context.Output.WriteText("server-side-dependent-list.txt", dependencyListText);

                return sorted.Select(x => new LegacyDependencyMapItem(x.From.Substring(2), x.To.Substring(2), x.Version, x.Type))
                             .GroupBy(x => x.From)
                             .ToDictionary(g => g.Key, g => g.ToList());
            }
        }

        private static LegacyDependencyMapType ToLegacyDependencyMapType(this DependencyType dependencyType)
        {
            switch (dependencyType)
            {
                case DependencyType.Link:
                    return LegacyDependencyMapType.File;
                case DependencyType.Inclusion:
                case DependencyType.TocInclusion:
                    return LegacyDependencyMapType.Include;
                case DependencyType.UidInclusion:
                    return LegacyDependencyMapType.Uid;
                case DependencyType.Bookmark:
                    return LegacyDependencyMapType.Bookmark;
                default:
                    throw new NotSupportedException($"Legacy dependency type not supported: {dependencyType}");
            }
        }
    }
}
