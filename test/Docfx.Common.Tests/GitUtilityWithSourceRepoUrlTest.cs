// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.Git;
using Xunit;

namespace Docfx.Common.Tests;

[Collection("docfx STA")]
public class GitUtilityWithSourceRepoUrlTest : IDisposable
{
    private readonly string _originalBranchName;
    private readonly string _originalSourceRepoUrl;

    private const string ORG_NAME = "dotnet";
    private const string REPO_NAME = "docfx";

    private const string BRANCH_NAME = "special-branch";
    private const string DOCFX_SOURCE_BRANCH_NAME = nameof(DOCFX_SOURCE_BRANCH_NAME);
    private const string DOCFX_SOURCE_REPOSITORY = nameof(DOCFX_SOURCE_REPOSITORY);

    public GitUtilityWithSourceRepoUrlTest()
    {
        _originalBranchName = Environment.GetEnvironmentVariable(DOCFX_SOURCE_BRANCH_NAME);
        _originalSourceRepoUrl = Environment.GetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY);

        Environment.SetEnvironmentVariable(DOCFX_SOURCE_BRANCH_NAME, BRANCH_NAME);
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY, $"{ORG_NAME}/{REPO_NAME}");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_BRANCH_NAME, _originalBranchName);
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY, _originalSourceRepoUrl);
    }

    [Fact]
    public void TryGetFileDetailTest()
    {
        var info = GitUtility.TryGetFileDetail(Directory.GetCurrentDirectory());
        Assert.Equal(BRANCH_NAME, info.Branch);
        Assert.Equal("https://github.com/dotnet/docfx", info.Repo);
    }

    [Fact]
    public void RawContentUrlToContentUrlTest()
    {
        string rawUrl = "https://raw.githubusercontent.com/dotnet/docfx/main/README.md";
        string expected = "https://github.com/dotnet/docfx/blob/special-branch/README.md";

        var result = GitUtility.RawContentUrlToContentUrl(rawUrl);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://github.com/user/repo.git", "main", "path/to/file.cs", 0, $"https://github.com/{ORG_NAME}/{REPO_NAME}/blob/main/path/to/file.cs")]
    [InlineData("https://github.com/user/repo.git", "main", "path/to/file.cs", 10, $"https://github.com/{ORG_NAME}/{REPO_NAME}/blob/main/path/to/file.cs#L10")]
    [InlineData("https://bitbucket.org/user/repo.git", "main", "path/to/file.cs", 0, $"https://bitbucket.org/{ORG_NAME}/{REPO_NAME}/src/main/path/to/file.cs")]
    [InlineData("https://bitbucket.org/user/repo.git", "main", "path/to/file.cs", 10, $"https://bitbucket.org/{ORG_NAME}/{REPO_NAME}/src/main/path/to/file.cs#lines-10")]
    [InlineData("https://dev.azure.com/user/repo/_git/repo", "main", "path/to/file.cs", 0, $"https://dev.azure.com/{ORG_NAME}/{REPO_NAME}/_git/repo?path=path/to/file.cs&version=GBmain")]
    [InlineData("https://dev.azure.com/user/repo/_git/repo", "0123456789abcdef0123456789abcdef01234567", "path/to/file.cs", 10, $"https://dev.azure.com/{ORG_NAME}/{REPO_NAME}/_git/repo?path=path/to/file.cs&version=GC0123456789abcdef0123456789abcdef01234567&line=10")]
    [InlineData("https://user.visualstudio.com/repo/_git/repo", "main", "path/to/file.cs", 0, $"https://{ORG_NAME}.visualstudio.com/{REPO_NAME}/_git/repo?path=path/to/file.cs&version=GBmain")]
    [InlineData("https://user.visualstudio.com/repo/_git/repo", "0123456789abcdef0123456789abcdef01234567", "path/to/file.cs", 10, $"https://{ORG_NAME}.visualstudio.com/{REPO_NAME}/_git/repo?path=path/to/file.cs&version=GC0123456789abcdef0123456789abcdef01234567&line=10")]
    [InlineData("git@github.com:user/repo.git", "main", "path/to/file.cs", 0, $"https://github.com/{ORG_NAME}/{REPO_NAME}/blob/main/path/to/file.cs")]
    [InlineData("ssh://mseng@vs-ssh.visualstudio.com:22/FakeProject/_ssh/Docfx", "main", "path/to/file.cs", 0, $"https://{ORG_NAME}.visualstudio.com/{REPO_NAME}/_ssh/Docfx?path=path/to/file.cs&version=GBmain")]
    public void GetSourceUrlTest(string repo, string branch, string path, int line, string result)
    {
        Assert.Equal(result, GitUtility.GetSourceUrl(new(repo, branch, path, line)));
    }
}
