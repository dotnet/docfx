// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class CompositePackage : Package
    {
        private readonly List<Package> _packages;

        public CompositePackage(List<Package> packages)
        {
            _packages = packages;
        }

        public override bool DirectoryExists(string directory = ".") => _packages.Any(pkg => pkg.DirectoryExists(directory));

        public override bool Exists(PathString path) => _packages.Any(pkg => pkg.Exists(path));

        // public override bool DirectoryExists(PathString directory) => System.IO.Directory.Exists(Directory.Concat(directory));
        public override IEnumerable<PathString> GetFiles(string directory = ".", Func<string, bool>? fileNamePredicate = null)
            => _packages.SelectMany(pkg => pkg.DirectoryExists(directory) ? pkg.GetFiles(directory, fileNamePredicate) : new List<PathString>());

        public override PathString GetFullFilePath(PathString path)
        {
            var result = ApplyToFirst((pkg) => true, (pkg) => pkg.GetFullFilePath(path));
            if (result == default)
            {
                throw new FileNotFoundException(path);
            }
            return result;
        }

        public override DateTime GetLastWriteTimeUtc(PathString path)
        {
            var result = ApplyToFirst((pkg) => pkg.Exists(path), (pkg) => pkg.GetLastWriteTimeUtc(path));
            if (result == default)
            {
                throw new FileNotFoundException(path);
            }
            return result;
        }

        public override Stream ReadStream(PathString path)
        {
            var result = ApplyToFirst((pkg) => pkg.Exists(path), (pkg) => pkg.ReadStream(path));
            if (result == default)
            {
                throw new FileNotFoundException(path);
            }
            return result;
        }

        public override PathString? TryGetGitFilePath(PathString path)
        {
            for (int i = 0; i < _packages.Count; i++)
            {
                var package = _packages[i];
                var gitFilePath = package.TryGetGitFilePath(path);
                if (gitFilePath != null)
                {
                    return gitFilePath;
                }
            }

            return null;
        }

        public override PathString? TryGetPhysicalPath(PathString path)
        {
            for (int i = 0; i < _packages.Count; i++)
            {
                var package = _packages[i];
                var physicalPath = package.TryGetPhysicalPath(path);
                if (physicalPath != null)
                {
                    return physicalPath;
                }
            }

            return null;
        }

        // A nullable type parameter must be known to be a value type or non-nullable reference type unless language version '9.0' or greater is used
#pragma warning disable CS8603 // Possible null reference return.
        private T ApplyToFirst<T>(Func<Package, bool> predicate, Func<Package, T> func, T defaultValue = default(T))
        {
            for (int i = 0; i < _packages.Count; i++)
            {
                var package = _packages[i];
                if (predicate.Invoke(package))
                {
                    return func.Invoke(package);
                }
            }

            return defaultValue;
        }
#pragma warning restore CS8603 // Possible null reference return.
    }
}
