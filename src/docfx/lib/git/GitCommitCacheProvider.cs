// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class GitCommitCacheProvider
    {
        private const int MaxCommitCacheCountPerFile = 10;

        public static async Task<ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>> LoadCommitCache(string repoRemote)
        {
            Debug.Assert(!string.IsNullOrEmpty(repoRemote));
            var cacheFilePath = AppData.GetCommitCachePath(repoRemote);
            if (!File.Exists(cacheFilePath))
            {
                return new ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>();
            }
            return await ProcessUtility.ReadFile(cacheFilePath, stream =>
            {
                var result = new ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>();
                using (var reader = new BinaryReader(stream))
                {
                    var fileCount = reader.ReadInt32();
                    for (var fileIndex = 0; fileIndex < fileCount; fileIndex++)
                    {
                        var file = reader.ReadString();
                        var cacheCount = reader.ReadInt32();
                        var cachedCommits = result.GetOrAdd(file, _ => new Dictionary<(long, long), (long[], int)>());
                        for (var cacheIndex = 0; cacheIndex < cacheCount; cacheIndex++)
                        {
                            var commit = reader.ReadInt64();
                            var blob = reader.ReadInt64();
                            var commitCount = reader.ReadInt32();
                            var commitHistory = new long[commitCount];
                            for (var commitIndex = 0; commitIndex < commitCount; commitIndex++)
                            {
                                commitHistory[commitIndex] = reader.ReadInt64();
                            }
                            cachedCommits.Add((commit, blob), (commitHistory, cacheIndex + 1));
                        }
                    }
                }
                return result;
            });
        }

        public static Task SaveCache(string repoRemote, IReadOnlyList<KeyValuePair<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>> commitCache)
        {
            Debug.Assert(!string.IsNullOrEmpty(repoRemote));

            if (commitCache == null)
            {
                return Task.CompletedTask;
            }

            var cacheFilePath = AppData.GetCommitCachePath(repoRemote);
            PathUtility.CreateDirectoryFromFilePath(cacheFilePath);
            return ProcessUtility.WriteFile(cacheFilePath, stream =>
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(commitCache.Count);
                    foreach (var (file, value) in commitCache)
                    {
                        // todo: this implicit lock is now split across too many places, need consolidate and abstract them.
                        lock (value)
                        {
                            writer.Write(file);
                            writer.Write(Math.Min(value.Count, MaxCommitCacheCountPerFile));
                            var lruValues = value.OrderBy(pair => pair.Value.lruOrder).Take(MaxCommitCacheCountPerFile);
                            foreach (var ((commit, blob), (commitHistory, _)) in lruValues)
                            {
                                writer.Write(commit);
                                writer.Write(blob);
                                writer.Write(commitHistory.Length);
                                foreach (var sha in commitHistory)
                                {
                                    writer.Write(sha);
                                }
                            }
                        }
                    }
                }
            });
        }
    }
}
