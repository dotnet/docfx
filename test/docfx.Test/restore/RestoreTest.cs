// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class RestoreTest
    {
        [Theory]
        [InlineData("https://github.com/dotnet/docfx", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://visualstudio.com/dotnet/docfx", "https://visualstudio.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#master", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#live", "https://github.com/dotnet/docfx", "live")]
        [InlineData("https://github.com/dotnet/docfx#", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#986127a", "https://github.com/dotnet/docfx", "986127a")]
        [InlineData("https://github.com/dotnet/docfx#a#a", "https://github.com/dotnet/docfx", "a#a")]
        [InlineData("https://github.com/dotnet/docfx#a\\b/d<e>f*h|i%3C", "https://github.com/dotnet/docfx", "a\\b/d<e>f*h|i%3C")]
        public static void GetGitInfo(string remote, string expectedUrl, string expectedRev)
        {
            // Act
            var (url, rev) = Restore.GetGitRemoteInfo(remote);

            // Assert
            Assert.Equal(expectedUrl, url);
            Assert.Equal(expectedRev, rev);
        }

        [Fact]
        public static async Task RestoreGitWorkTrees()
        {
            // prepare denepdency repo and work trees to clean
            var docsetPath = ".restore_clean";
            var gitUrl = "https://github.com/docascode/docfx-test-dependencies-clean";
            var testBranches = new[] { "master", "chi", "test-clean-1", "test-clean-2", "test-clean-3", "test-clean-4" };

            Directory.CreateDirectory(docsetPath);
            var restoreDir = Restore.GetRestoreRootDir(gitUrl);
            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));
            await ProcessUtility.ProcessLock(
                Path.Combine(Path.GetRelativePath(AppData.RestoreDir, restorePath), ".lock"),
                async () =>
                {
                    if (GitUtility.IsRepo(restoreDir))
                    {
                        // already exists, just pull the new updates from remote
                        await GitUtility.Fetch(restorePath);
                    }
                    else
                    {
                        // doesn't exist yet, clone this repo to a specified branch
                        await GitUtility.Clone(restoreDir, gitUrl, restorePath, null, true);
                    }

                    var workTrees = new HashSet<string>(await GitUtility.ListWorkTrees(restorePath, false));
                    await ParallelUtility.ForEach(testBranches, async testBranch =>
                    {
                        var workTreeHead = await GitUtility.Revision(restorePath, testBranch);
                        var workTreePath = Restore.GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                        if (workTrees.Add(workTreePath))
                        {
                            await GitUtility.AddWorkTree(restorePath, workTreeHead, workTreePath);
                        }
                    });
                });

            // run restore
            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep5: {gitUrl}#master
  dep6: {gitUrl}#chi");

            await Restore.Run(docsetPath, new CommandLineOptions(), new Report());

            // check the work trees
            var workTreeList = await GitUtility.ListWorkTrees(restorePath, false);
            Assert.Equal(2, workTreeList.Count);

            // check restore lock file
            var restoreLock = await RestoreLocker.Load(docsetPath);
            Assert.Equal(2, restoreLock.Git.Count);
        }
    }
}
