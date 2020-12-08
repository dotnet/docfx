// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class CompositePackage : Package
    {
        private readonly List<Package> _packages;

        public override PathString BasePath => _packages.First().BasePath;

        public CompositePackage(List<Package> packages)
        {
            Debug.Assert(packages.Count != 0);
            Debug.Assert(packages.All(pkg => pkg.BasePath == packages[0].BasePath));
            _packages = packages;
        }

        public override bool Exists(PathString path) => _packages.Any(pkg => pkg.Exists(path));

        public override IEnumerable<PathString> GetFiles(PathString directory = default, string[]? allowedFileNames = null)
            => _packages.SelectMany(pkg => pkg.GetFiles(directory, allowedFileNames));

        public override PathString GetFullFilePath(PathString path) => _packages.First((pkg) => pkg.Exists(path)).GetFullFilePath(path);

        public override DateTime? TryGetLastWriteTimeUtc(PathString path)
        {
            for (int i = 0; i < _packages.Count; i++)
            {
                var lastWriteTimeUtc = _packages[i].TryGetLastWriteTimeUtc(path);
                if (lastWriteTimeUtc != null)
                {
                    return lastWriteTimeUtc;
                }
            }

            return null;
        }

        public override byte[] ReadBytes(PathString path) => _packages.First((pkg) => pkg.Exists(path)).ReadBytes(path);

        public override Stream ReadStream(PathString path) => _packages.First((pkg) => pkg.Exists(path)).ReadStream(path);

        public override PathString? TryGetGitFilePath(PathString path)
        {
            for (int i = 0; i < _packages.Count; i++)
            {
                var gitFilePath = _packages[i].TryGetGitFilePath(path);
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
                var physicalPath = _packages[i].TryGetPhysicalPath(path);
                if (physicalPath != null)
                {
                    return physicalPath;
                }
            }

            return null;
        }
    }
}
