// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class DependencyLock
    {
        public static DependencyGitLock GetGitLock(this DependencyGitLock dependencyLock, string href, string branch)
        {
            Debug.Assert(dependencyLock != null);

            if (dependencyLock.Git.TryGetValue($"{href}#{branch}", out var gitLock))
            {
                return gitLock;
            }

            if (branch == "master" && dependencyLock.Git.TryGetValue($"{href}", out gitLock))
            {
                return gitLock;
            }

            return null;
        }

        public static bool ContainsGitLock(this DependencyGitLock dependencyLock, string href)
        {
            Debug.Assert(dependencyLock != null);

            return dependencyLock.Git.ContainsKey(href) || dependencyLock.Git.Keys.Any(g => g.StartsWith($"{href}#"));
        }

        public static DependencyGitLock Load(string docset, SourceInfo<string> dependencyLockPath)
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

            return JsonUtility.Deserialize<DependencyGitLock>(content, new FilePath(dependencyLockPath));
        }

        public static void Save(string docset, string dependencyLockPath, DependencyGitLock dependencyLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));
            Debug.Assert(!string.IsNullOrEmpty(dependencyLockPath));

            var content = JsonUtility.Serialize(dependencyLock, indent: true);

            if (!UrlUtility.IsHttp(dependencyLockPath))
            {
                var path = Path.Combine(docset, dependencyLockPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ProcessUtility.WriteFile(path, content);
            }
        }
    }
}
