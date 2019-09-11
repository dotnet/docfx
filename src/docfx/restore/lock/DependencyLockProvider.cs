// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class DependencyLockProvider
    {
        public static DependencyGitLock GetGitLock(this IReadOnlyDictionary<PackageUrl, DependencyGitLock> dependencyLock, PackageUrl packageUrl)
        {
            if (dependencyLock == null)
            {
                return null;
            }

            if (dependencyLock.TryGetValue(packageUrl, out var gitLock))
            {
                return gitLock;
            }

            return null;
        }

        public static Dictionary<PackageUrl, DependencyGitLock> LoadGitLock(string docset, SourceInfo<string> dependencyLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            if (string.IsNullOrEmpty(dependencyLockPath))
            {
                return null;
            }

            // dependency lock path can be a place holder for saving usage
            if (!UrlUtility.IsHttp(dependencyLockPath))
            {
                if (!File.Exists(Path.Combine(docset, dependencyLockPath)))
                {
                    return null;
                }
            }

            var content = RestoreFileMap.GetRestoredFileContent(docset, dependencyLockPath, fallbackDocset: null);

            Log.Write($"DependencyLock ({dependencyLockPath}):\n{content}");

            var dependencyLock = JsonUtility.Deserialize<DependencyLock>(content, new FilePath(dependencyLockPath));

            return dependencyLock.Git.ToDictionary(k => new PackageUrl(k.Key), v => v.Value);
        }

        public static void SaveGitLock(string docset, string dependencyLockPath, List<DependencyGitLock> dependencyGitLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));
            Debug.Assert(!string.IsNullOrEmpty(dependencyLockPath));

            var dependencyLock = new DependencyLock { Git = dependencyGitLock.ToDictionary(k => $"{new PackageUrl(k.Url, k.Branch)}", v => v) };
            var content = JsonUtility.Serialize(dependencyLock, indent: true);

            if (!UrlUtility.IsHttp(dependencyLockPath))
            {
                var path = Path.Combine(docset, dependencyLockPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ProcessUtility.WriteFile(path, content);
            }
        }

        private class DependencyLock
        {
            public Dictionary<string, DependencyGitLock> Git { get; set; } = new Dictionary<string, DependencyGitLock>();
        }
    }
}
