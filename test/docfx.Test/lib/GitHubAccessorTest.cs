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
            try
            {
                var profile = await _github.GetUserProfileByName("docascode");
                Assert.Equal("https://github.com/docascode", profile.ProfileUrl);
                Assert.Equal("DocFX", profile.DisplayName);
                Assert.Equal("docascode", profile.Name);
                Assert.Equal("14800732", profile.Id);
            }
            catch (DocfxInternalException) { }
        }

        [Fact]
        public async Task GetNameByCommitAsync()
        {
            try
            {
                var name = await _github.GetNameByCommit(
                    "docascode",
                    "docfx-test-dependencies",
                    "c467c848311ccd2550fdb25a77ef26f9d8a33d00");
                Assert.Equal("OsmondJiang", name);
            }
            catch (DocfxInternalException) { }
        }
    }
}
