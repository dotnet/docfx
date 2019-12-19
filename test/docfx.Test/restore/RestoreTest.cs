// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class RestoreTest
    {
        [Theory]
        [InlineData("'https://github.com/dotnet/docfx'", PackageType.Git, "https://github.com/dotnet/docfx", "master", null)]
        [InlineData("'https://github.com/dotnet/docfx/'", PackageType.Git, "https://github.com/dotnet/docfx", "master", null)]
        [InlineData("'https://visualstudio.com/dotnet/docfx'", PackageType.Git, "https://visualstudio.com/dotnet/docfx", "master", null)]
        [InlineData("'https://github.com/dotnet/docfx#master'", PackageType.Git, "https://github.com/dotnet/docfx", "master", null)]
        [InlineData("'https://github.com/dotnet/docfx#live'", PackageType.Git, "https://github.com/dotnet/docfx", "live", null)]
        [InlineData("'https://github.com/dotnet/docfx#'", PackageType.Git, "https://github.com/dotnet/docfx", "master", null)]
        [InlineData("'https://github.com/dotnet/docfx#986127a'", PackageType.Git, "https://github.com/dotnet/docfx", "986127a", null)]
        [InlineData("'https://github.com/dotnet/docfx#a#a'", PackageType.Git, "https://github.com/dotnet/docfx", "a#a", null)]
        [InlineData(@"'https://github.com/dotnet/docfx#a\\b/d<e>f*h|i%3C'", PackageType.Git, "https://github.com/dotnet/docfx", @"a\b/d<e>f*h|i%3C", null)]
        [InlineData("{'url': 'https://github.com/dotnet/docfx', 'branch': 'used'}", PackageType.Git, "https://github.com/dotnet/docfx", "used", null)]
        [InlineData("{'url': 'https://github.com/dotnet/docfx', 'path': 'a-path'}", PackageType.Git, "https://github.com/dotnet/docfx", "master", null)]
        [InlineData("{'url': 'https://github.com/dotnet/docfx#unused', 'branch': 'used'}", PackageType.Git, "https://github.com/dotnet/docfx#unused", "used", null)]
        [InlineData("{'url': 'crr/local-path'}", PackageType.Git, "crr/local-path", "master", null)]
        [InlineData("{'path': 'crr/local-path'}", PackageType.Folder, null, null, "crr/local-path")]
        public static void PackageUrlTest(
            string json,
            PackageType expectedPackageType,
            string expectedUrl,
            string expectedBranch,
            string expectedPath)
        {
            // Act
            var packageUrl = JsonUtility.Deserialize<PackagePath>(json.Replace('\'', '"'), new FilePath("file"));

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
                new FileResolver(".", new Config()).ReadString(
                    new SourceInfo<string>("https://raw.githubusercontent.com/docascode/docfx-test-dependencies-clean/master/README.md")));
        }

        [Fact]
        public static async Task DownloadFile_NoFetch_Should_Fail()
        {
            await Assert.ThrowsAsync<DocfxException>(() =>
                new FileResolver(".", new Config(), noFetch: true).Download(
                    new SourceInfo<string>("https://raw.githubusercontent.com/docascode/docfx-test-dependencies-clean/master/README.md")));
        }

        [Fact]
        public static async Task RestoreAgainstFileShouldNotCrash()
        {
            await Docfx.Run(new [] { "restore", "docfx.Test.dll" });
        }
    }
}
