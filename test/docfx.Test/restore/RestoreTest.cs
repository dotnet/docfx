// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Docs.Build;
using Xunit;

namespace Microsoft.Docs.Test
{
    public static class RestoreTest
    {
        [Theory]
        [InlineData("https://github.com/dotnet/docfx", "github.com/dotnet/docfx", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://visualstudio.com/dotnet/docfx", "visualstudio.com/dotnet/docfx", "https://visualstudio.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#master", "github.com/dotnet/docfx", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#live", "github.com/dotnet/docfx", "https://github.com/dotnet/docfx", "live")]
        [InlineData("https://github.com/dotnet/docfx#", "github.com/dotnet/docfx", "https://github.com/dotnet/docfx", "master")]
        [InlineData("https://github.com/dotnet/docfx#986127a", "github.com/dotnet/docfx", "https://github.com/dotnet/docfx", "986127a")]
        public static void GetGitInfo(string remote, string expectedDir, string expectedUrl, string expectedRev)
        {
            // Act
            var (dir, url, rev) = Restore.GetGitInfo(remote);

            // Assert
            var restoreDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx", "git");
            Assert.Equal(PathUtility.NormalizeFolder(Path.Combine(restoreDir, expectedDir)), dir);
            Assert.Equal(expectedUrl, url);
            Assert.Equal(expectedRev, rev);
        }
    }
}
