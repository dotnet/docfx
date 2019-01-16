// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
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
        public static void SplitGitHref(string remote, string expectedUrl, string expectedRev)
        {
            // Act
            var (url, rev) = HrefUtility.SplitGitHref(remote);

            // Assert
            Assert.Equal(expectedUrl, url);
            Assert.Equal(expectedRev, rev);
        }

        [Fact]
        public static async Task RestoreGitWorkTrees()
        {
            var docsetPath = "restore-worktrees";
            var gitUrl = "https://github.com/docascode/docfx-test-dependencies-clean";

            Directory.CreateDirectory(docsetPath);

            var restoreDir = AppData.GetGitDir(gitUrl);
            DeleteDir(restoreDir);

            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));
            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep1: {gitUrl}#test-1-clean
  dep2: {gitUrl}#test-2-clean
  dep3: {gitUrl}#test-3-clean
  dep4: {gitUrl}#test-4-clean
  dep5: {gitUrl}#master
  dep6: {gitUrl}#chi");


            // run restroe and check the work trees
            await Program.Run(new[] { "restore", docsetPath });
            var workTreeList = await GitUtility.ListWorkTree(restorePath);
            Assert.Equal(6, workTreeList.Count);

            foreach (var wirkTreeFolder in workTreeList.Where(w => w.Contains("-clean-")))
            {
                Directory.SetLastWriteTimeUtc(wirkTreeFolder, DateTime.UtcNow - TimeSpan.FromDays(20));
            }

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep5: {gitUrl}#master
  dep6: {gitUrl}#chi");

            // run restore again to clean up
            // check the work trees
            await Program.Run(new[] { "restore", docsetPath });
            await Program.Run(new[] { "gc" });

            workTreeList = await GitUtility.ListWorkTree(restorePath);
            Assert.Equal(2, workTreeList.Count);
        }

        [Fact]
        public static async Task RestoreUrls()
        {
            // prepare versions
            var docsetPath = "restore-urls";
            Directory.CreateDirectory(docsetPath);
            var url = "https://raw.githubusercontent.com/docascode/docfx-test-dependencies-clean/master/README.md";
            var restoreDir = AppData.GetFileDownloadDir(url);
            await ParallelUtility.ForEach(Enumerable.Range(0, 10), version =>
            {
                var restorePath = Path.Combine(restoreDir, version.ToString());
                PathUtility.CreateDirectoryFromFilePath(restorePath);
                File.WriteAllText(restorePath, $"{version}");
                File.SetLastWriteTimeUtc(restorePath, DateTime.UtcNow - TimeSpan.FromDays(20));
                return Task.CompletedTask;
            });

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
github:
  userCache: https://raw.githubusercontent.com/docascode/docfx-test-dependencies-clean/master/README.md");

            // run restore again to clean up
            await Program.Run(new[] { "restore", docsetPath });
            await Program.Run(new[] { "gc" });

            Assert.Single(Directory.EnumerateFiles(restoreDir, "*"));
        }

        [Theory]
        [InlineData("abc123", "\"0xdef456\"", "abc123+%220xdef456%22")]
        [InlineData("abc123", null, "abc123")]
        public static void GetRestoreFileName(string hash, string etag, string expected)
        {
            Assert.Equal(expected, RestoreFile.GetRestoreFileName(hash, etag == null ? null : new EntityTagHeaderValue(etag)));
        }

        [Theory]
        [InlineData("abc123+%220xdef456%22", "\"0xdef456\"")]
        [InlineData("abc123", null)]
        public static void GetEtag(string restoreFileName, string expected)
        {
            Assert.Equal(expected == null ? null : new EntityTagHeaderValue(expected), RestoreFile.GetEtag(restoreFileName));
        }

        private static void DeleteDir(string root)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            var dir = new DirectoryInfo(root);

            if (dir.Exists)
            {
                SetAttributesNormal(dir);
                dir.Delete(true);
            }

            void SetAttributesNormal(DirectoryInfo sub)
            {
                foreach (var subDir in sub.GetDirectories())
                {
                    SetAttributesNormal(subDir);
                    subDir.Attributes = FileAttributes.Normal;
                }
                foreach (var file in sub.GetFiles())
                {
                    file.Attributes = FileAttributes.Normal;
                }
            }
        }
    }
}
