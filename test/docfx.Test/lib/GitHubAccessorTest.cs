// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GitHubAccessorTest
    {
        private readonly static string s_token = Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN");
        private readonly static Config s_config = JsonUtility.DeserializeData<Config>($@"{{'githubToken': '{s_token}'}}".Replace('\'', '\"'), null);
        private readonly static GitHubAccessor s_github = new GitHubAccessor(s_config);

        [Theory]
        [InlineData("OsmondJiang", 19990166, "Osmond Jiang", "OsmondJiang")]
        [InlineData("OPSTest", 23694395, "OPSTest", "OPSTest")]
        [InlineData("luyajun0205", 15990849, "luyajun0205", "luyajun0205")]
        [InlineData("N1o2t3E4x5i6s7t8N9a0m9e", null, null, null)]
        public static void GetUserByLogin(string login, int? expectedId, string expectedName, string expectedLogin)
        {
            var (error, user) = s_github.GetUserByLogin(new SourceInfo<string>(login));

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(s_token))
            {
                if (expectedId is null)
                {
                    Assert.NotNull(error);
                    Assert.Null(user);
                }
                else
                {
                    Assert.Null(error);
                    Assert.NotNull(user);
                    Assert.Equal(expectedId, user.Id);
                    Assert.Equal(expectedName, user.Name);
                    Assert.Equal(expectedLogin, user.Login);
                }
            }
        }

        [Theory]
        [InlineData("xinjiang@microsoft.com", "docascode", "docfx-test-dependencies", "c467c848311ccd2550fdb25a77ef26f9d8a33d00", "OsmondJiang", 19990166, "Osmond Jiang", new[] { "xinjiang@microsoft.com" })]
        [InlineData("opse2etestingppe@outlook.com", "OPS-E2E-PPE", "E2E_Contribution_DocfxV3", "e0f6bbdf1c8809562ca7ea1b3749660078143607", "OPSTestPPE", 26447601, "OPSTestPPE", new[] { "opse2etestingppe@outlook.com" })]
        [InlineData("v-yajlu@microsoft.com", "OPS-E2E-PPE", "E2E_Contribution_DocfxV3", "c2f754e529491f59a7ceaa1376308820ba05f586", "luyajun0205", 15990849, "luyajun0205", new[] { "v-yajlu@microsoft.com" })]
        [InlineData("test1@example.com", "docascode", "docfx-test-dependencies", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", null, null, null, null)]
        [InlineData("test2@example.com", "docascode", "contribution-test", "0000000000000000000000000000000000000000", null, null, null, null)]
        [InlineData("error@example.com", "docascode", "this-repo-does-not-exists", "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", null, null, null, null)]
        [InlineData("error@example.com", null, null, "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", null, null, null, null)]
        [InlineData("51308672+disabled-account-osmond@users.noreply.github.com", "docascode", "contribution-test", "b2b280fbc64790011c7a4d01bca5b84b6d98e386", null, null, null, null)]
        [InlineData("yufeih@live.com", "docascode", "contribution-test", "6d0e5bc3595e3841ac62dc545dfbb2c01fe64e7c", "yufeih", 511355, "Yufei Huang", new[] { "yufeih@live.com", "yufeih@microsoft.com" })]
        public static void GetUserByEmail(
            string email, string repoOwner, string repoName, string commit, string expectedLogin, int? expectedId, string expectedName, string[] expectedEmails)
        {
            var (error, user) = s_github.GetUserByEmail(email, repoOwner, repoName, commit);

            // skip check if the machine exceeds the GitHub API rate limit
            if (!string.IsNullOrEmpty(s_token))
            {
                Assert.Null(error);
                if (expectedId is null)
                {
                    Assert.Null(user);
                }
                else
                {
                    Assert.NotNull(user);
                    Assert.Equal(expectedLogin, user.Login);
                    Assert.Equal(expectedId, user.Id);
                    Assert.Equal(expectedName, user.Name);
                    Assert.Equal(expectedEmails, user.Emails);
                }
            }
        }
    }
}
