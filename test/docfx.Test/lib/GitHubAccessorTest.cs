// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubAccessorTest
    {
        private readonly static string _token = Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN");
        private readonly static GitHubAccessor _github = new GitHubAccessor(_token);

        [Theory]
        [InlineData("OsmondJiang", 19990166)]
        [InlineData("N1o2t3E4x5i6s7t8N9a0m9e", null)]
        public async Task GetUserByLogin(string login, int? id)
        {
            var (error, profile) = await _github.GetUserByLogin(login);

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(_token))
            {
                Assert.Null(error?.Message);
                Assert.Equal(id, profile?.Id);
            }
        }

        [Theory]
        [InlineData("docascode", "docfx-test-dependencies", "c467c848311ccd2550fdb25a77ef26f9d8a33d00", false, "OsmondJiang", 19990166)]
        [InlineData("docascode", "docfx-test-dependencies", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", false, null, null)]
        [InlineData("docascode", "this-repo-does-not-exists", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", true, null, null)]
        public async Task GetUserByCommit(string repoOwner, string repoName, string commit, bool hasError, string login, int? id)
        {
            var (error, users) = await _github.GetUsersByCommit(repoOwner, repoName, commit);

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(_token))
            {
                if (hasError)
                    Assert.NotNull(error?.Message);
                else
                    Assert.Null(error?.Message);

                var user = users?.FirstOrDefault();
                Assert.Equal(login, user?.Login);
                Assert.Equal(id, user?.Id);
            }
        }
    }
}
