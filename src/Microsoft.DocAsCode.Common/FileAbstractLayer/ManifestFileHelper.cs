// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Plugins;

    public static class ManifestFileHelper
    {
        public static bool AddFile(this Manifest manifest, string sourceFilePath, string extension, string targetRelativePath)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (sourceFilePath == null)
            {
                throw new ArgumentNullException(nameof(sourceFilePath));
            }
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }
            if (targetRelativePath == null)
            {
                throw new ArgumentNullException(nameof(targetRelativePath));
            }
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
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }
            if (targetRelativePath == null)
            {
                throw new ArgumentNullException(nameof(targetRelativePath));
            }
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
            item.OutputFiles[extension] = new OutputFileInfo
            {
                RelativePath = targetRelativePath,
            };
        }

        public static bool RemoveFile(this Manifest manifest, string sourceFilePath, string extension)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (sourceFilePath == null)
            {
                throw new ArgumentNullException(nameof(sourceFilePath));
            }
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }
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
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            lock (manifest)
            {
                return RemoveFileCore(item, extension);
            }
        }

        private static bool RemoveFileCore(ManifestItem item, string extension)
        {
            return item.OutputFiles.Remove(extension);
        }

        public static void Modify(this Manifest manifest, Action<Manifest> action)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (manifest)
            {
                action(manifest);
            }
        }

        public static T Modify<T>(this Manifest manifest, Func<Manifest, T> func)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            lock (manifest)
            {
                return func(manifest);
            }
        }

        public static void Dereference(this Manifest manifest, string manifestFolder, int parallelism)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (manifestFolder == null)
            {
                throw new ArgumentNullException(nameof(manifestFolder));
            }
            FileWriterBase.EnsureFolder(manifestFolder);
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromManifest(manifest, manifestFolder)
                .WriteToRealFileSystem(manifestFolder)
                .Create();
            Parallel.ForEach(
                from f in manifest.Files
                from ofi in f.OutputFiles.Values
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

        public static void Shrink(this Manifest manifest, string incrementalFolder, int parallism = 0)
        {
            lock (manifest)
            {
                Shrink(manifest.Files, incrementalFolder, parallism);
            }
        }

        public static void Shrink(this IEnumerable<ManifestItem> items, string incrementalFolder, int parallism = 0)
        {
            Parallel.ForEach(
                from m in items
                from ofi in m.OutputFiles.Values
                where ofi.Hash != null &&
                    ofi.LinkToPath != null &&
                    ofi.LinkToPath.Length > incrementalFolder.Length &&
                    ofi.LinkToPath.StartsWith(incrementalFolder) &&
                    (ofi.LinkToPath[incrementalFolder.Length] == '\\' || ofi.LinkToPath[incrementalFolder.Length] == '/')
                group ofi by ofi.Hash into g
                select g.ToList(),
                new ParallelOptions { MaxDegreeOfParallelism = parallism > 0 ? parallism : Environment.ProcessorCount },
                list =>
                {
                    var groups = from item in list group item by item.LinkToPath;
                    var file = groups.First().First().LinkToPath;
                    foreach (var g in groups.Skip(1))
                    {
                        File.Delete(Environment.ExpandEnvironmentVariables(g.First().LinkToPath));
                        foreach (var item in g)
                        {
                            item.LinkToPath = file;
                        }
                    }
                });
        }
    }
}
