// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public class ApplicationLevelCache : CacheBase
    {
        private static Func<string, string, string> GetApplicationLevelCacheFilePath = (projectPath, fileName) => Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, "obj", "xdoc", "cache", "final", fileName);
        private static ConcurrentDictionary<string, ApplicationLevelCache> _cache = new ConcurrentDictionary<string, ApplicationLevelCache>();

        private ApplicationLevelCache(string projectPath) : base(projectPath)
        {
        }

        public static ApplicationLevelCache Get(IEnumerable<string> files)
        {
            string path = GetApplicationLevelCache(files);
            if (string.IsNullOrEmpty(path)) return null;
            return _cache.GetOrAdd(path, p => new ApplicationLevelCache(p));
        }

        private static string GetApplicationLevelCache(IEnumerable<string> files)
        {
            if (files == null || !files.Any()) return null;
            var normalizedFiles = files.GetNormalizedFullPathList();

            // Application Level cache locates in the same folder with the top one file in the file list
            var firstFile = normalizedFiles.First();

            // Use hash code
            var fileName = files.GetNormalizedFullPathKey().GetHashCode().ToString();
            return GetApplicationLevelCacheFilePath(firstFile, fileName);
        }
    }
}
