// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Plugins;

namespace Docfx;

/// <summary>
/// Helper class to merge document.
/// </summary>
internal static class RunMerge
{
    /// <summary>
    /// Merge document with specified settings.
    /// </summary>
    public static void Exec(MergeJsonConfig config, string configDirectory)
    {
        foreach (var round in config)
        {
            var baseDirectory = configDirectory ?? Directory.GetCurrentDirectory();
            var intermediateOutputFolder = round.Destination ?? Path.Combine(baseDirectory, "obj");
            EnvironmentContext.SetBaseDirectory(baseDirectory);
            EnvironmentContext.SetOutputDirectory(intermediateOutputFolder);
            MergeDocument(config, baseDirectory, intermediateOutputFolder);
            EnvironmentContext.Clean();
        }
    }

    private static void MergeDocument(MergeJsonConfig config, string baseDirectory, string outputDirectory)
    {
        foreach (var round in config)
        {
            var parameters = ConfigToParameter(round, baseDirectory, outputDirectory);
            if (parameters.Files.Count == 0)
            {
                Logger.LogWarning("No files found, nothing is to be generated");
                continue;
            }
            try
            {
                new MetadataMerger().Merge(parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }
    }

    private static MetadataMergeParameters ConfigToParameter(MergeJsonItemConfig config, string baseDirectory, string outputDirectory) =>
        new()
        {
            OutputBaseDir = outputDirectory,
            Metadata = config.GlobalMetadata?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty,
            FileMetadata = ConvertToFileMetadataItem(baseDirectory, config.FileMetadata),
            TocMetadata = config.TocMetadata?.ToImmutableList() ?? [],
            Files = GetFileCollectionFromFileMapping(
                baseDirectory,
                DocumentType.Article,
                GlobUtility.ExpandFileMapping(baseDirectory, config.Content)),
        };

    private static FileMetadata ConvertToFileMetadataItem(string baseDirectory, Dictionary<string, FileMetadataPairs> fileMetadata)
    {
        if (fileMetadata == null)
        {
            return null;
        }
        var result = new Dictionary<string, ImmutableArray<FileMetadataItem>>();
        foreach (var item in fileMetadata)
        {
            var list = new List<FileMetadataItem>();
            foreach (var pair in item.Value.Items)
            {
                list.Add(new FileMetadataItem(pair.Glob, item.Key, pair.Value));
            }
            result.Add(item.Key, list.ToImmutableArray());
        }

        return new FileMetadata(baseDirectory, result);
    }

    private static FileCollection GetFileCollectionFromFileMapping(string baseDirectory, DocumentType type, FileMapping files)
    {
        var result = new FileCollection(baseDirectory);
        foreach (var mapping in files.Items)
        {
            result.Add(type, mapping.Files, mapping.Src, mapping.Dest);
        }
        return result;
    }
}
