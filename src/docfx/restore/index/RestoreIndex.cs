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
        public static RestoreGitIndex TryGetGitIndex(string remote, string branch, string commit)
        {
            var restoreDir = PathUtility.UrlToShortName(remote);
            var indexes = GetGitIndexes(restoreDir).Where(i => i.Branch == branch);

            if (!string.IsNullOrEmpty(commit))
            {
                return indexes.FirstOrDefault(i => i.Commit == commit);
            }

            return indexes.OrderBy(i => i.Date).FirstOrDefault();
        }

        public static async Task<(string path, RestoreGitIndex index)> RequireGitIndex(string remote, string branch, string commit, InUseType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(remote));

            var restoreDir = PathUtility.UrlToShortName(remote);

            RestoreGitIndex index = null;
            await ProcessUtility.RunInsideMutex(
                restoreDir,
                () =>
                {
                    var indexes = GetGitIndexes(restoreDir);

                    switch (type)
                    {
                        case InUseType.Restore:
                            index = indexes.FirstOrDefault(i => string.IsNullOrEmpty(i.InUse)) ?? new RestoreGitIndex
                            {
                                Line = indexes.Count() + 1,
                                Branch = branch,
                                Commit = commit ?? "{commit}",
                                Date = DateTime.UtcNow,
                                InUse = type.ToString(),
                            };

                            indexes.Add(index);
                            break;
                        case InUseType.Build:
                            Debug.Assert(!string.IsNullOrEmpty(commit));
                            index = indexes.FirstOrDefault(i => (string.IsNullOrEmpty(i.InUse) || i.InUse == type.ToString()) && i.Branch == branch && i.Commit == commit);
                            if (index != null)
                            {
                                index.InUse = type.ToString();
                            }
                            break;
                        default:
                            throw new NotSupportedException($"{type} is not supported");
                    }

                    WriteGitIndexes(restoreDir, indexes);
                    return Task.CompletedTask;
                });

            return index == null ? default : (Path.Combine(restoreDir, $"{index.Line}"), index);
        }

        public static async Task ReleaseGitIndex(string remote, RestoreGitIndex index)
        {
            Debug.Assert(index != null);
            var restoreDir = PathUtility.UrlToShortName(remote);

            await ProcessUtility.RunInsideMutex(
                restoreDir,
                () =>
                {
                    var indexes = GetGitIndexes(restoreDir);

                    var indexToRelease = indexes.FirstOrDefault(i => i.Line == index.Line && i.InUse == InUseType.Build.ToString());

                    Debug.Assert(index != null);

                    indexToRelease.InUse = null;

                    WriteGitIndexes(restoreDir, indexes);
                    return Task.CompletedTask;
                });
        }

        private static List<RestoreGitIndex> GetGitIndexes(string restoreDir)
        {
            var indexFile = Path.Combine(restoreDir, "index.txt");
            var content = File.Exists(indexFile) ? File.ReadAllText(indexFile) : string.Empty;

            return JsonUtility.Deserialize<List<RestoreGitIndex>>(content);
        }

        private static void WriteGitIndexes(string restoreDir, List<RestoreGitIndex> indexes)
        {
            var indexFile = Path.Combine(restoreDir, "index.txt");
            File.WriteAllText(indexFile, JsonUtility.Serialize(indexes));
        }
    }

    public enum InUseType
    {
        Restore,
        Build,
    }
}
