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
        public static bool TryGetGitIndex(string remote, string branch, string commit, out string path, out DependencyGitIndex index)
        {
            var restoreDir = AppData.GetGitDir(remote);
            var indexes = GetIndexes<DependencyGitIndex>(restoreDir).Where(i => i.Branch == branch);

            path = null;
            if (!string.IsNullOrEmpty(commit))
            {
                // found commit matched index
                index = indexes.FirstOrDefault(i => i.Commit == commit && ProcessUtility.IsExclusiveLockHeld(i.Id) /*not being restored*/);
            }

            // found latest restored index
            index = indexes.OrderByDescending(i => i.RestoredDate).FirstOrDefault(i => ProcessUtility.IsExclusiveLockHeld(i.Id) /*not being restored*/);

            if (index != null)
            {
                path = PathUtility.NormalizeFile(Path.Combine(restoreDir, $"{index.Id}"));
            }

            return Directory.Exists(path);
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

        public static async Task ReleaseIndex<T>(string remote, T index, LockType lockType, bool successed = true) where T : DependencyIndex
        {
            Debug.Assert(index != null);
            var restoreDir = AppData.GetGitDir(remote);

            await ProcessUtility.RunInsideMutex(
                remote + "/index.json",
                () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);
                    var indexToRelease = indexes.FirstOrDefault(i => i.Id == index.Id);

                    Debug.Assert(index != null);
                    Debug.Assert(indexToRelease != null);

                    switch (lockType)
                    {
                        case LockType.Restore:
                            indexToRelease.RestoredDate = DateTime.UtcNow;
                            ProcessUtility.ReleaseExclusiveLock(indexToRelease.Id);
                            break;
                        case LockType.Build:
                            ProcessUtility.ReleaseSharedLock(indexToRelease.Id);
                            break;
                    }

                    indexToRelease.Restored = successed;
                    WriteIndexes<T>(restoreDir, indexes);
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

        private static async Task<(string path, T index)> AcquireIndex<T>(string remote, LockType type, Func<string, T> createNewIndex, Func<T, bool> matchExistingIndex) where T : DependencyIndex
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));

            var restoreDir = AppData.GetGitDir(remote);

            T index = null;
            await ProcessUtility.RunInsideMutex(
                remote + "/index.json",
                () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);

                    switch (type)
                    {
                        case LockType.Restore: // find an available index or create a new index for restoring
                            index = indexes.FirstOrDefault(i =>
                                        DateTime.UtcNow - i.RestoredDate > TimeSpan.FromSeconds(_defaultLockdownTimeInSecond) && // in case it's just restored, not used yet
                                        ProcessUtility.AcquireExclusiveLock(i.Id)) // compairing conderations' order matters
                                    ?? createNewIndex($"{indexes.Count + 1}");
                            index.RestoredDate = DateTime.UtcNow;
                            index.Restored = false;
                            indexes.Add(index);
                            break;
                        case LockType.Build: // find an matched index for building
                            index = indexes.FirstOrDefault(i =>
                                        matchExistingIndex(i) &&
                                        i.Restored &&
                                        ProcessUtility.AcquireSharedLock(i.Id)); // compairing conderations' order matters
                            break;
                        default:
                            throw new NotSupportedException($"{type} is not supported");
                    }

                    WriteIndexes<T>(restoreDir, indexes);
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
    }

    public enum LockType
    {
        None,
        Build,
        Restore,
    }
}
