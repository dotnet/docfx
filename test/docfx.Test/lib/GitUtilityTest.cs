// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class GitUtilityTest
    {
        [Theory]
        [InlineData("README.md")]
        public static void GetRepoInfoSameAsGitExe(string file)
        {
            var repo = GitUtility.FindRepository(Path.GetFullPath(file));
            Assert.NotNull(repo);

            var (url, branch, commit) = GitUtility.GetRepoInfo(repo);
            Assert.NotNull(url);
            Assert.Equal(Exec("git", "rev-parse --abbrev-ref HEAD", repo), branch ?? "HEAD");
            Assert.Equal(Exec("git", "rev-parse HEAD", repo), commit);
        }

        [Theory]
        [InlineData("README.md")]
        public static void GetCommitsSameAsGitExe(string file)
        {
            var repo = Repository.Create(Path.GetFullPath(file), branch: null);
            Assert.NotNull(repo);

            using var gitCommitProvider = new FileCommitProvider(new ErrorLog(), repo, "git-commit-test-cache");
            var pathToRepo = PathUtility.NormalizeFile(file);

            // current branch
            var exe = Exec("git", $"--no-pager log --format=\"%H|%cI|%an|%ae\" -- \"{pathToRepo}\"", repo.Path);
            var lib = gitCommitProvider.GetCommitHistory(pathToRepo);

            Assert.Equal(
                exe.Replace("\r", ""),
                string.Join("\n", lib.Select(c => $"{c.Sha}|{c.Time.ToString("s")}{c.Time.ToString("zzz")}|{c.AuthorName}|{c.AuthorEmail}")));

            // another branch
            exe = Exec("git", $"--no-pager log --format=\"%H|%cI|%an|%ae\" a050eaf -- \"{pathToRepo}\"", repo.Path);
            lib = gitCommitProvider.GetCommitHistory(pathToRepo, "a050eaf");

            Assert.Equal(
                exe.Replace("\r", ""),
                string.Join("\n", lib.Select(c => $"{c.Sha}|{c.Time.ToString("s")}{c.Time.ToString("zzz")}|{c.AuthorName}|{c.AuthorEmail}")));

            gitCommitProvider.Save();
        }

        private static string Exec(string name, string args, string cwd)
        {
            var p = Process.Start(new ProcessStartInfo { FileName = name, Arguments = args, WorkingDirectory = cwd, RedirectStandardOutput = true });
            return p.StandardOutput.ReadToEnd().Trim();
        }
    }
}
