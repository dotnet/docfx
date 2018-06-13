// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyDependencyMap
    {
        public static void Convert(Docset docset, Context context, List<Document> documemts, DependencyMap dependencyMap, TableOfContentsMap tocMap)
        {
            var legacyDependencyMap = new List<LegacyDependencyMapItem>();

            // process toc map
            foreach (var document in documemts)
            {
                if (document.ContentType == ContentType.Asset ||
                    document.ContentType == ContentType.TableOfContents ||
                    document.ContentType == ContentType.Unknown)
                {
                    continue;
                }
                var toc = tocMap.GetNearestToc(document);
                legacyDependencyMap.Add(new LegacyDependencyMapItem
                {
                    From = $"~/{document.ToLegacyPathRelativeToBasePath(docset)}",
                    To = $"~/{toc.ToLegacyPathRelativeToBasePath(docset)}",
                    Type = LegacyDependencyMapType.Metadata,
                });
            }

            foreach (var (source, dependencies) in dependencyMap)
            {
                foreach (var dependencyItem in dependencies)
                {
                    if(source.Equals(dependencyItem.Dest))
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
}
