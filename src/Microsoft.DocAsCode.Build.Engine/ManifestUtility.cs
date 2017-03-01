// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public static class ManifestUtility
    {
        public static void RemoveDuplicateOutputFiles(ManifestItemCollection manifestItems)
        {
            if (manifestItems == null)
            {
                throw new ArgumentNullException(nameof(manifestItems));
            }

            var itemsToRemove = new HashSet<string>();
            foreach (var duplicates in (from m in manifestItems
                                        from output in m.OutputFiles.Values
                                        let relativePath = output?.RelativePath
                                        select new { item = m, relativePath })
                              .GroupBy(obj => obj.relativePath, FilePathComparer.OSPlatformSensitiveStringComparer)
                              .Where(g => g.Count() > 1))
            {
                Logger.LogWarning($"Overwrite occurs while input files \"{string.Join(", ", duplicates.Select(duplicate => duplicate.item.SourceRelativePath))}\" writing to the same output file \"{duplicates.Key}\"");
                itemsToRemove.UnionWith(duplicates.Skip(1).Select(duplicate => duplicate.item.SourceRelativePath));
            }
            manifestItems.RemoveAll(m => itemsToRemove.Contains(m.SourceRelativePath));
        }

        public static Manifest MergeManifest(List<Manifest> manifests)
        {
            if (manifests == null)
            {
                throw new ArgumentNullException(nameof(manifests));
            }

            var xrefMaps = (from manifest in manifests
                            where manifest.XRefMap != null
                            select manifest.XRefMap).ToList();
            var incrementalInfos = (from manifest in manifests
                                    from i in manifest.IncrementalInfo ?? Enumerable.Empty<IncrementalInfo>()
                                    select i).ToList();
            return new Manifest(
                (from manifest in manifests
                 from file in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                 select file).Distinct())
            {
                Homepages = (from manifest in manifests
                             from homepage in manifest.Homepages ?? Enumerable.Empty<HomepageInfo>()
                             select homepage).Distinct().ToList(),
                XRefMap = xrefMaps.Count <= 1 ? xrefMaps.FirstOrDefault() : xrefMaps,
                SourceBasePath = manifests.FirstOrDefault()?.SourceBasePath,
                IncrementalInfo = incrementalInfos.Count > 0 ? incrementalInfos : null,
                VersionInfo = manifests.SelectMany(m => m.VersionInfo).ToDictionary(p => p.Key, p => p.Value)
            };
        }
    }
}
