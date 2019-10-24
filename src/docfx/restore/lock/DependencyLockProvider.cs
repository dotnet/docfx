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

        public static DependencyLockProvider CreateFromAppData(string docset, string locale)
        {
            var file = AppData.GetDependencyLockFile(docset, locale);

            if (!File.Exists(file))
            {
                return new DependencyLockProvider(new Dictionary<(string url, string branch), DependencyGitLock>());
            }

            var content = File.ReadAllText(file);

            return Create(file, content);
        }

        public static DependencyLockProvider CreateFromConfig(string docsetPath, Config config)
        {
            if (string.IsNullOrEmpty(config.DependencyLock))
            {
                return new DependencyLockProvider(new Dictionary<(string url, string branch), DependencyGitLock>());
            }

            var fullPath = Path.Combine(docsetPath, config.DependencyLock);
            if (!File.Exists(fullPath))
            {
                throw Errors.FileNotFound(config.DependencyLock).ToException();
            }

            var content = ProcessUtility.ReadFile(Path.Combine(docsetPath, config.DependencyLock));

            return Create(config.DependencyLock, content);
        }

        public static void SaveGitLock(string docset, string locale, string dependencyLockPath, List<DependencyGitLock> dependencyGitLock)
        {
            var dependencyLock = new DependencyLock { Git = dependencyGitLock.ToDictionary(k => $"{new PackagePath(k.Url, k.Branch)}", v => v) };
            var content = JsonUtility.Serialize(dependencyLock, indent: true);

            string path;
            if (!string.IsNullOrEmpty(dependencyLockPath))
            {
                // write to user specified place
                path = Path.Combine(docset, dependencyLockPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ProcessUtility.WriteFile(path, content);
            }

            // write to appdata, prepare for build
            path = AppData.GetDependencyLockFile(docset, locale);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            ProcessUtility.WriteFile(path, content);
        }

        private static DependencyLockProvider Create(string path, string content)
        {
            Debug.Assert(!string.IsNullOrEmpty(content));
            Log.Write($"DependencyLock ({path}):\n{content}");

            var dependencyLock = JsonUtility.Deserialize<DependencyLock>(content, new FilePath(path));

            return new DependencyLockProvider(dependencyLock.Git.ToDictionary(
                k =>
                {
                    var packageUrl = new PackagePath(k.Key);
                    return (packageUrl.Url, packageUrl.Branch);
                },
                v => v.Value));
        }

        private class DependencyLock
        {
            public Dictionary<string, DependencyGitLock> Git { get; set; } = new Dictionary<string, DependencyGitLock>();
        }
    }
}
