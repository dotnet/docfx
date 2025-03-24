// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.Git;

namespace Docfx.Common.Tests;

[DoNotParallelize]
[TestClass]
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

    [TestMethod]
    public void Environment_ForBranchName()
    {
        var info = GitUtility.TryGetFileDetail(Directory.GetCurrentDirectory());
        Assert.AreEqual("special-branch", info.Branch);
    }

    [TestMethod]
    [DataRow("https://github.com/user/repo.git", "main", "path/to/file.cs", 0, "https://github.com/user/repo/blob/main/path/to/file.cs")]
    [DataRow("https://github.com/user/repo.git", "main", "path/to/file.cs", 10, "https://github.com/user/repo/blob/main/path/to/file.cs#L10")]
    [DataRow("https://bitbucket.org/user/repo.git", "main", "path/to/file.cs", 0, "https://bitbucket.org/user/repo/src/main/path/to/file.cs")]
    [DataRow("https://bitbucket.org/user/repo.git", "main", "path/to/file.cs", 10, "https://bitbucket.org/user/repo/src/main/path/to/file.cs#lines-10")]
    [DataRow("https://dev.azure.com/user/repo/_git/repo", "main", "path/to/file.cs", 0, "https://dev.azure.com/user/repo/_git/repo?path=path/to/file.cs&version=GBmain")]
    [DataRow("https://dev.azure.com/user/repo/_git/repo", "0123456789abcdef0123456789abcdef01234567", "path/to/file.cs", 10, "https://dev.azure.com/user/repo/_git/repo?path=path/to/file.cs&version=GC0123456789abcdef0123456789abcdef01234567&line=10")]
    [DataRow("https://user.visualstudio.com/repo/_git/repo", "main", "path/to/file.cs", 0, "https://user.visualstudio.com/repo/_git/repo?path=path/to/file.cs&version=GBmain")]
    [DataRow("https://user.visualstudio.com/repo/_git/repo", "0123456789abcdef0123456789abcdef01234567", "path/to/file.cs", 10, "https://user.visualstudio.com/repo/_git/repo?path=path/to/file.cs&version=GC0123456789abcdef0123456789abcdef01234567&line=10")]
    [DataRow("git@github.com:user/repo.git", "main", "path/to/file.cs", 0, "https://github.com/user/repo/blob/main/path/to/file.cs")]
    [DataRow("ssh://mseng@vs-ssh.visualstudio.com:22/FakeProject/_ssh/Docfx", "main", "path/to/file.cs", 0, "https://vs-ssh.visualstudio.com/FakeProject/_ssh/Docfx?path=path/to/file.cs&version=GBmain")]
    public void GetSourceUrlTest(string repo, string branch, string path, int line, string result)
    {
        Assert.AreEqual(result, GitUtility.GetSourceUrl(new(repo, branch, path, line)));
    }
}
