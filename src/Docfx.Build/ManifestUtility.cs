// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

#pragma warning disable CS0612 // Type or member is obsolete

static class ManifestUtility
{
    public static void RemoveDuplicateOutputFiles(List<ManifestItem> manifestItems)
    {
        ArgumentNullException.ThrowIfNull(manifestItems);

        var manifestItemGroups = (from item in manifestItems
                                  from output in item.Output.Values
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
        ArgumentNullException.ThrowIfNull(manifests);

        var xrefMaps = (from manifest in manifests
                        where manifest.Xrefmap != null
                        select manifest.Xrefmap).ToList();
        var manifestGroupInfos = (from manifest in manifests
                                  from g in manifest.Groups ?? Enumerable.Empty<ManifestGroupInfo>()
                                  select g).ToList();
        return new Manifest(
            (from manifest in manifests
             from file in manifest.Files ?? Enumerable.Empty<ManifestItem>()
             select file).Distinct())
        {
            Xrefmap = xrefMaps.Count <= 1 ? xrefMaps.FirstOrDefault() : xrefMaps,
            SourceBasePath = manifests.FirstOrDefault()?.SourceBasePath,
            Groups = manifestGroupInfos.Count > 0 ? manifestGroupInfos : null,
        };
    }
}
