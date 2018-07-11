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
            var docsetPath = ".restore_clean";
            var gitUrl = "https://github.com/docascode/docfx-test-dependencies-clean";
            Directory.CreateDirectory(docsetPath);
            var restorePath = PathUtility.NormalizeFolder(Path.Combine(Restore.GetRestoreRootDir(gitUrl), ".git"));

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep1: {gitUrl}#test-clean-1
  dep2: {gitUrl}#test-clean-2
  dep3: {gitUrl}#test-clean-3
  dep4: {gitUrl}#test-clean-4
  dep5: {gitUrl}#master
  dep6: {gitUrl}#chi");

            // run restroe and check the work trees
            await Program.Run(new[] { "restore", docsetPath });
            var workTreeList = await GitUtility.ListWorkTrees(restorePath, false);
            Assert.Equal(6, workTreeList.Count);

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep5: {gitUrl}#master
  dep6: {gitUrl}#chi");

            // run restore again to clean up
            // check the work trees
            await Program.Run(new[] { "restore", docsetPath });
            workTreeList = await GitUtility.ListWorkTrees(restorePath, false);
            Assert.Equal(2, workTreeList.Count);

            // check restore lock file
            var restoreLock = await RestoreLocker.Load(docsetPath);
            Assert.Equal(2, restoreLock.Git.Count);
        }
    }
}
