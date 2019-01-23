// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class DependencyLock : DependencyVersion
    {
        public IReadOnlyDictionary<string, DependencyLock> Git { get; set; } = new Dictionary<string, DependencyLock>();

        public IReadOnlyDictionary<string, DependencyVersion> Downloads { get; set; } = new Dictionary<string, DependencyVersion>();

        public DependencyLock()
        {
        }

        public DependencyLock(IReadOnlyDictionary<string, DependencyLock> gitVersions, IReadOnlyDictionary<string, DependencyVersion> downloads, DependencyVersion version = null)
            : this(gitVersions, downloads, version?.Commit, version?.Hash)
        {
        }

        public DependencyLock(IReadOnlyDictionary<string, DependencyLock> gitVersions, IReadOnlyDictionary<string, DependencyVersion> downloads, string commit, string hash)
            : base(commit, hash)
        {
            Debug.Assert(gitVersions != null);
            Debug.Assert(downloads != null);

            Git = gitVersions;
            Downloads = downloads;
        }

        public DependencyLock GetGitLock(string href, string branch)
        {
            if (Git.TryGetValue($"{href}#{branch}", out var dependencyLock))
            {
                return dependencyLock;
            }

            if (branch == "master" && Git.TryGetValue($"{href}", out dependencyLock))
            {
                return dependencyLock;
            }

            return null;
        }

        public bool ContainsGitLock(string href)
        {
            return Git.ContainsKey(href) || Git.Keys.Any(g => g.StartsWith($"{href}#"));
        }

        public static async Task<DependencyLock> Load(string docset, string dependencyLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            if (string.IsNullOrEmpty(dependencyLockPath))
            {
                return null;
            }

            // dependency lock path can be a place holder for saving usage
            if (!HrefUtility.IsHttpHref(dependencyLockPath))
            {
                if (!File.Exists(Path.Combine(docset, dependencyLockPath)))
                {
                    return null;
                }
            }

            var (_, restoredLockFile) = RestoreMap.GetFileRestorePath(docset, dependencyLockPath);

            var content = await ProcessUtility.ReadFile(restoredLockFile);

            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            return JsonUtility.Deserialize<DependencyLock>(content);
        }

        public static Task<DependencyLock> Load(string docset, CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var (errors, config) = ConfigLoader.TryLoad(docset, commandLineOptions);

            return Load(docset, config.DependencyLock);
        }

        public static async Task Save(string docset, string dependencyLockPath, DependencyLock dependencyLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));
            Debug.Assert(!string.IsNullOrEmpty(dependencyLockPath));

            var content = JsonUtility.Serialize(dependencyLock, formatting: Newtonsoft.Json.Formatting.Indented);

            if (!HrefUtility.IsHttpHref(dependencyLockPath))
            {
                await ProcessUtility.WriteFile(Path.Combine(docset, dependencyLockPath), content);
            }

            // todo: upload to remote file directly
        }
    }
}
