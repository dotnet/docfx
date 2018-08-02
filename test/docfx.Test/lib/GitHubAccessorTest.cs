// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubAccessorTest
    {
        private readonly GitHubAccessor _github = new GitHubAccessor();

        [Fact]
        public async Task GetUserProfileByNameAsync()
        {
            var (errors, profile) = await _github.GetUserProfileByName("docascode");

            if (errors.Count == 0)
            {
                Assert.Equal("https://github.com/docascode", profile.ProfileUrl);
                Assert.Equal("DocFX", profile.DisplayName);
                Assert.Equal("docascode", profile.Name);
                Assert.Equal("14800732", profile.Id);
            }
            else
            {
                Assert.Equal("exceed-rate-limit", errors[0].Code);
            }
        }
    }
}
