// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal static class LegacyDependencyMap
    {
        public static void Convert(Docset docset, Context context, List<Document> documemts, DependencyMap dependencyMap, TableOfContentsMap tocMap)
        {
            using (Progress.Start("Convert Legacy Dependency Map"))
            {
                var legacyDependencyMap = new ConcurrentBag<LegacyDependencyMapItem>();

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
                        legacyDependencyMap.Add(new LegacyDependencyMapItem
                        {
                            From = $"~/{document.ToLegacyPathRelativeToBasePath(docset)}",
                            To = $"~/{toc.ToLegacyPathRelativeToBasePath(docset)}",
                            Type = LegacyDependencyMapType.Metadata,
                        });
                    });

                foreach (var (source, dependencies) in dependencyMap)
                {
                    foreach (var dependencyItem in dependencies)
                    {
                        if (source.Equals(dependencyItem.Dest))
                        {
                            continue;
                        }

                        legacyDependencyMap.Add(new LegacyDependencyMapItem
                        {
                            From = $"~/{source.ToLegacyPathRelativeToBasePath(docset)}",
                            To = $"~/{dependencyItem.Dest.ToLegacyPathRelativeToBasePath(docset)}",
                            Type = dependencyItem.Type.ToLegacyDependencyMapType(),
                        });
                    }
                }

                context.WriteJson(legacyDependencyMap, Path.Combine(docset.Config.SiteBasePath, ".dependency-map.json"));
            }
        }

        private static LegacyDependencyMapType ToLegacyDependencyMapType(this DependencyType dependencyType)
        {
            switch (dependencyType)
            {
                case DependencyType.Link:
                    return LegacyDependencyMapType.File;
                case DependencyType.Inclusion:
                    return LegacyDependencyMapType.Include;
                case DependencyType.Bookmark:
                    return LegacyDependencyMapType.Bookmark;
                default:
                    return LegacyDependencyMapType.None;
            }
        }

        private class LegacyDependencyMapItem
        {
            [JsonProperty("from")]
            public string From { get; set; }

            [JsonProperty("to")]
            public string To { get; set; }

            [JsonProperty("type")]
            public LegacyDependencyMapType Type { get; set; }
        }

        private enum LegacyDependencyMapType
        {
            None,
            Uid,
            Include,
            File,
            Overwrite,
            OverwriteFragments,
            Bookmark,
            Metadata,
        }
    }
}
