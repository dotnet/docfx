// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class DirectoryPackage : Package
    {
        private readonly PathString _directory;

        private readonly Package _package;

        public DirectoryPackage(Package package, string directory = ".")
        {
            _directory = new PathString(directory);
            _package = package;
        }

        public override Package CreateSubPackage(string relativePath)
            => _package.CreateSubPackage(_directory.Concat(new PathString(relativePath)));

        public override bool Exists(PathString path) => _package.Exists(_directory.Concat(path));

        public override IEnumerable<PathString> GetFiles(string directory = ".", Func<string, bool>? fileNamePredicate = null)
            => _package.GetFiles(ApplyDirectory(directory), fileNamePredicate);

        public override PathString GetFullFilePath(PathString path) => _package.GetFullFilePath(ApplyDirectory(path));

        public override DateTime GetLastWriteTimeUtc(PathString path) => _package.GetLastWriteTimeUtc(ApplyDirectory(path));

        public override Stream ReadStream(PathString path) => _package.ReadStream(_directory.Concat(path));

        public override PathString? TryGetPhysicalPath(PathString path) => _package.TryGetPhysicalPath(ApplyDirectory(path));

        public override PathString? TryGetGitFilePath(PathString path) => _package.TryGetGitFilePath(ApplyDirectory(path));

        private PathString ApplyDirectory(PathString path) => _directory.Concat(path);

        private PathString ApplyDirectory(string path) => _directory.Concat(new PathString(path));
    }
}
