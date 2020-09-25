// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;

namespace Microsoft.Docs.Build
{
    internal class LocalPackage : Package
    {
        private static readonly EnumerationOptions s_enumerationOptions = new EnumerationOptions { RecurseSubdirectories = true };

        private readonly string _directory;

        public LocalPackage(string directory = ".") => _directory = Path.GetFullPath(directory);

        public override bool Exists(PathString path) => File.Exists(Path.Combine(_directory, path));

        public override Stream ReadStream(PathString path) => File.OpenRead(Path.Combine(_directory, path));

        public override IEnumerable<PathString> GetFiles()
        {
            if (!Directory.Exists(_directory))
            {
                throw Errors.Config.DirectoryNotFound(_directory).ToException();
            }

            return new FileSystemEnumerable<PathString>(_directory, ToPathString, s_enumerationOptions)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName[0] != '.',
                ShouldRecursePredicate =
                 (ref FileSystemEntry entry) => entry.FileName[0] != '.' && !entry.FileName.Equals("_site", StringComparison.OrdinalIgnoreCase),
            };

            static PathString ToPathString(ref FileSystemEntry entry)
            {
                var result = entry.RootDirectory.Length == entry.Directory.Length
                    ? entry.FileName.ToString()
                    : string.Concat(entry.Directory.Slice(entry.RootDirectory.Length + 1), "/", entry.FileName);

                return PathString.DangerousCreate(result);
            }
        }

        public override PathString? TryGetPhysicalPath(PathString path)
        {
            var fullPath = Path.Combine(_directory, path);
            if (File.Exists(fullPath))
            {
                return new PathString(fullPath);
            }

            return null;
        }

        public override PathString? TryGetGitFilePath(PathString path)
        {
            return new PathString(Path.Combine(_directory, path));
        }
    }
}
