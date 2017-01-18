// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.Common;

    internal class ProjectLevelCache : CacheBase
    {
        private static Func<string, string> GetProjectLevelConfig = projectPath => Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, "obj", "xdoc", "cache", "obj", ".inter").ToNormalizedFullPath();
        private static ConcurrentDictionary<string, ProjectLevelCache> _cache = new ConcurrentDictionary<string, ProjectLevelCache>();
        public readonly string OutputFolder;

        private ProjectLevelCache(string projectPath) : base(projectPath)
        {
            OutputFolder = Path.GetDirectoryName(Path.GetFullPath(projectPath)).ToNormalizedFullPath();
        }

        public static ProjectLevelCache Get(IEnumerable<string> files)
        {
            if (files == null || !files.Any()) return null;
            var normalizedFiles = files.GetNormalizedFullPathList();

            // Use the folder for the first file as the cache folder
            var firstFile = normalizedFiles.First();

            string path = GetProjectLevelConfig(firstFile);
            return _cache.GetOrAdd(path, p => new ProjectLevelCache(p));
        }
    }
}
