// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build;

internal class LanguageServerPackage : Package
{
    public override PathString BasePath => _memoryPackage.BasePath;

    private readonly MemoryPackage _memoryPackage;
    private readonly List<Package> _packages = new();
    private DateTime _lastPackageFilesUpdateTime = DateTime.UtcNow;

    public LanguageServerPackage(MemoryPackage memoryPackage, Package fallbackPackage)
    {
        Debug.Assert(memoryPackage.BasePath == fallbackPackage.BasePath);

        _memoryPackage = memoryPackage;
        _packages.Add(memoryPackage);
        _packages.Add(fallbackPackage);
    }

    public void AddOrUpdate(PathString path, string content) => _memoryPackage.AddOrUpdate(path, content);

    public override bool Exists(PathString path) => Watcher.Read(() => _packages.Any(pkg => pkg.Exists(path)));

    public IEnumerable<PathString> GetAllFilesInMemory() => _memoryPackage.GetAllFilesInMemory();

    public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
        => Watcher.Read(
            () => _packages.SelectMany(pkg => pkg.GetFiles(directory, allowedFileNames)),
            () => _lastPackageFilesUpdateTime);

    public override PathString GetFullFilePath(PathString path) => _packages.First().GetFullFilePath(path);

    public override DateTime? TryGetLastWriteTimeUtc(PathString path)
        => Watcher.Read(() =>
        {
            for (var i = 0; i < _packages.Count; i++)
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
        => Watcher.Read(
            () => _packages.First((pkg) => pkg.Exists(path)).ReadBytes(path),
            () => TryGetLastWriteTimeUtc(path));

    public override Stream ReadStream(PathString path)
         => Watcher.Read(
            () => _packages.First((pkg) => pkg.Exists(path)).ReadStream(path),
            () => TryGetLastWriteTimeUtc(path));

    public void RefreshPackageFilesUpdateTime() => _lastPackageFilesUpdateTime = DateTime.UtcNow;

    public void RemoveFile(PathString path) => _memoryPackage.RemoveFile(path);

    public override PathString? TryGetPhysicalPath(PathString path)
        => Watcher.Read(() =>
        {
            for (var i = 0; i < _packages.Count; i++)
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
