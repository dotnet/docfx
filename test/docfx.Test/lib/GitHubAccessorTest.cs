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
        private readonly static string s_token = Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN");
        private readonly static GitHubAccessor s_github = new GitHubAccessor(s_token);

        [Theory]
        [InlineData("OsmondJiang", 19990166, "Osmond Jiang", "OsmondJiang")]
        [InlineData("OPSTest", 23694395, "OPSTest", "OPSTest")]
        [InlineData("luyajun0205", 15990849, "luyajun0205", "luyajun0205")]
        [InlineData("N1o2t3E4x5i6s7t8N9a0m9e", null, null, null)]
        public static async Task GetUserByLogin(string login, int? expectedId, string expectedName, string expectedLogin)
        {
            var (error, profile) = await s_github.GetUserByLogin(login);

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(s_token))
            {
                Assert.Null(error?.Message);
                Assert.Equal(expectedId, profile?.Id);
                Assert.Equal(expectedName, profile?.Name);
                Assert.Equal(expectedLogin, profile?.Login);
            }
        }

        [Theory]
        [InlineData("docascode", "docfx-test-dependencies", "c467c848311ccd2550fdb25a77ef26f9d8a33d00", "OsmondJiang", 19990166, "Osmond Jiang", new[] { "xinjiang@microsoft.com" })]
        [InlineData("OPS-E2E-PPE", "E2E_Contribution_DocfxV3", "e0f6bbdf1c8809562ca7ea1b3749660078143607", "OPSTestPPE", 26447601, "OPSTestPPE", new[] { "opse2etestingppe@outlook.com" })]
        [InlineData("OPS-E2E-PPE", "E2E_Contribution_DocfxV3", "c2f754e529491f59a7ceaa1376308820ba05f586", "luyajun0205", 15990849, "luyajun0205", new[] { "v-yajlu@microsoft.com" })]
        [InlineData("docascode", "docfx-test-dependencies", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", null, null, null, null)]
        [InlineData("docascode", "contribution-test", "0000000000000000000000000000000000000000", null, null, null, null)]
        [InlineData("docascode", "contribution-test", "b2b280fbc64790011c7a4d01bca5b84b6d98e386", null, null, null, new[] { "51308672+disabled-account-osmond@users.noreply.github.com" })]
        [InlineData("docascode", "contribution-test", "6d0e5bc3595e3841ac62dc545dfbb2c01fe64e7c", "yufeih", 511355, "Yufei Huang", new[] { "yufeih@live.com", "yufeih@microsoft.com" })]
        public static async Task GetUserByCommit(string repoOwner, string repoName, string commit, string login, int? id, string name, string[] emails)
        {
            var (error, users) = await s_github.GetUsersByCommit(repoOwner, repoName, commit);

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(s_token))
            {
                Assert.Null(error);

                var user = users?.FirstOrDefault();
                Assert.Equal(login, user?.Login);
                Assert.Equal(id, user?.Id);
                Assert.Equal(name, user?.Name);
                Assert.Equal(emails, user?.Emails);
            }
        }

        [Theory]
        [InlineData("docascode", "this-repo-does-not-exists", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef")]
        public static async Task GetUserByCommit_RepoNotFound(string repoOwner, string repoName, string commit)
        {
            var (error, _) = await s_github.GetUsersByCommit(repoOwner, repoName, commit);

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(s_token))
            {
                Assert.NotNull(error);
            }
        }
    }
}
