// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class DependencyIndexPool
    {
        private const int _defaultLockdownTimeInSecond = 10 * 60;

        /// <summary>
        /// Try get git dependency repository path and index with remote, branch and dependency version(commit).
        /// If the dependency version is null, get the latest one(order by last write time).
        /// If the dependency version is not null, get the one matched with the version(commit).
        /// </summary>
        public static async Task<(string path, DependencyGitIndex index)> TryGetGitIndex(string remote, string branch, string commit)
        {
            var restoreDir = AppData.GetGitDir(remote);

            string path = null;
            DependencyGitIndex index = null;
            await ProcessUtility.RunInsideMutex(
                remote + "/index.json",
                async () =>
                {
                    var indexes = GetIndexes<DependencyGitIndex>(restoreDir);

                    var filteredIndex = indexes.Where(i => i.Branch == branch);
                    if (!string.IsNullOrEmpty(commit))
                    {
                        // found commit matched index
                        filteredIndex = filteredIndex.Where(i => i.Commit == commit);
                    }

                    // found latest restored index
                    foreach (var i in filteredIndex.OrderByDescending(i => i.LastAccessDate))
                    {
                        if (i.Restored && !await ProcessUtility.IsExclusiveLockHeld(GetLockKey(remote, i.Id)))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index != null)
                    {
                        index.LastAccessDate = DateTime.UtcNow;
                        path = PathUtility.NormalizeFile(Path.Combine(restoreDir, $"{index.Id}"));
                    }

                    WriteIndexes(restoreDir, indexes.ToList());
                });

            return !Directory.Exists(path) ? default : (path, index);
        }

        /// <summary>
        /// Get restored git repository path, dependencyLock and index with url and dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static Task<(string path, DependencyLockModel subDependencyLock, DependencyGitIndex gitIndex)> AcquireGitIndex2Build(string url, DependencyLockModel dependencyLock)
        {
            var (remote, branch, _) = HrefUtility.SplitGitHref(url);
            return AcquireGitIndex2Build(remote, branch, dependencyLock);
        }

        /// <summary>
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static async Task<(string path, DependencyLockModel subDependencyLock, DependencyGitIndex gitIndex)> AcquireGitIndex2Build(string remote, string branch, DependencyLockModel dependencyLock)
        {
            Debug.Assert(dependencyLock != null);

            var gitVersion = dependencyLock.GetGitLock(remote, branch);

            if (gitVersion == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            var (path, index) = await AcquireGitIndex(remote, branch, gitVersion.Commit, LockType.Build);

            if (string.IsNullOrEmpty(path) || index == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            path = Path.Combine(AppData.GetGitDir(remote), path);
            Debug.Assert(Directory.Exists(path));

            return (path, gitVersion, index);
        }

        public static Task<(string path, DependencyGitIndex index)> AcquireGitIndex2Restore(string remote, string branch, string commit)
            => AcquireGitIndex(remote, branch, commit, LockType.Restore);

        public static async Task ReleaseIndex<T>(T index, LockType lockType, bool successed = true) where T : DependencyIndex
        {
            Debug.Assert(index != null);
            Debug.Assert(!string.IsNullOrEmpty(index?.Url));

            var url = index.Url;
            var restoreDir = AppData.GetGitDir(url);

            await ProcessUtility.RunInsideMutex(
                url + "/index.json",
                async () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);
                    var indexesToRelease = indexes.Where(i => i.Id == index.Id);
                    Debug.Assert(indexesToRelease.Count() == 1);

                    var indexToRelease = indexesToRelease.First();
                    Debug.Assert(indexToRelease != null);
                    Debug.Assert(!string.IsNullOrEmpty(indexToRelease.Acquirer));

                    switch (lockType)
                    {
                        case LockType.Restore:
                            indexToRelease.LastAccessDate = DateTime.UtcNow;
                            indexToRelease.Restored = successed;
                            Debug.Assert(await ProcessUtility.ReleaseExclusiveLock(GetLockKey(url, index.Id), index.Acquirer));
                            break;
                        case LockType.Build:
                            Debug.Assert(await ProcessUtility.ReleaseSharedLock(GetLockKey(url, index.Id), index.Acquirer));
                            break;
                    }

                    WriteIndexes(restoreDir, indexes);
                });
        }

        private static Task<(string path, DependencyGitIndex index)> AcquireGitIndex(string remote, string branch, string commit, LockType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(commit));

            return AcquireIndex<DependencyGitIndex>(
                remote,
                type,
                index =>
                {
                    // update branch and commit info to new rented index
                    index.Commit = commit;
                    index.Branch = branch;
                    return index;
                },
                existingIndex => existingIndex.Branch == branch && existingIndex.Commit == commit);
        }

        private static async Task<(string path, T index)> AcquireIndex<T>(string url, LockType type, Func<T, T> updateExistingIndex, Func<T, bool> matchExistingIndex) where T : DependencyIndex, new()
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var restoreDir = AppData.GetGitDir(url);

            T index = null;
            bool acquired = false;
            string acquirer = null;
            await ProcessUtility.RunInsideMutex(
                url + "/index.json",
                async () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);

                    switch (type)
                    {
                        case LockType.Restore: // find an available index or create a new index for restoring
                            foreach (var i in indexes)
                            {
                                if (DateTime.UtcNow - i.LastAccessDate > TimeSpan.FromSeconds(_defaultLockdownTimeInSecond))
                                {
                                    (acquired, acquirer) = await ProcessUtility.AcquireExclusiveLock(GetLockKey(url, i.Id));
                                    if (acquired)
                                    {
                                        index = i;
                                        break;
                                    }
                                }
                            }

                            if (index == null)
                            {
                                (acquired, acquirer) = await ProcessUtility.AcquireExclusiveLock(GetLockKey(url, $"{indexes.Count + 1}"));
                                if (acquired)
                                {
                                    index = new T() { Id = $"{indexes.Count + 1}" };
                                }
                            }

                            Debug.Assert(index != null && acquired && !string.IsNullOrEmpty(acquirer));

                            // reset every property of rented index
                            index.Url = url;
                            index.Restored = false;
                            index.LastAccessDate = DateTime.MinValue;
                            index.Acquirer = acquirer;

                            index = updateExistingIndex(index);
                            indexes.Add(index);
                            break;
                        case LockType.Build: // find an matched index for building
                            foreach (var i in indexes)
                            {
                                if (matchExistingIndex(i))
                                {
                                    (acquired, acquirer) = await ProcessUtility.AcquireSharedLock(GetLockKey(url, i.Id));
                                    if (acquired)
                                    {
                                        index = i;
                                        index.Acquirer = acquirer;
                                        break;
                                    }
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException($"{type} is not supported");
                    }

                    WriteIndexes(restoreDir, indexes);
                });

            if (index != null)
            {
                Debug.Assert(!string.IsNullOrEmpty(index.Acquirer));
            }

            return index == null ? default : ($"{index.Id}", index);
        }

        private static List<T> GetIndexes<T>(string restoreDir) where T : DependencyIndex
        {
            var indexFile = Path.Combine(restoreDir, "index.json");
            var content = File.Exists(indexFile) ? File.ReadAllText(indexFile) : string.Empty;

            return JsonUtility.Deserialize<List<T>>(content) ?? new List<T>();
        }

        private static void WriteIndexes<T>(string restoreDir, List<T> indexes) where T : DependencyIndex
        {
            Directory.CreateDirectory(restoreDir);
            var indexFile = Path.Combine(restoreDir, "index.json");
            File.WriteAllText(indexFile, JsonUtility.Serialize(indexes));
        }

        private static string GetLockKey(string remote, string id) => $"{remote}/{id}";
    }

    public enum LockType
    {
        None,
        Build,
        Restore,
    }
}
