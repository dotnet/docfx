// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class GarbageCollector
    {
        public static async Task Collect(int retentionDays)
        {
            Debug.Assert(retentionDays > 0);
            var cleanedWorkTrees = await CollectGit(retentionDays);
            var cleanedDownloadedFiles = CollectUrls(retentionDays);
            Console.WriteLine($"Cleaned {cleanedWorkTrees} git work trees and {cleanedDownloadedFiles} downloaded files");
        }

        private static async Task<int> CollectGit(int retentionDays)
        {
            var cleaned = 0;
            if (!Directory.Exists(AppData.GitRoot))
            {
                return cleaned;
            }

            using (Progress.Start("Cleaning git repositories"))
            {
                var gitWorkTreeRoots = Directory.EnumerateDirectories(AppData.GitRoot, ".git", SearchOption.AllDirectories);

                await ParallelUtility.ForEach(gitWorkTreeRoots, CleanWorkTrees, Progress.Update);
            }

            Task CleanWorkTrees(string gitWorkTreeRoot)
            {
                return ProcessUtility.RunInsideMutex(
                       Path.GetFullPath(gitWorkTreeRoot),
                       () =>
                       {
                           var workTreeFolder = Path.GetDirectoryName(gitWorkTreeRoot);
                           var existingWorkTreeFolders = Directory
                                .EnumerateDirectories(workTreeFolder, "*", SearchOption.TopDirectoryOnly)
                                .Where(f => !f.EndsWith(".git"));

                           foreach (var existingWorkTreeFolder in existingWorkTreeFolders)
                           {
                               if (new DirectoryInfo(existingWorkTreeFolder).LastWriteTimeUtc + TimeSpan.FromDays(retentionDays) < DateTime.UtcNow)
                               {
                                   Interlocked.Increment(ref cleaned);
                                   Directory.Delete(existingWorkTreeFolder, true);
                               }
                           }

                           return GitUtility.PruneWorkTrees(workTreeFolder);
                       });
            }

            return cleaned;
        }

        private static int CollectUrls(int retentionDays)
        {
            var cleaned = 0;
            if (!Directory.Exists(AppData.DownloadsRoot))
            {
                return cleaned;
            }

            using (Progress.Start("Cleaning downloaded files"))
            {
                var downloadedFiles = Directory.EnumerateFiles(AppData.DownloadsRoot, "*", SearchOption.AllDirectories);

                ParallelUtility.ForEach(
                    downloadedFiles,
                    downloadedFile =>
                    {
                        if (new FileInfo(downloadedFile).LastWriteTimeUtc + TimeSpan.FromDays(retentionDays) < DateTime.UtcNow)
                        {
                            Interlocked.Increment(ref cleaned);
                            File.Delete(downloadedFile);
                        }
                    },
                    Progress.Update);
            }

            return cleaned;
        }
    }
}
