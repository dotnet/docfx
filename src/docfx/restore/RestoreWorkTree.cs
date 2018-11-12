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
    internal static class RestoreWorkTree
    {
        public static string GetRestoreWorkTreeDir(string restoreDir, string workTreeHead)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, workTreeHead));

        public static async Task<List<(string href, string head)>> AddWorkTrees(string restoreDir, List<string> hrefs, Config config)
        {
            Debug.Assert(!string.IsNullOrEmpty(restoreDir));
            Debug.Assert(hrefs != null && hrefs.Any());

            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));
            var (url, refspec) = GitUtility.GetGitRemoteInfo(hrefs.First());
            var workTreeHeads = new ConcurrentBag<(string href, string head)>();

            await ProcessUtility.RunInsideMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.GitRestoreDir, restorePath)),
                async () =>
                {
                    try
                    {
                        // TODO: group worktrees to avoid calling `GitUtility.GetGitRemoteInfo` repeatedly.
                        var refspecs = hrefs.Select(h => GitUtility.GetGitRemoteInfo(h).refspec);
                        await GitUtility.CloneOrUpdateBare(restorePath, url, refspecs, config);
                        await AddWorkTrees();
                    }
                    catch (Exception ex)
                    {
                        throw Errors.GitCloneFailed(hrefs.First()).ToException(ex);
                    }
                });

            async Task AddWorkTrees()
            {
                var existingWorkTrees = new ConcurrentDictionary<string, int>((await GitUtility.ListWorkTree(restorePath)).ToDictionary(k => k, v => 0));
                await ParallelUtility.ForEach(hrefs, async href =>
                {
                    var (_, rev) = GitUtility.GetGitRemoteInfo(href);
                    var workTreeHead = $"{GitUtility.RevParse(restorePath, rev)}-{PathUtility.Encode(rev)}";
                    var workTreePath = GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    if (existingWorkTrees.TryAdd(workTreePath, 0))
                    {
                        // use branch name instead of commit hash
                        // https://git-scm.com/docs/git-worktree#_commands
                        await GitUtility.AddWorkTree(restorePath, rev, workTreePath);
                    }

                    // update the last write time
                    Directory.SetLastWriteTimeUtc(workTreePath, DateTime.UtcNow);

                    workTreeHeads.Add((href, workTreeHead));
                });
            }

            Debug.Assert(hrefs.Count == workTreeHeads.Count);
            return workTreeHeads.ToList();
        }
    }
}
