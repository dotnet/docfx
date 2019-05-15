// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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

            var manifestItemGroups = (from item in manifestItems
                                      from output in item.OutputFiles.Values
                                      let relativePath = output?.RelativePath
                                      select new { item, relativePath }).GroupBy(obj => obj.relativePath, FilePathComparer.OSPlatformSensitiveStringComparer);

            foreach (var manifestItemGroup in manifestItemGroups)
            {
                if (manifestItemGroup.Count() > 1)
                {
                    // TODO: plan to change this warning to error, add error code to analyze the impact.
                    Logger.LogWarning(
                        $"Multiple input files would generate to the same output path overwriting each other. Please rename at least {manifestItemGroup.Count() - 1} of following input files to ensure that there will be only one file to generate to the output path: \"{string.Join(", ", manifestItemGroup.Select(duplicate => duplicate.item.SourceRelativePath))}\".",
                        code: WarningCodes.Build.DuplicateOutputFiles);

                    foreach (var itemToRemove in manifestItemGroup.Skip(1))
                    {
                        manifestItems.Remove(itemToRemove.item);
                    }
                }
            }
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
            var manifestGroupInfos = (from manifest in manifests
                                      from g in manifest.Groups ?? Enumerable.Empty<ManifestGroupInfo>()
                                      select g).ToList();
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
                Groups = manifestGroupInfos.Count > 0 ? manifestGroupInfos : null,
                VersionInfo = manifests.Where(m => m.VersionInfo != null).SelectMany(m => m.VersionInfo).ToDictionary(p => p.Key, p => p.Value)
            };
        }

        public static void ApplyLogCodes(ManifestItemCollection manifestItems, ConcurrentDictionary<string, ImmutableHashSet<string>> codes)
        {
            if (manifestItems == null)
            {
                throw new ArgumentException(nameof(manifestItems));
            }
            if (codes == null)
            {
                throw new ArgumentException(nameof(codes));
            }
            foreach (var item in manifestItems)
            {
                if (codes.TryGetValue(item.SourceRelativePath, out var value))
                {
                    item.LogCodes = value;
                }
            }
        }
    }
}
