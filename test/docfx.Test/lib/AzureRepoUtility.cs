// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class AzureRepoUtilityTest
    {
        [Theory]
        [InlineData("https://ceapex.visualstudio.com/", false, null, null)]
        [InlineData("http://ceapex.visualstudio.com/project", false, null, null)]
        [InlineData("http://ceapex.visualstudio.com/project/_git", false, null, null)]
        [InlineData("http://ceapex.visualstudio.com/project/repo", false, null, null)]
        [InlineData("http://ceapex.visualstudio.com/project/_git/repo/unknown", false, null, null)]
        [InlineData("http://ceapex.visualstudio.com/project/_git/repo#", false, null, null)]
        [InlineData("https://dev.azure.com/ceapex", false, null, null)]
        [InlineData("http://dev.azure.com/ceapex/project", false, null, null)]
        [InlineData("http://dev.azure.com/ceapex/project/_git", false, null, null)]
        [InlineData("http://dev.azure.com/ceapex/project/repo", false, null, null)]
        [InlineData("http://dev.azure.com/ceapex/project/_git/repo/unknown", false, null, null)]
        [InlineData("http://dev.azure.com/ceapex/project/_git/repo#", false, null, null)]
        [InlineData("http://ceapex.visualstudio.com/project/_git/repo/", true, "project", "repo")]
        [InlineData("https://ceapex.visualstudio.com/project/_git/repo", true, "project", "repo")]
        [InlineData("http://ceapex.visualstudio.com/project/_git/repo", true, "project", "repo")]
        [InlineData("http://ceapex.visualstudio.com/project/_git/repo#branch", true, "project", "repo")]
        [InlineData("https://ceapex.visualstudio.com/project/_git/repo#branch", true, "project", "repo")]
        [InlineData("https://ceapex.visualstudio.com/DefaultCollection/project/_git/repo", true, "project", "repo")]
        [InlineData("http://ceapex.visualstudio.com/DefaultCollection/project/_git/repo/", true, "project", "repo")]
        [InlineData("https://ceapex.visualstudio.com/DefaultCollection/project/_git/repo#branch", true, "project", "repo")]
        [InlineData("http://ceapex.visualstudio.com/DefaultCollection/project/_git/repo#branch", true, "project", "repo")]
        [InlineData("https://dev.azure.com/ceapex/project/_git/repo", true, "project", "repo")]
        [InlineData("http://dev.azure.com/ceapex/project/_git/repo/", true, "project", "repo")]
        [InlineData("https://dev.azure.com/ceapex/project/_git/repo#branch", true, "project", "repo")]
        [InlineData("http://dev.azure.com/ceapex/project/_git/repo#branch", true, "project", "repo")]
        public static void ParseAzureRepoRemote(string remote, bool parsed, string expectedProject, string expectedName)
        {
            if (AzureRepoUtility.TryParse(remote, out var owner, out var name))
            {
                Assert.True(parsed);
                Assert.Equal(expectedProject, owner);
                Assert.Equal(expectedName, name);
                return;
            }

            Assert.False(parsed);
        }
    }
}
