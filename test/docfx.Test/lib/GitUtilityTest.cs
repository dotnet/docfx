// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class GitUtilityTest
    {
        [Theory]
        [InlineData("README.md")]
        public static void GetRepoInfoSameAsGitExe(string file)
        {
            Assert.False(GitUtility.IsRepo(Path.GetFullPath(file)));

            var repo = GitUtility.FindRepo(Path.GetFullPath(file));
            Assert.NotNull(repo);

            var (remote, branch, commit) = GitUtility.GetRepoInfo(repo);
            Assert.Equal(Exec("git", "config --get remote.origin.url", repo), remote);
            Assert.Equal(Exec("git", "rev-parse --abbrev-ref HEAD", repo), branch ?? "HEAD");
            Assert.Equal(Exec("git", "rev-parse HEAD", repo), commit);
        }

        [Theory]
        [InlineData("README.md")]
        public static void GetCommitsSameAsGitExe(string file)
        {
            Assert.False(GitUtility.IsRepo(Path.GetFullPath(file)));

            var repo = GitUtility.FindRepo(Path.GetFullPath(file));
            Assert.NotNull(repo);

            var pathToRepo = PathUtility.NormalizeFile(file);

            var exe = Exec("git", $"--no-pager log --format=\"%H|%cI|%an|%ae\" -- \"{pathToRepo}\"", repo);
            var lib = GitUtility.GetCommits(repo, new List<string> { pathToRepo })[0].ToList();

            Assert.Equal(
                exe.Replace("\r", ""),
                string.Join("\n", lib.Select(c => $"{c.Sha}|{c.Time.ToString("s")}{c.Time.ToString("zzz")}|{c.AuthorName}|{c.AuthorEmail}")));
        }

        [Fact]
        public static async Task GitCommandConcurreny()
        {
            var cwd = GitUtility.FindRepo(Path.GetFullPath("README.md"));

            var results = await Task.WhenAll(Enumerable.Range(0, 10).AsParallel().Select(i => GitUtility.Revision(cwd)));

            Assert.True(results.All(r => r.Any()));
        }

        [Theory]
        [InlineData("https://github.com/docfx/docfx.git", "token", "https://token@github.com/docfx/docfx.git")]
        [InlineData("https://github.com/docfx/docfx.git", "", "https://github.com/docfx/docfx.git")]
        [InlineData("https://github.com/docfx/docfx.git", null, "https://github.com/docfx/docfx.git")]
        public static void AppTokenToRemoteUrl(string remote, string token, string expectedRemote)
            => Assert.Equal(expectedRemote, GitUtility.AppendToken(remote, token));

        private static string Exec(string name, string args, string cwd)
        {
            var p = Process.Start(new ProcessStartInfo { FileName = name, Arguments = args, WorkingDirectory = cwd, RedirectStandardOutput = true });
            p.WaitForExit();
            return p.StandardOutput.ReadToEnd().Trim();
        }
    }
}
