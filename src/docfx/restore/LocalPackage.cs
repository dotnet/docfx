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

        public override bool Exists(PathString path) => File.Exists(_directory.Concat(path));

        public override IEnumerable<PathString> GetFiles(Func<string, bool>? fileNamePredicate = null)
        {
            if (!Directory.Exists(_directory))
            {
                throw Errors.Config.DirectoryNotFound(_directory).ToException();
            }
            return PathUtility.GetFilesInDirectory(_directory, fileNamePredicate);
        }

        public override PathString GetFullFilePath(PathString path) => new PathString(_directory.Concat(path));

        public override DateTime GetLastWriteTimeUtc(PathString path)
            => File.GetLastWriteTimeUtc(_directory.Concat(path));

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
