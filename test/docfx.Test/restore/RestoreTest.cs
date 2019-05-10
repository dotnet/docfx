// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class RestoreTest
    {
        [Theory]
        [InlineData("https://github.com/dotnet/docfx", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx/", "https://github.com/dotnet/docfx", "master")]
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
            var (url, rev, _) = UrlUtility.SplitGitUrl(remote);

            // Assert
            Assert.Equal(expectedUrl, url);
            Assert.Equal(expectedRev, rev);
        }

        [Fact]
        public static async Task ForceAcquireNewWorkTree()
        {
            var docsetPath = "force-acquire-work-tree";
            var gitUrl = "https://github.com/docascode/docfx.test";

            Directory.CreateDirectory(docsetPath);

            var restoreDir = AppData.GetGitDir(gitUrl);
            DeleteDir(restoreDir);

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep: {gitUrl}#master");

            // acquire slot lock firestly
            ProcessUtility.AcquireExclusiveLock($"{gitUrl}/1", true);

            // run restroe and check the work trees
            await Program.Run(new[] { "restore", docsetPath });
            Assert.Equal(1, GetWorkTreeFolderCount(restoreDir));
        }

        [Fact]
        public static async Task RestoreGitWorkTrees()
        {
            var docsetPath = "restore-worktrees";
            var gitUrl = "https://github.com/docascode/docfx-test-dependencies-clean";

            Directory.CreateDirectory(docsetPath);

            var restoreDir = AppData.GetGitDir(gitUrl);
            DeleteDir(restoreDir);

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep5: {gitUrl}#master
  dep6: {gitUrl}#chi");

            // run restroe and check the work trees
            await Program.Run(new[] { "restore", docsetPath });
            Assert.Equal(2, GetWorkTreeFolderCount(restoreDir));

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep1: {gitUrl}#test-1-clean");

            // run restore again
            await Program.Run(new[] { "restore", docsetPath });

            // since the lockdown time works, new slot will be created
            Assert.Equal(3, GetWorkTreeFolderCount(restoreDir));

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep2: {gitUrl}#test-2-clean
  dep3: {gitUrl}#test-3-clean");

            // reset last access time
            // make one slot availabe for next restore
            var slots = DependencySlotPool<DependencyGit>.GetSlots(AppData.GetGitDir(gitUrl));
            Assert.True(slots.Count == 3);
            slots[1].LastAccessDate = DateTime.UtcNow - TimeSpan.FromDays(1);
            DependencySlotPool<DependencyGit>.WriteSlots(AppData.GetGitDir(gitUrl), slots);

            // run restore again
            // will create a new slot and find an available slot
            await Program.Run(new[] { "restore", docsetPath });
            Assert.Equal(4, GetWorkTreeFolderCount(restoreDir));

            // restore again to use existing worktree
            await Program.Run(new[] { "restore", docsetPath });
            Assert.Equal(4, GetWorkTreeFolderCount(restoreDir));

            // make worktree not synced with slots
            Exec("git", "worktree add 5", restoreDir);
            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
dependencies:
  dep1: {gitUrl}#test-4-clean");

            // run restore again
            // will create a new slot and find an available slot
            await Program.Run(new[] { "restore", docsetPath });
            Assert.Equal(5, GetWorkTreeFolderCount(restoreDir));
        }

        [Fact]
        public static async Task RestoreUrls()
        {
            // prepare versions
            var docsetPath = "restore-urls";
            if (Directory.Exists(docsetPath))
            {
                Directory.Delete(docsetPath, true);
            }
            Directory.CreateDirectory(docsetPath);
            var url = "https://raw.githubusercontent.com/docascode/docfx-test-dependencies-clean/master/README.md";
            var restoreDir = AppData.GetFileDownloadDir(url);

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
gitHub:
  userCache: https://raw.githubusercontent.com/docascode/docfx-test-dependencies-clean/master/README.md");

            // run restore
            await Program.Run(new[] { "restore", docsetPath });

            Assert.Equal(2, Directory.EnumerateFiles(restoreDir, "*").Count());

            // run restore again
            await Program.Run(new[] { "restore", docsetPath });

            Assert.Equal(2, Directory.EnumerateFiles(restoreDir, "*").Count());
        }

        private static int GetWorkTreeFolderCount(string path)
        => Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)
            .Where(c => Path.GetFileName(c) != ".git").Count();

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

        private static string Exec(string name, string args, string cwd)
        {
            var p = Process.Start(new ProcessStartInfo { FileName = name, Arguments = args, WorkingDirectory = cwd, RedirectStandardOutput = true });
            return p.StandardOutput.ReadToEnd().Trim();
        }
    }
}
