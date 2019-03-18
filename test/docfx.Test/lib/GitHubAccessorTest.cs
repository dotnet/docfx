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
        [InlineData("docascode", 14800732)]
        [InlineData("N1o2t3E4x5i6s7t8N9a0m9e", null)]
        public async Task GetUserByLogin(string login, int? id)
        {
            var (error, profile) = await _github.GetUserByLogin(login);

            // skip check if the machine exceeds the GitHub API rate limit
            if (error is null)
            {
                Assert.Equal(id, profile?.Id);
            }
        }

        [Theory]
        [InlineData("docascode", "docfx-test-dependencies", "c467c848311ccd2550fdb25a77ef26f9d8a33d00", null, "OsmondJiang", 19990166)]
        [InlineData("docascode", "docfx-test-dependencies", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", null, null, null)]
        [InlineData("docascode", "this-repo-does-not-exists", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", null, null, null)]
        public async Task GetUserByCommit(string repoOwner, string repoName, string commit, string errorCode, string login, int? id)
        {
            var (error, user) = await _github.GetUserByCommit(repoOwner, repoName, commit);

            // skip check if the machine exceeds the GitHub API rate limit
            if (error?.Code != "github-api-failed")
            {
                Assert.Equal(login, user?.Login);
                Assert.Equal(id, user?.Id);
                Assert.Equal(errorCode, error?.Code);
            }
        }
    }
}
