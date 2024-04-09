// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.Git;
using Xunit;

namespace Docfx.Common.Tests;

[Collection("docfx STA")]
public class GitUtilityWithSourceRepositoryUrlTest : IDisposable
{
    private readonly string _originalBranchName;
    private readonly string _originalSourceRepoUrl;

    private const string ORG_NAME = "dotnet";
    private const string REPO_NAME = "docfx";

    private const string BRANCH_NAME = "special-branch";
    private const string DOCFX_SOURCE_BRANCH_NAME = nameof(DOCFX_SOURCE_BRANCH_NAME);
    private const string DOCFX_SOURCE_REPOSITORY_URL = nameof(DOCFX_SOURCE_REPOSITORY_URL);

    public GitUtilityWithSourceRepositoryUrlTest()
    {
        _originalBranchName = Environment.GetEnvironmentVariable(DOCFX_SOURCE_BRANCH_NAME);
        _originalSourceRepoUrl = Environment.GetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY_URL);

        Environment.SetEnvironmentVariable(DOCFX_SOURCE_BRANCH_NAME, BRANCH_NAME);
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY_URL, $"https://github.com/{ORG_NAME}/{REPO_NAME}");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_BRANCH_NAME, _originalBranchName);
        Environment.SetEnvironmentVariable(DOCFX_SOURCE_REPOSITORY_URL, _originalSourceRepoUrl);
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
    [InlineData("git@github.com:user/repo.git", "main", "path/to/file.cs", 0, $"https://github.com/{ORG_NAME}/{REPO_NAME}/blob/main/path/to/file.cs")]
    public void GetSourceUrlTest_GitHub(string repo, string branch, string path, int line, string result)
    {
        Assert.Equal(result, GitUtility.GetSourceUrl(new(repo, branch, path, line)));
    }
}
