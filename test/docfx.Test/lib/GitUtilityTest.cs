// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
            Assert.Equal(Exec("config --get remote.origin.url"), remote);
            Assert.Equal(Exec("rev-parse --abbrev-ref HEAD"), branch ?? "HEAD");
            Assert.Equal(Exec("rev-parse HEAD"), commit);
        }

        [Theory]
        [InlineData("README.md")]
        public static async Task GetCommitsSameAsGitLog(string file)
        {
            Assert.False(GitUtility.IsRepo(Path.GetFullPath(file)));

            var repo = GitUtility.FindRepo(Path.GetFullPath(file));
            Assert.NotNull(repo);

            var pathToRepo = PathUtility.NormalizeFile(file);

            var exe = await GitUtility.GetCommits(repo, pathToRepo);
            var lib = GitUtility.GetCommits(repo, new List<string> { pathToRepo })[0].ToList();
            Assert.Equal(JsonConvert.SerializeObject(exe), JsonConvert.SerializeObject(lib));
        }

        [Fact]
        public static async Task GitCommandConcurreny()
        {
            var cwd = GitUtility.FindRepo(Path.GetFullPath("README.md"));

            var results = await Task.WhenAll(Enumerable.Range(0, 10).AsParallel().Select(i => GitUtility.HeadRevision(cwd)));

            Assert.True(results.All(r => r.Any()));
        }

        private static string Exec(string args)
        {
            var p = Process.Start(new ProcessStartInfo { FileName = "git", Arguments = args, RedirectStandardOutput = true });
            p.WaitForExit();
            return p.StandardOutput.ReadToEnd().Trim();
        }
    }
}
