// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public static class ManifestFileHelper
{
    public static bool AddFile(this Manifest manifest, string sourceFilePath, string extension, string targetRelativePath)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(sourceFilePath);
        ArgumentNullException.ThrowIfNull(extension);
        ArgumentNullException.ThrowIfNull(targetRelativePath);

        if (sourceFilePath.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(sourceFilePath));
        }
        if (targetRelativePath.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(extension));
        }

        lock (manifest)
        {
            foreach (var item in manifest.Files)
            {
                if (item.SourceRelativePath == sourceFilePath)
                {
                    AddFileCore(item, extension, targetRelativePath);
                    return true;
                }
            }
        }
        return false;
    }

    public static void AddFile(this Manifest manifest, ManifestItem item, string extension, string targetRelativePath)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(extension);
        ArgumentNullException.ThrowIfNull(targetRelativePath);

        if (targetRelativePath.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(extension));
        }

        lock (manifest)
        {
            AddFileCore(item, extension, targetRelativePath);
        }
    }

    private static void AddFileCore(ManifestItem item, string extension, string targetRelativePath)
    {
        item.Output[extension] = new OutputFileInfo
        {
            RelativePath = targetRelativePath,
        };
    }

    public static bool RemoveFile(this Manifest manifest, string sourceFilePath, string extension)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(sourceFilePath);
        ArgumentNullException.ThrowIfNull(extension);

        if (sourceFilePath.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(sourceFilePath));
        }

        lock (manifest)
        {
            foreach (var item in manifest.Files)
            {
                if (item.SourceRelativePath == sourceFilePath)
                {
                    return RemoveFileCore(item, extension);
                }
            }
        }
        return false;
    }

    public static bool RemoveFile(this Manifest manifest, ManifestItem item, string extension)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(extension);

        lock (manifest)
        {
            return RemoveFileCore(item, extension);
        }
    }

    private static bool RemoveFileCore(ManifestItem item, string extension)
    {
        return item.Output.Remove(extension);
    }

    public static void Modify(this Manifest manifest, Action<Manifest> action)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(action);

        lock (manifest)
        {
            action(manifest);
        }
    }

    public static T Modify<T>(this Manifest manifest, Func<Manifest, T> func)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(func);

        lock (manifest)
        {
            return func(manifest);
        }
    }

    public static void Dereference(this Manifest manifest, string manifestFolder, int parallelism)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifestFolder);

        FileWriterBase.EnsureFolder(manifestFolder);
        var fal = FileAbstractLayerBuilder.Default
            .ReadFromManifest(manifest, manifestFolder)
            .WriteToRealFileSystem(manifestFolder)
            .Create();
        Parallel.ForEach(
            from f in manifest.Files
            from ofi in f.Output.Values
            where ofi.LinkToPath != null
            select ofi,
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            ofi =>
            {
                try
                {
                    fal.Copy(ofi.RelativePath, ofi.RelativePath);
                    ofi.LinkToPath = null;
                }
                catch (PathTooLongException ex)
                {
                    Logger.LogError($"Unable to dereference file '{ofi.RelativePath}': {ex.Message}", file: ofi.RelativePath);
                }
            });
    }
}
