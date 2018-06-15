// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class RestoreTest
    {
        [Theory]
        [InlineData("https://github.com/dotnet/docfx", "github.com/dotnet/docfx/master", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://visualstudio.com/dotnet/docfx", "visualstudio.com/dotnet/docfx/master", "https://visualstudio.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#master", "github.com/dotnet/docfx/master", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#live", "github.com/dotnet/docfx/live", "https://github.com/dotnet/docfx", "live")]
        [InlineData("https://github.com/dotnet/docfx#", "github.com/dotnet/docfx/master", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#986127a", "github.com/dotnet/docfx/986127a", "https://github.com/dotnet/docfx", "986127a")]
        [InlineData("https://github.com/dotnet/docfx#a#a", "github.com/dotnet/docfx/a%23a", "https://github.com/dotnet/docfx", "a#a")]
        [InlineData("https://github.com/dotnet/docfx#a\\b/d<e>f*h|i%3C", "github.com/dotnet/docfx/a%5Cb%2Fd%3Ce%3Ef%2Ah%7Ci%253C", "https://github.com/dotnet/docfx", "a\\b/d<e>f*h|i%3C")]
        public static void GetGitInfo(string remote, string expectedDir, string expectedUrl, string expectedRev)
        {
            // Act
            var (dir, url, rev) = Restore.GetGitRestoreInfo(remote);

            // Assert
            var restoreDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx", "git");
            Assert.Equal(PathUtility.NormalizeFolder(Path.Combine(restoreDir, expectedDir)), dir);
            Assert.Equal(expectedUrl, url);
            Assert.Equal(expectedRev, rev);
        }
    }
}
