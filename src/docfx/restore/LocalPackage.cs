// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LocalPackage : Package
    {
        private readonly PathString _directory;

        public LocalPackage(string directory = ".")
        {
            _directory = new PathString(Path.GetFullPath(directory));
        }

        public override PathString BasePath => _directory;

        public override bool Exists(PathString path) => File.Exists(_directory.Concat(path));

        public override IEnumerable<PathString> GetFiles(string directory = ".", Func<string, bool>? fileNamePredicate = null)
        {
            var directoryPath = _directory.Concat(new PathString(directory));
            if (!Directory.Exists(directoryPath))
            {
                throw Errors.Config.DirectoryNotFound(directoryPath).ToException();
            }
            return PathUtility.GetFilesInDirectory(directoryPath, fileNamePredicate);
        }

        public override PathString GetFullFilePath(PathString path) => new PathString(_directory.Concat(path));

        public override DateTime? TryGetLastWriteTimeUtc(PathString path)
            => Exists(path) ? File.GetLastWriteTimeUtc(_directory.Concat(path)) : default;

        public override byte[] ReadAllBytes(PathString path) => File.ReadAllBytes(_directory.Concat(path));

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
