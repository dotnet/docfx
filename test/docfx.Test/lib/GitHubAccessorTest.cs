// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubAccessorTest
    {
        private GitHubAccessor _github = new GitHubAccessor();

        [Fact]
        public async Task GetUserProfileByNameAsync()
        {
            var (error, profile) = await _github.GetUserProfileByName("docascode");
            if (error == null)
            {
                Assert.Equal("https://github.com/docascode", profile.ProfileUrl);
                Assert.Equal("DocFX", profile.DisplayName);
                Assert.Equal("docascode", profile.Name);
                Assert.Equal("14800732", profile.Id);
            }
            else
            {
                Assert.Equal("resolve-author-failed", error.Code);
            }
        }

        [Fact]
        public async Task GetNameByCommitAsync()
        {
            var (error, name) = await _github.GetNameByCommit(
                "docascode",
                "docfx-test-dependencies",
                "c467c848311ccd2550fdb25a77ef26f9d8a33d00");
            if (error == null)
            {
                Assert.Equal("OsmondJiang", name);
            }
            else
            {
                Assert.Equal("resolve-commit-failed", error.Code);
            }
        }
    }
}
