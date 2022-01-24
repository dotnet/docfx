// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Xunit;

namespace Microsoft.Docs.Build;

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

        using var errors = new ErrorWriter();
        using var gitCommitProvider = new GitCommitLoader(errors, repo, "git-commit-test-cache");
        var pathToRepo = PathUtility.NormalizeFile(file);

        // current branch
        var exe = Exec("git", $"--no-pager log --format=\"%H|%cI|%an|%ae\" -- \"{pathToRepo}\"", repo.Path);
        var lib = gitCommitProvider.GetCommitHistory(pathToRepo);

        Assert.Equal(
            exe.Replace("\r", ""),
            string.Join("\n", lib.Select(c => $"{c.Sha}|{c.Time:s}{c.Time:zzz}|{c.AuthorName}|{c.AuthorEmail}")));

        // another branch
        exe = Exec("git", $"--no-pager log --format=\"%H|%cI|%an|%ae\" a050eaf9 -- \"{pathToRepo}\"", repo.Path);
        lib = gitCommitProvider.GetCommitHistory(pathToRepo, "a050eaf9");

        Assert.Equal(
            exe.Replace("\r", ""),
            string.Join("\n", lib.Select(c => $"{c.Sha}|{c.Time:s}{c.Time:zzz}|{c.AuthorName}|{c.AuthorEmail}")));

        gitCommitProvider.Save();
    }

    [Theory]
    [InlineData("https://xxxxx@dev.azure.com/test-repo.git", "https://dev.azure.com/test-repo")]
    [InlineData("git@github.com/test-repo.git", "git@github.com/test-repo.git")]
    public static void NormalizeGitUrlTest(string url, string expected)
    {
        Assert.Equal(expected, GitUtility.NormalizeGitUrl(url));
    }

    private static string Exec(string name, string args, string cwd)
    {
        var p = Process.Start(new ProcessStartInfo { FileName = name, Arguments = args, WorkingDirectory = cwd, RedirectStandardOutput = true });
        return p.StandardOutput.ReadToEnd().Trim();
    }
}
