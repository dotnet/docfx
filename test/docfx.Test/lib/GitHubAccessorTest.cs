// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubAccessorTest
    {
        private GitHubAccessor _github = new GitHubAccessor();

        [Theory]
        [InlineData("docascode", null, 14800732)]
        [InlineData("N1o2t3E4x5i6s7t8N9a0m9e", "github-user-not-found", null)]
        public async Task GetUserByLogin(string login, string errorCode, int? id)
        {
            var (error, profile) = await _github.GetUserByLogin(login);

            // skip check if the machine exceeds the GitHub API rate limit
            if (error?.Code != "github-api-failed")
            {
                Assert.Equal(errorCode, error?.Code);
                Assert.Equal(id, profile?.Id);
            }
        }

        [Theory]
        [InlineData("docascode", "docfx-test-dependencies", "c467c848311ccd2550fdb25a77ef26f9d8a33d00", null, "OsmondJiang")]
        [InlineData("docascode", "docfx-test-dependencies", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", null, null)]
        public async Task GetLoginByCommit(string repoOwner, string repoName, string commit, string errorCode, string login)
        {
            var (error, name) = await _github.GetLoginByCommit(repoOwner, repoName, commit);

            // skip check if the machine exceeds the GitHub API rate limit
            if (error?.Code != "github-api-failed")
            {
                Assert.Equal(login, name);
                Assert.Equal(errorCode, error?.Code);
            }
        }
    }
}
