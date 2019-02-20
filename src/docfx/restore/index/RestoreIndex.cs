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
    internal static class RestoreIndex
    {
        /// <summary>
        /// Try get git dependency repository path with remote, branch and dependency version(commit).
        /// If the dependency version is null, get the latest one(order by last write time).
        /// If the dependency version is not null, get the one matched with the version(commit).
        /// </summary>
        public static bool TryGetGitIndex(string remote, string branch, string commit, out string path, out RestoreGitIndex index)
        {
            var restoreDir = AppData.GetGitDir(remote);
            var indexes = GetIndexes<RestoreGitIndex>(restoreDir).Where(i => i.Branch == branch);

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

        public static Task<(string path, RestoreGitIndex index)> RequireGitIndex(string remote, string branch, string commit, LockType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(commit));

            return RequireIndex(
                remote,
                type,
                id => new RestoreGitIndex
                {
                    Id = id,
                    Branch = branch,
                    Commit = commit,
                },
                existingIndex => existingIndex.Branch == branch && existingIndex.Commit == commit);
        }

        public static async Task ReleaseIndex<T>(string remote, T index, LockType lockType, bool successed = true) where T : RestoreIndexModel
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

        private static async Task<(string path, T index)> RequireIndex<T>(string remote, LockType type, Func<string, T> createNewIndex, Func<T, bool> matchExistingIndex) where T : RestoreIndexModel
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
                        case LockType.Restore: // find an available index or create a new index for using
                            index = indexes.FirstOrDefault(i => ProcessUtility.AcquireExclusiveLock(i.Id)) ?? createNewIndex($"{indexes.Count + 1}");
                            index.RestoredDate = DateTime.UtcNow;
                            indexes.Add(index);
                            break;
                        case LockType.Build: // find an matched index for using
                            index = indexes.FirstOrDefault(i => ProcessUtility.AcquireSharedLock(i.Id) && matchExistingIndex(i) && i.Restored);
                            break;
                        default:
                            throw new NotSupportedException($"{type} is not supported");
                    }

                    WriteIndexes<T>(restoreDir, indexes);
                    return Task.CompletedTask;
                });

            return index == null ? default : ($"{index.Id}", index);
        }

        private static List<T> GetIndexes<T>(string restoreDir) where T : RestoreIndexModel
        {
            var indexFile = Path.Combine(restoreDir, "index.json");
            var content = File.Exists(indexFile) ? File.ReadAllText(indexFile) : string.Empty;

            return JsonUtility.Deserialize<List<T>>(content) ?? new List<T>();
        }

        private static void WriteIndexes<T>(string restoreDir, List<T> indexes) where T : RestoreIndexModel
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
