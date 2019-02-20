// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreIndex
    {
        private const int _defaultTimeoutInSeconds = 60 * 30;

        public static bool TryGetGitIndex(string remote, string branch, string commit, out string path, out RestoreGitIndex index)
        {
            var restoreDir = PathUtility.UrlToShortName(remote);
            var indexes = GetIndexes<RestoreGitIndex>(restoreDir).Where(i => i.Branch == branch);

            path = null;
            if (!string.IsNullOrEmpty(commit))
            {
                // found commit matched index
                index = indexes.FirstOrDefault(i => i.Commit == commit && i.LockType != LockType.Exclusive/*not being restored*/);
            }

            // found latest restored index
            index = indexes.OrderByDescending(i => i.RestoredDate).FirstOrDefault(i => i.LockType != LockType.Exclusive/*not being restored*/);

            if (index != null)
            {
                path = PathUtility.NormalizeFile(Path.Combine(restoreDir, $"{index.Id}"));
            }

            return Directory.Exists(path);
        }

        public static Task<(string path, RestoreGitIndex index)> RequireGitIndex(string remote, string branch, string commit, LockType type)
        {
            Debug.Assert(string.IsNullOrEmpty(branch));
            Debug.Assert(string.IsNullOrEmpty(commit));

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

        public static async Task ReleaseIndex<T>(string remote, T index, bool successed = true) where T : RestoreIndexModel
        {
            Debug.Assert(index != null);
            var restoreDir = PathUtility.UrlToShortName(remote);

            await ProcessUtility.RunInsideMutex(
                restoreDir,
                () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);

                    var indexToRelease = indexes.FirstOrDefault(i => i.Id == index.Id && i.LockType == index.LockType);

                    Debug.Assert(index != null);
                    Debug.Assert(indexToRelease != null);

                    switch (indexToRelease.LockType)
                    {
                        case LockType.Exclusive:
                            Debug.Assert(indexToRelease.RequiredBy.Count() == 1);
                            indexToRelease.RequiredBy.Clear();
                            indexToRelease.LockType = LockType.None;
                            break;
                        case LockType.Shared:
                            indexToRelease.RequiredBy.RemoveAll(r => r.Id == Thread.CurrentThread.ManagedThreadId);
                            if (!indexToRelease.RequiredBy.Any())
                            {
                                indexToRelease.LockType = LockType.None;
                            }
                            break;
                    }

                    indexToRelease.Restored = successed;
                    WriteIndexes<T>(restoreDir, indexes);
                    return Task.CompletedTask;
                });
        }

        private static async Task<(string path, T index)> RequireIndex<T>(string remote, LockType type, Func<int, T> createNewIndex, Func<T, bool> matchExistingIndex) where T : RestoreIndexModel
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));

            var restoreDir = PathUtility.UrlToShortName(remote);
            var requirer = new RestoreIndexAcquirer { Id = Thread.CurrentThread.ManagedThreadId, Date = DateTime.UtcNow };

            T index = null;
            await ProcessUtility.RunInsideMutex(
                restoreDir,
                () =>
                {
                    var indexes = GetIndexes<T>(restoreDir);

                    switch (type)
                    {
                        case LockType.Exclusive: // find an available index or create a new index for using
                            index = indexes.FirstOrDefault(i => i.LockType == LockType.None) ?? createNewIndex(indexes.Count + 1);
                            Debug.Assert(!index.RequiredBy.Any());
                            index.RestoredDate = DateTime.UtcNow;
                            index.LockType = LockType.Exclusive;
                            index.RequiredBy = new List<RestoreIndexAcquirer> { requirer };
                            indexes.Add(index);
                            break;
                        case LockType.Shared: // find an matched index for using
                            index = indexes.FirstOrDefault(i => (i.LockType == LockType.None || i.LockType == LockType.Shared) && matchExistingIndex(i) && i.Restored);
                            if (index != null)
                            {
                                index.RequiredBy.Add(requirer);
                            }
                            break;
                        default:
                            throw new NotSupportedException($"{type} is not supported");
                    }

                    WriteIndexes<T>(restoreDir, indexes);
                    return Task.CompletedTask;
                });

            return index == null ? default : (PathUtility.NormalizeFile(Path.Combine(restoreDir, $"{index.Id}")), index);
        }

        private static List<T> GetIndexes<T>(string restoreDir) where T : RestoreIndexModel
        {
            var indexFile = Path.Combine(restoreDir, "index.json");
            var content = File.Exists(indexFile) ? File.ReadAllText(indexFile) : string.Empty;

            var indexes = JsonUtility.Deserialize<List<T>>(content);

            foreach (var index in indexes)
            {
                // incase index requirer crashed, no longer release the index anymore
                index.RequiredBy.RemoveAll(r => DateTime.UtcNow - r.Date > TimeSpan.FromSeconds(_defaultTimeoutInSeconds));
                if (!index.RequiredBy.Any())
                {
                    index.LockType = LockType.None;
                }
            }

            return indexes;
        }

        private static void WriteIndexes<T>(string restoreDir, List<T> indexes) where T : RestoreIndexModel
        {
            var indexFile = Path.Combine(restoreDir, "index.json");
            File.WriteAllText(indexFile, JsonUtility.Serialize(indexes));
        }
    }

    public enum LockType
    {
        None,
        Shared,
        Exclusive,
    }
}
