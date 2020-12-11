// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerPackage : Package
    {
        public override PathString BasePath => _packages.First().BasePath;

        private readonly List<Package> _packages = new List<Package>();
        private DateTime _lastPackageFilesUpdateTime;

        private MemoryPackage MemoryPackage => (_packages[0] as MemoryPackage)!;

        public LanguageServerPackage(MemoryPackage memoryPackage, Package fallbackPackage)
        {
            _packages.Add(memoryPackage);
            _packages.Add(fallbackPackage);
            Debug.Assert(_packages.All(pkg => pkg.BasePath == _packages[0].BasePath));

            _lastPackageFilesUpdateTime = DateTime.UtcNow;
        }

        public void AddOrUpdate(PathString path, string content) => MemoryPackage.AddOrUpdate(path, content);

        public override bool Exists(PathString path) => Watcher.Watch(() => _packages.Any(pkg => pkg.Exists(path)));

        public IEnumerable<PathString> GetAllFilesInMemory() => MemoryPackage.GetAllFilesInMemory();

        public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
            => Watcher.Watch(
                () => _packages.SelectMany(pkg => pkg.GetFiles(directory, allowedFileNames)),
                () => _lastPackageFilesUpdateTime);

        public override PathString GetFullFilePath(PathString path) => _packages.First().GetFullFilePath(path);

        public override DateTime? TryGetLastWriteTimeUtc(PathString path)
            => Watcher.Watch(() =>
            {
                for (int i = 0; i < _packages.Count; i++)
                {
                    var lastWriteTimeUtc = _packages[i].TryGetLastWriteTimeUtc(path);
                    if (lastWriteTimeUtc != null)
                    {
                        return lastWriteTimeUtc;
                    }
                }

                return null;
            });

        public override byte[] ReadBytes(PathString path)
            => Watcher.Watch(
                () => _packages.First((pkg) => pkg.Exists(path)).ReadBytes(path),
                () => TryGetLastWriteTimeUtc(path));

        public override Stream ReadStream(PathString path)
             => Watcher.Watch(
                () => _packages.First((pkg) => pkg.Exists(path)).ReadStream(path),
                () => TryGetLastWriteTimeUtc(path));

        public void RefreshPackageFilesUpdateTime() => _lastPackageFilesUpdateTime = DateTime.UtcNow;

        public void RemoveFile(PathString path) => MemoryPackage.RemoveFile(path);

        public override PathString? TryGetPhysicalPath(PathString path)
            => Watcher.Watch(() =>
            {
                for (int i = 0; i < _packages.Count; i++)
                {
                    var physicalPath = _packages[i].TryGetPhysicalPath(path);
                    if (physicalPath != null)
                    {
                        return physicalPath;
                    }
                }

                return null;
            });
    }
}
