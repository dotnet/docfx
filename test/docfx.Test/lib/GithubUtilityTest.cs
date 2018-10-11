// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class GithubUtilityTest
    {
        [Theory]
        [InlineData("http://github.com/", false,null, null)]
        [InlineData("http://github.com/org", false, null, null)]
        [InlineData("http://github.com/org/name/unknown", false, null, null)]
        [InlineData("http://github.com/org/name#", false, null, null)]
        [InlineData("http://github.com/org/name/", true, "org", "name")]
        [InlineData("http://github.com/org/name", true, "org", "name")]
        [InlineData("http://github.com/org/name#branch", true, "org", "name")]
        [InlineData("https://github.com/org/name#branch", true, "org", "name")]
        public static void ParseGithubRemote(string remote, bool parsed, string expectedOwner, string expectedName)
        {
            if (GithubUtility.TryParse(remote, out var repoInfo))
            {
                Assert.True(parsed);
                Assert.Equal(expectedOwner, repoInfo.owner);
                Assert.Equal(expectedName, repoInfo.name);
                return;
            }

            Assert.False(parsed);
        }
    }
}
