// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DependencyLockProvider
    {
        private readonly IReadOnlyDictionary<(string url, string branch), DependencyGitLock> _dependencyGitLock;

        private DependencyLockProvider(IReadOnlyDictionary<(string url, string branch), DependencyGitLock> dependencyGitLock)
        {
            Debug.Assert(dependencyGitLock != null);

            _dependencyGitLock = dependencyGitLock;
        }

        public DependencyGitLock GetGitLock(string url, string branch)
        {
            if (_dependencyGitLock.TryGetValue((url, branch), out var gitLock))
            {
                return gitLock;
            }

            return null;
        }

        public IEnumerable<(string url, string branch, DependencyGitLock)> ListAll()
        {
            return _dependencyGitLock.Select(k => (k.Key.url, k.Key.branch, k.Value));
        }

        public static DependencyLockProvider Create(string docsetPath, SourceInfo<string> dependencyLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            if (string.IsNullOrEmpty(dependencyLockPath))
            {
                return new DependencyLockProvider(new Dictionary<(string url, string branch), DependencyGitLock>());
            }

            // dependency lock path can be a place holder for saving usage
            if (!UrlUtility.IsHttp(dependencyLockPath))
            {
                if (!File.Exists(Path.Combine(docsetPath, dependencyLockPath)))
                {
                    return new DependencyLockProvider(new Dictionary<(string url, string branch), DependencyGitLock>());
                }
            }

            var content = RestoreFileMap.GetRestoredFileContent(docsetPath, dependencyLockPath, fallbackDocset: null);

            Log.Write($"DependencyLock ({dependencyLockPath}):\n{content}");

            var dependencyLock = JsonUtility.Deserialize<DependencyLock>(content, new FilePath(dependencyLockPath));

            return new DependencyLockProvider(dependencyLock.Git.ToDictionary(
                k =>
                {
                    var packageUrl = new PackageUrl(k.Key);
                    return (packageUrl.Url, packageUrl.Branch);
                },
                v => v.Value));
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
