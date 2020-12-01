// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LocalPackage : DirectoryPackage
    {
        public LocalPackage(string directory = ".")
            : base(directory) { }

        public override DirectoryPackage CreateSubPackage(string relativePath) => new LocalPackage(Path.Combine(Directory, relativePath));

        public override bool Exists(PathString path) => File.Exists(Directory.Concat(path));

        public override bool DirectoryExists(PathString directory) => System.IO.Directory.Exists(Directory.Concat(directory));

        public override Stream ReadStream(PathString path) => File.OpenRead(Directory.Concat(path));

        public override IEnumerable<PathString> GetFiles(bool getFullPath = false, Func<string, bool>? fileNamePredicate = null)
            => PathUtility.GetFilesInDirectory(Directory, getFullPath, fileNamePredicate);
    }
}
