// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Enumeration;

namespace Microsoft.Docs.Build;

internal class LocalPackage : Package
{
    private static readonly EnumerationOptions s_enumerationOptions = new() { RecurseSubdirectories = true };

    private readonly PathString _directory;

    public LocalPackage(string directory = ".") => _directory = new(Path.GetFullPath(directory));

    public override PathString BasePath => _directory;

    public override bool Exists(PathString path) => File.Exists(_directory.Concat(path));

    public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
    {
        var directoryPath = _directory.Concat(directory);
        if (!Directory.Exists(directoryPath))
        {
            return new List<PathString>();
        }

        return new FileSystemEnumerable<PathString>(
            directoryPath,
            ToRelativePathString,
            s_enumerationOptions)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                if (entry.IsDirectory || entry.FileName[0] == '.')
                {
                    return false;
                }
                if (allowedFileNames != null)
                {
                    for (var i = 0; i < allowedFileNames.Length; i++)
                    {
                        if (entry.FileName.Equals(allowedFileNames[i], StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            },
            ShouldRecursePredicate =
                    (ref FileSystemEntry entry) => entry.FileName[0] != '.' && !entry.FileName.Equals("_site", StringComparison.OrdinalIgnoreCase),
        };

        static PathString ToRelativePathString(ref FileSystemEntry entry)
        {
            var result = entry.RootDirectory.Length == entry.Directory.Length
                ? entry.FileName.ToString()
                : string.Concat(entry.Directory[(entry.RootDirectory.Length + 1)..], "/", entry.FileName);

            return PathString.DangerousCreate(result);
        }
    }

    public override PathString GetFullFilePath(PathString path) => new(_directory.Concat(path));

    public override DateTime? TryGetLastWriteTimeUtc(PathString path)
    {
        if (Exists(path))
        {
            return File.GetLastWriteTimeUtc(_directory.Concat(path));
        }
        return null;
    }

    public override byte[] ReadBytes(PathString path) => File.ReadAllBytes(_directory.Concat(path));

    public override Stream ReadStream(PathString path) => File.OpenRead(_directory.Concat(path));

    public override PathString? TryGetPhysicalPath(PathString path)
    {
        var fullPath = _directory.Concat(path);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        return null;
    }
}
