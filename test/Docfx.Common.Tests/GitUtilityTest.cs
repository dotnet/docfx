// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.Git;
using Xunit;

namespace Docfx.Common.Tests;

[Collection("docfx STA")]
public class GitUtilityTest : IDisposable
{
    private readonly string _originalBranchName;
    private const string EnvName = "DOCFX_SOURCE_BRANCH_NAME";
    public GitUtilityTest()
    {
        _originalBranchName = Environment.GetEnvironmentVariable(EnvName);
        Environment.SetEnvironmentVariable(EnvName, "special-branch");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvName, _originalBranchName);
    }

    [Fact]
    public void Environment_ForBranchName()
    {
        var info = GitUtility.TryGetFileDetail(Directory.GetCurrentDirectory());
        Assert.Equal("special-branch", info.Branch);
    }

    [Theory]
    [InlineData("https://github.com/user/repo.git", "main", "path/to/file.cs", 0, "https://github.com/user/repo/blob/main/path/to/file.cs")]
    [InlineData("https://github.com/user/repo.git", "main", "path/to/file.cs", 10, "https://github.com/user/repo/blob/main/path/to/file.cs#L10")]
    [InlineData("https://bitbucket.org/user/repo.git", "main", "path/to/file.cs", 0, "https://bitbucket.org/user/repo/src/main/path/to/file.cs")]
    [InlineData("https://bitbucket.org/user/repo.git", "main", "path/to/file.cs", 10, "https://bitbucket.org/user/repo/src/main/path/to/file.cs#lines-10")]
    [InlineData("https://dev.azure.com/user/repo/_git/repo", "main", "path/to/file.cs", 0, "https://dev.azure.com/user/repo/_git/repo?path=path/to/file.cs&version=GBmain")]
    [InlineData("https://dev.azure.com/user/repo/_git/repo", "0123456789abcdef0123456789abcdef01234567", "path/to/file.cs", 10, "https://dev.azure.com/user/repo/_git/repo?path=path/to/file.cs&version=GC0123456789abcdef0123456789abcdef01234567&line=10")]
    [InlineData("https://user.visualstudio.com/repo/_git/repo", "main", "path/to/file.cs", 0, "https://user.visualstudio.com/repo/_git/repo?path=path/to/file.cs&version=GBmain")]
    [InlineData("https://user.visualstudio.com/repo/_git/repo", "0123456789abcdef0123456789abcdef01234567", "path/to/file.cs", 10, "https://user.visualstudio.com/repo/_git/repo?path=path/to/file.cs&version=GC0123456789abcdef0123456789abcdef01234567&line=10")]
    [InlineData("git@github.com:user/repo.git", "main", "path/to/file.cs", 0, "https://github.com/user/repo/blob/main/path/to/file.cs")]
    [InlineData("ssh://mseng@vs-ssh.visualstudio.com:22/FakeProject/_ssh/Docfx", "main", "path/to/file.cs", 0, "https://vs-ssh.visualstudio.com/FakeProject/_ssh/Docfx?path=path/to/file.cs&version=GBmain")]
    public static void GetSourceUrlTest(string repo, string branch, string path, int line, string result)
    {
        Assert.Equal(result, GitUtility.GetSourceUrl(new(repo, branch, path, line)));
    }
}
