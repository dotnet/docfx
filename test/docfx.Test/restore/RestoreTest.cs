// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public static class RestoreTest
{
    [Theory]
    [InlineData("'https://github.com/dotnet/docfx'", PackageType.Git, "https://github.com/dotnet/docfx", "main", "")]
    [InlineData("'https://github.com/dotnet/docfx/'", PackageType.Git, "https://github.com/dotnet/docfx", "main", "")]
    [InlineData("'https://visualstudio.com/dotnet/docfx'", PackageType.Git, "https://visualstudio.com/dotnet/docfx", "main", "")]
    [InlineData("'https://github.com/dotnet/docfx#master'", PackageType.Git, "https://github.com/dotnet/docfx", "master", "")]
    [InlineData("'https://github.com/dotnet/docfx#live'", PackageType.Git, "https://github.com/dotnet/docfx", "live", "")]
    [InlineData("'https://github.com/dotnet/docfx#'", PackageType.Git, "https://github.com/dotnet/docfx", "main", "")]
    [InlineData("'https://github.com/dotnet/docfx#986127a'", PackageType.Git, "https://github.com/dotnet/docfx", "986127a", "")]
    [InlineData("'https://github.com/dotnet/docfx#a#a'", PackageType.Git, "https://github.com/dotnet/docfx", "a#a", "")]
    [InlineData(@"'https://github.com/dotnet/docfx#a\\b/d<e>f*h|i%3C'", PackageType.Git, "https://github.com/dotnet/docfx", @"a\b/d<e>f*h|i%3C", "")]
    [InlineData("{'url': 'https://github.com/dotnet/docfx', 'branch': 'used'}", PackageType.Git, "https://github.com/dotnet/docfx", "used", "")]
    [InlineData("{'url': 'https://github.com/dotnet/docfx', 'path': 'a-path'}", PackageType.Git, "https://github.com/dotnet/docfx", "main", "")]
    [InlineData("{'url': 'https://github.com/dotnet/docfx#unused', 'branch': 'used'}", PackageType.Git, "https://github.com/dotnet/docfx#unused", "used", "")]
    [InlineData("{'url': 'crr/local-path'}", PackageType.Git, "crr/local-path", "main", "")]
    [InlineData("{'path': 'crr/local-path'}", PackageType.Folder, "", "main", "crr/local-path")]
    [InlineData("{}", PackageType.None, "", "main", "")]
    public static void PackageUrlTest(
        string json, PackageType expectedPackageType, string expectedUrl, string expectedBranch, string expectedPath)
    {
        // Act
        var packageUrl = JsonUtility.DeserializeData<PackagePath>(json.Replace('\'', '"'), new FilePath("file"));

        // Assert
        Assert.Equal(expectedUrl, packageUrl.Url);
        Assert.Equal(expectedPackageType, packageUrl.Type);
        Assert.Equal(expectedBranch, packageUrl.Branch);
        Assert.Equal(expectedPath, packageUrl.Path);
    }

    [Fact]
    public static void DownloadFile_Success()
    {
        Assert.NotNull(
            new FileResolver(new LocalPackage()).ReadString(
                new SourceInfo<string>("https://raw.githubusercontent.com/docascode/docfx-test-dependencies/master/README.md")));
    }

    [Fact]
    public static void DownloadFile_NoFetch_Should_Fail()
    {
        Assert.Throws<DocfxException>(() =>
            new FileResolver(new LocalPackage(), fetchOptions: FetchOptions.NoFetch).Download(
                new SourceInfo<string>("https://raw.githubusercontent.com/docascode/docfx-test-dependencies/master/dep.md")));
    }

    [Fact]
    public static void Download_Read_File_In_Parallel_Should_Succeed()
    {
        var fileResolver = new FileResolver(new LocalPackage(), fetchOptions: FetchOptions.Latest);

        Parallel.For(0, 10, i =>
        {
            Assert.NotEmpty(fileResolver.ReadString(
                new SourceInfo<string>("https://raw.githubusercontent.com/docascode/docfx-test-dependencies/master/extend1.yml")));
        });
    }

    [Fact]
    public static void RestoreAgainstFileShouldNotCrash()
    {
        Docfx.Run(new[] { "restore", "docfx.Test.dll" });
    }
}
