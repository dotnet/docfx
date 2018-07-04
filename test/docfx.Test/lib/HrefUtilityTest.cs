// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public static class HrefUtilityTest
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
        public static void SplitHref(string href, string path, string query, string fragment)
        {
            var (apath, aquery, afragment) = HrefUtility.SplitHref(href);

            Assert.Equal(path, apath);
            Assert.Equal(query, aquery);
            Assert.Equal(fragment, afragment);
        }

        [Theory]
        [InlineData("a", false)]
        [InlineData("a/b", false)]
        [InlineData("a\\b", false)]
        [InlineData("/a", true)]
        [InlineData("\\a", true)]
        [InlineData("#a", false)]
        [InlineData("http://a", true)]
        [InlineData("https://a.com", true)]
        [InlineData("c:/a", true)]
        [InlineData("c:\\a", true)]
        public static void IsAbsolutePath(string href, bool expected)
        {
            Assert.Equal(expected, HrefUtility.IsAbsoluteHref(href));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("a&#/b\\.* d.png", "a%26%23/b/.%2A%20d.png")]
        public static void EscapeUrl(string path, string expected)
        {
            Assert.Equal(expected, HrefUtility.EscapeUrl(path));
        }
    }
}
