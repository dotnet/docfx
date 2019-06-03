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
        [InlineData("OsmondJiang", 19990166, "Osmond Jiang", "OsmondJiang")]
        [InlineData("OPSTest", 23694395, "OPSTest", "OPSTest")]
        [InlineData("N1o2t3E4x5i6s7t8N9a0m9e", null, null, null)]
        public async Task GetUserByLogin(string login, int? expectedId, string expectedName, string expectedLogin)
        {
            var (error, profile) = await _github.GetUserByLogin(login);

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(_token))
            {
                Assert.Null(error?.Message);
                Assert.Equal(expectedId, profile?.Id);
                Assert.Equal(expectedName, profile?.Name);
                Assert.Equal(expectedLogin, profile?.Login);
            }
        }

        [Theory]
        [InlineData("docascode", "docfx-test-dependencies", "c467c848311ccd2550fdb25a77ef26f9d8a33d00", false, "OsmondJiang", 19990166, "Osmond Jiang", new[] { "xinjiang@microsoft.com" })]
        [InlineData("OPS-E2E-PPE", "E2E_Contribution_DocfxV3", "e0f6bbdf1c8809562ca7ea1b3749660078143607", false, "OPSTestPPE", 26447601, "OPSTestPPE", new[] { "opse2etestingppe@outlook.com" })]
        [InlineData("docascode", "docfx-test-dependencies", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", false, null, null, null, null)]
        [InlineData("docascode", "this-repo-does-not-exists", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", true, null, null, null, null)]
        public async Task GetUserByCommit(string repoOwner, string repoName, string commit, bool hasError, string login, int? id, string name, string[] emails)
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
                Assert.Equal(name, user?.Name);
                Assert.Equal(emails, user?.Emails);
            }
        }
    }
}
