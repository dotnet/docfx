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
        public static Dictionary<string, List<LegacyDependencyMapItem>> Convert(Docset docset, Context context, List<Document> documemts, DependencyMap dependencyMap, TableOfContentsMap tocMap)
        {
            using (Progress.Start("Convert Legacy Dependency Map"))
            {
                var legacyDependencyMap = new ListBuilder<LegacyDependencyMapItem>();

                // process toc map
                Parallel.ForEach(
                    documemts,
                    document =>
                    {
                        if (document.ContentType == ContentType.Resource ||
                            document.ContentType == ContentType.TableOfContents ||
                            document.ContentType == ContentType.Redirection ||
                            document.ContentType == ContentType.Unknown)
                        {
                            return;
                        }
                        var toc = tocMap.GetNearestToc(document);
                        if (toc != null)
                        {
                            legacyDependencyMap.Add(new LegacyDependencyMapItem
                            {
                                From = $"~/{document.ToLegacyPathRelativeToBasePath(docset)}",
                                To = $"~/{toc.ToLegacyPathRelativeToBasePath(docset)}",
                                Type = LegacyDependencyMapType.Metadata,
                            });
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

                        legacyDependencyMap.Add(new LegacyDependencyMapItem
                        {
                            From = $"~/{source.ToLegacyPathRelativeToBasePath(docset)}",
                            To = $"~/{dependencyItem.To.ToLegacyPathRelativeToBasePath(docset)}",
                            Type = dependencyItem.Type.ToLegacyDependencyMapType(),
                        });
                    }
                }

                var sorted = from d in legacyDependencyMap.ToList()
                             orderby d.From, d.To, d.Type
                             select d;
                context.Output.WriteJson(sorted, Path.Combine(docset.SiteBasePath, ".dependency-map.json"));
                return sorted.Select(x => new LegacyDependencyMapItem { From = x.From.Substring(2), To = x.To.Substring(2), Type = x.Type })
                    .GroupBy(x => x.From).ToDictionary(g => g.Key, g => g.ToList());
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
