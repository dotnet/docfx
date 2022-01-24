// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class SubPackage : Package
{
    private readonly PathString _directory;

    private readonly Package _package;

    public SubPackage(Package package, string directory = ".")
    {
        _directory = new(directory);
        _package = package;
    }

    public override PathString BasePath => _package.BasePath.Concat(_directory);

    public override bool Exists(PathString path) => _package.Exists(_directory.Concat(path));

    public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
        => _package.GetFiles(ApplyDirectory(directory), allowedFileNames);

    public override PathString GetFullFilePath(PathString path) => _package.GetFullFilePath(ApplyDirectory(path));

    public override DateTime? TryGetLastWriteTimeUtc(PathString path) => _package.TryGetLastWriteTimeUtc(ApplyDirectory(path));

    public override byte[] ReadBytes(PathString path) => _package.ReadBytes(_directory.Concat(path));

    public override Stream ReadStream(PathString path) => _package.ReadStream(_directory.Concat(path));

    public override PathString? TryGetPhysicalPath(PathString path) => _package.TryGetPhysicalPath(ApplyDirectory(path));

    private PathString ApplyDirectory(PathString path) => _directory.Concat(path);
}
