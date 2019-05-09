// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public static class UrlUtilityTest
    {
        [Theory]
        [InlineData("", "", "", "")]
        [InlineData("a", "a", "", "")]
        [InlineData("a#b", "a", "", "#b")]
        [InlineData("a#b<", "a", "", "#b<")]
        [InlineData("a?c", "a", "?c", "")]
        [InlineData("a?b#c", "a", "?b", "#c")]
        [InlineData("a#b?c=d", "a", "", "#b?c=d")]
        [InlineData("a?b#c?d=e", "a", "?b", "#c?d=e")]
        [InlineData("a?b#c#d", "a", "?b", "#c#d")]
        public static void SplitUrl(string url, string path, string query, string fragment)
        {
            var (apath, aquery, afragment) = UrlUtility.SplitUrl(url);

            Assert.Equal(path, apath);
            Assert.Equal(query, aquery);
            Assert.Equal(fragment, afragment);
        }

        [Theory]
        [InlineData("", "", "", "")]
        [InlineData("", "b", "c", "")]
        [InlineData("a", "b=1", "c", "a?b=1#c")]
        [InlineData("a", "", null, "a")]
        [InlineData("a?b=1#c", "b=2", "c1", "a?b=2#c1")]
        [InlineData("a?b=1#c", "b1=1", "", "a?b=1&b1=1#c")]
        [InlineData("a?b=1#c", "", "c1", "a?b=1#c1")]
        public static void MergeUrl(string url, string query, string fragment, string expected)
        {
            var result = UrlUtility.MergeUrl(url, query, fragment);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(@"", LinkType.RelativePath)]
        [InlineData(@"a", LinkType.RelativePath)]
        [InlineData(@"a/b", LinkType.RelativePath)]
        [InlineData(@"a\b", LinkType.RelativePath)]
        [InlineData(@"/", LinkType.AbsolutePath)]
        [InlineData(@"/a", LinkType.AbsolutePath)]
        [InlineData(@"\\a", LinkType.External)]
        [InlineData(@"//a", LinkType.External)]
        [InlineData(@"#", LinkType.SelfBookmark)]
        [InlineData(@"#a", LinkType.SelfBookmark)]
        [InlineData(@"http://a", LinkType.External)]
        [InlineData(@"https://a.com", LinkType.External)]
        [InlineData(@"http:a", LinkType.External)]
        [InlineData(@"feedback-url:?query=a", LinkType.External)]
        [InlineData(@"c:/a", LinkType.WindowsAbsolutePath)]
        [InlineData(@"c:\a", LinkType.WindowsAbsolutePath)]
        public static void GetLinkType(string url, LinkType expected)
        {
            Assert.Equal(expected, UrlUtility.GetLinkType(url));
        }

        [Theory]
        [InlineData("a", false)]
        [InlineData("a/b", false)]
        [InlineData("a\\b", false)]
        [InlineData("/a", false)]
        [InlineData("\\a", false)]
        [InlineData("#a", false)]
        [InlineData("c:/a", false)]
        [InlineData("c:\\a", false)]
        [InlineData("http://a", true)]
        [InlineData("http://////a", false)]
        [InlineData("https://a.com", true)]
        [InlineData("https://a.com#b", true)]
        [InlineData("https://////a.com", false)]
        public static void IsHttp(string url, bool expected)
            => Assert.Equal(expected, UrlUtility.IsHttp(url));

        [Theory]
        [InlineData("http://github.com/", false, null, null)]
        [InlineData("http://github.com/org", false, null, null)]
        [InlineData("http://github.com/org/name/unknown", false, null, null)]
        [InlineData("http://github.com/org/name#", false, null, null)]
        [InlineData("http://github.com/org/name/", true, "org", "name")]
        [InlineData("http://github.com/org/name", true, "org", "name")]
        [InlineData("http://github.com/org/name#branch", true, "org", "name")]
        [InlineData("https://github.com/org/name#branch", true, "org", "name")]
        public static void ParseGithubUrl(string remote, bool parsed, string expectedOwner, string expectedName)
        {
            if (UrlUtility.TryParseGitHubUrl(remote, out var owner, out var name))
            {
                Assert.True(parsed);
                Assert.Equal(expectedOwner, owner);
                Assert.Equal(expectedName, name);
                return;
            }

            Assert.False(parsed);
        }

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
        public static void ParseAzureReposUrl(string remote, bool parsed, string expectedProject, string expectedName)
        {
            if (UrlUtility.TryParseAzureReposUrl(remote, out var owner, out var name))
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
