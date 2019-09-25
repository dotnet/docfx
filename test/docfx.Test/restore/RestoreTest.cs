// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YamlDotNet.Helpers;

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
        [InlineData("{'url': 'https://github.com/dotnet/docfx#unused', 'branch': 'used'}", PackageType.Git, "https://github.com/dotnet/docfx", "used", null)]
        [InlineData("{'url': 'crr/local-path'}", PackageType.Folder, "crr/local-path", null, "crr/local-path")]
        public static void PackageUrlTest(
            string json,
            PackageType expectedPackageType,
            string expectedUrl,
            string expectedRev,
            string expectedPath)
        {
            // Act
            var packageUrl = JsonUtility.Deserialize<PackageUrl>(json.Replace('\'', '"'), new FilePath("file"));

            // Assert
            Assert.Equal(expectedUrl, packageUrl.Url);
            Assert.Equal(expectedPackageType, packageUrl.Type);
            Assert.Equal(expectedRev, packageUrl.Branch);
            Assert.Equal(expectedPath, packageUrl.Path);
        }

        [Fact]
        public static async Task RestoreUrls()
        {
            // prepare versions
            var docsetPath = "restore-urls";
            if (Directory.Exists(docsetPath))
            {
                Directory.Delete(docsetPath, true);
            }
            Directory.CreateDirectory(docsetPath);
            var url = "https://raw.githubusercontent.com/docascode/docfx-test-dependencies-clean/master/README.md";
            var restoreDir = AppData.GetFileDownloadDir(url);

            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), $@"
monikerDefinition: {url}");

            // run restore
            await Docfx.Run(new[] { "restore", docsetPath });

            Assert.Equal(2, Directory.EnumerateFiles(restoreDir, "*").Count());

            // run restore again
            var filePath = RestoreFile.GetRestorePathFromUrl(url);
            var etagPath = RestoreFile.GetRestoreEtagPath(url);

            File.Delete(etagPath);
            File.WriteAllText(filePath, "1");
            await Docfx.Run(new[] { "restore", docsetPath });

            Assert.Equal(2, Directory.EnumerateFiles(restoreDir, "*").Count());
            Assert.NotEqual("1", File.ReadAllText(filePath));
        }
    }
}
