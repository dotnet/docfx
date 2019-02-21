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
                () =>
                {
                    var indexes = GetIndexes<DependencyGitIndex>(restoreDir);

                    var filteredIndex = indexes.Where(i => i.Branch == branch);
                    if (!string.IsNullOrEmpty(commit))
                    {
                        // found commit matched index
                        filteredIndex = filteredIndex.Where(i => i.Commit == commit);
                    }

                    // found latest restored index
                    index = indexes.OrderByDescending(i => i.LastAccessDate).FirstOrDefault(i => i.Restored);

                    if (index != null)
                    {
                        index.LastAccessDate = DateTime.UtcNow;
                        path = PathUtility.NormalizeFile(Path.Combine(restoreDir, $"{index.Id}"));
                    }

                    WriteIndexes(restoreDir, indexes.ToList());

                    return Task.CompletedTask;
                });

            return !Directory.Exists(path) ? default : (path, index);
        }

        /// <summary>
        /// Get restored git repository path, dependencyLock and index with url and dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static Task<(string path, DependencyLockModel subDependencyLock, DependencyGitIndex gitIndex)> AcquireGitIndex2Build(string url, DependencyLockModel dependencyLock)
        {
            var (remote, branch) = HrefUtility.SplitGitHref(url);
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
            Debug.Assert(!string.IsNullOrEmpty(index?.Url));

            var url = index.Url;
            var restoreDir = AppData.GetGitDir(url);

            await ProcessUtility.RunInsideMutex(
                url + "/index.json",
                () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);
                    var indexesToRelease = indexes.Where(i => i.Id == index.Id);
                    Debug.Assert(indexesToRelease.Count() == 1);

                    var indexToRelease = indexesToRelease.First();
                    Debug.Assert(index != null);
                    Debug.Assert(indexToRelease != null);

                    switch (lockType)
                    {
                        case LockType.Restore:
                            indexToRelease.LastAccessDate = DateTime.UtcNow;
                            Debug.Assert(ProcessUtility.ReleaseExclusiveLock(GetLockKey(url, indexToRelease.Id)));
                            break;
                        case LockType.Build:
                            Debug.Assert(ProcessUtility.ReleaseSharedLock(GetLockKey(url, indexToRelease.Id)));
                            break;
                    }

                    indexToRelease.Restored = successed;
                    WriteIndexes(restoreDir, indexes);
                    return Task.CompletedTask;
                });
        }

        private static Task<(string path, DependencyGitIndex index)> AcquireGitIndex(string remote, string branch, string commit, LockType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(commit));

            return AcquireIndex(
                remote,
                type,
                id => new DependencyGitIndex
                {
                    Id = id,
                    Branch = branch,
                    Commit = commit,
                },
                existingIndex => existingIndex.Branch == branch && existingIndex.Commit == commit);
        }

        private static async Task<(string path, T index)> AcquireIndex<T>(string url, LockType type, Func<string, T> createNewIndex, Func<T, bool> matchExistingIndex) where T : DependencyIndex
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var restoreDir = AppData.GetGitDir(url);

            T index = null;
            await ProcessUtility.RunInsideMutex(
                url + "/index.json",
                () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);

                    switch (type)
                    {
                        case LockType.Restore: // find an available index or create a new index for restoring
                            index = indexes.FirstOrDefault(i =>
                                        DateTime.UtcNow - i.LastAccessDate > TimeSpan.FromSeconds(_defaultLockdownTimeInSecond) && // in case it's just restored, not used yet
                                        ProcessUtility.AcquireExclusiveLock(GetLockKey(url, i.Id))) // compairing conderations' order matters
                                    ?? (ProcessUtility.AcquireExclusiveLock(GetLockKey(url, $"{indexes.Count + 1}")) ? createNewIndex($"{indexes.Count + 1}") : null);
                            Debug.Assert(index != null);
                            index.Restored = false;
                            index.Url = url;
                            indexes.Add(index);
                            break;
                        case LockType.Build: // find an matched index for building
                            index = indexes.FirstOrDefault(i =>
                                        matchExistingIndex(i) &&
                                        i.Restored &&
                                        ProcessUtility.AcquireSharedLock(GetLockKey(url, i.Id))); // compairing conderations' order matters
                            break;
                        default:
                            throw new NotSupportedException($"{type} is not supported");
                    }

                    WriteIndexes(restoreDir, indexes);
                    return Task.CompletedTask;
                });

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
