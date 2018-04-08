// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Docs.Test.lib
{
    public static class GitHubTest
    {
        [Theory]
        [InlineData("https://github.com/dotnet/corefx", "a76e40a75722be72cab9ce1eac66e6153aaffac2", "stoub@microsoft.com", "stephentoub")]
        [InlineData("https://github.com/MicrosoftDocs/azure-docs", "4224f3826ee0e4774fefef1a99b1558558c8f5fa", "tomfitz@microsoft.com", "tfitzmac")]
        public static async Task GetUsers(string remote, string commit, string authorEmail, string contributors)
        {
            var commits = new List<GitCommit> { new GitCommit { Sha = commit, AuthorEmail = authorEmail } };
            Assert.True(GitHub.TryParse(remote, out var info));
            Assert.Equal(contributors, string.Join(',', (await GitHub.GetUsers(info.owner, info.name, commits)).Select(u => u.Login)));
        }
    }
}
