// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class LocalPackage : Package
    {
        private static readonly EnumerationOptions s_enumerationOptions = new EnumerationOptions { RecurseSubdirectories = true };

        private readonly PathString _directory;

        public LocalPackage(string directory = ".")
        {
            _directory = new PathString(Path.GetFullPath(directory));
        }

        public override PathString BasePath => _directory;

        public override bool Exists(PathString path) => File.Exists(_directory.Concat(path));

        public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
        {
            var directoryPath = _directory.Concat(new PathString(directory));
            if (!Directory.Exists(directoryPath))
            {
                throw Errors.Config.DirectoryNotFound(directoryPath).ToException();
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
                    : string.Concat(entry.Directory.Slice(entry.RootDirectory.Length + 1), "/", entry.FileName);

                return PathString.DangerousCreate(result);
            }
        }

        public override PathString GetFullFilePath(PathString path) => new PathString(_directory.Concat(path));

        public override DateTime? TryGetLastWriteTimeUtc(PathString path)
            => Exists(path) ? File.GetLastWriteTimeUtc(_directory.Concat(path)) : default;

        public override byte[] ReadBytes(PathString path) => File.ReadAllBytes(_directory.Concat(path));

        public override Stream ReadStream(PathString path) => File.OpenRead(_directory.Concat(path));

        public override PathString? TryGetGitFilePath(PathString path) => _directory.Concat(path);

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
}
