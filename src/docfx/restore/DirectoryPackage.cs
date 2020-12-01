// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;

namespace Microsoft.Docs.Build
{
    internal abstract class DirectoryPackage : Package
    {
        protected PathString Directory { get; }

        public DirectoryPackage(string directory = ".") => Directory = new PathString(Path.GetFullPath(directory));

        public abstract DirectoryPackage CreateSubPackage(string relativePath);

        public override bool Exists(PathString path) => throw new NotSupportedException();

        public abstract bool DirectoryExists(PathString directory);

        public override Stream ReadStream(PathString path) => throw new NotSupportedException();

        public override IEnumerable<PathString> GetFiles()
        {
            if (!DirectoryExists(Directory))
            {
                throw Errors.Config.DirectoryNotFound(Directory).ToException();
            }
            return GetFiles(false, null);
        }

        public abstract IEnumerable<PathString> GetFiles(bool getFullPath = false, Func<string, bool>? fileNameFilter = null);

        public override PathString? TryGetPhysicalPath(PathString path)
        {
            var fullPath = Directory.Concat(path);
            if (File.Exists(fullPath))
            {
                return new PathString(fullPath);
            }

            return null;
        }

        public virtual PathString GetFullFilePath(PathString path) => new PathString(Directory.Concat(path));

        public override PathString? TryGetFullFilePath(PathString path)
        {
            var fullPath = new PathString(Directory.Concat(path));
            if (Exists(fullPath))
            {
                return fullPath;
            }

            return null;
        }

        public override PathString? TryGetGitFilePath(PathString path)
        {
            return new PathString(Directory.Concat(path));
        }
    }
}
