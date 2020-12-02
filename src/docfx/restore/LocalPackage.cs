// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LocalPackage : Package
    {
        private static readonly EnumerationOptions s_enumerationOptions = new EnumerationOptions { RecurseSubdirectories = true };
        private static int s_v;

        private readonly PathString _directory;

        public LocalPackage(string directory = ".") => _directory = new PathString(Path.GetFullPath(directory));

        public override bool Exists(PathString path) => Watcher.Watch(() => File.Exists(Path.Combine(_directory, path)));

        public override Stream ReadStream(PathString path)
        {
            var fullpath = Path.Combine(_directory, path);

            Watcher.Watch(() => fullpath.EndsWith("troubleshoot-guide.md") ? s_v++ : 0);

            return File.OpenRead(fullpath);
        }

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

        public override DateTime GetLastWriteTimeUtc(PathString path)
            => File.GetLastWriteTimeUtc(_directory.Concat(path));

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
