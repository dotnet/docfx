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
        [InlineData("a#b", "a", "", "b")]
        [InlineData("a#b<", "a", "", "b<")]
        [InlineData("a?c", "a", "c", "")]
        [InlineData("a?b#c", "a", "b", "c")]
        [InlineData("a#b?c=d", "a", "", "b?c=d")]
        [InlineData("a?b#c?d=e", "a", "b", "c?d=e")]
        [InlineData("a?b#c#d", "a", "b", "c#d")]
        public static void SplitHref(string href, string path, string query, string fragment)
        {
            var (apath, aquery, afragment) = HrefUtility.SplitHref(href);

            Assert.Equal(path, apath);
            Assert.Equal(query, aquery);
            Assert.Equal(fragment, afragment);
        }

        [Theory]
        [InlineData("", "", "", "")]
        [InlineData("", "b=1", "c", "?b=1#c")]
        [InlineData("a", "b=1", "c", "a?b=1#c")]
        [InlineData("a?b=1#c", "b=2", "c1", "a?b=2#c1")]
        [InlineData("a?b=1#c", "b1=1", "", "a?b=1&b1=1#c")]
        [InlineData("a?b=1#c", "", "c1", "a?b=1#c1")]
        public static void MergeHref(string href, string query, string fragment, string expected)
        {
            var result = HrefUtility.MergeHref(href, query, fragment);

            Assert.Equal(expected, result);
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
        public static void IsHttpHref(string href, bool expected)
            => Assert.Equal(expected, HrefUtility.IsHttpHref(href));

        [Theory]
        [InlineData("", "")]
        [InlineData("a&#/b\\.* d.png", "a%26%23/b/.%2A%20d.png")]
        public static void EscapeUrl(string path, string expected)
        {
            Assert.Equal(expected, HrefUtility.EscapeUrl(path));
        }

        [Theory]
        [InlineData("a&#/b\\.* d.png", "a%26%23%2Fb%5C.%2A%20d.png")]
        [InlineData("\"0x8D658F7F87AE5C5\"", "%220x8D658F7F87AE5C5%22")]
        public static void EscapeUrlSegment(string path, string expected)
        {
            Assert.Equal(expected, HrefUtility.EscapeUrlSegment(path));
        }

        [Theory]
        [InlineData("a%26%23/b/.%2A%20d.png", "a&#/b/.* d.png")]
        [InlineData("a%26%23%2Fb%5C.%2A%20d.png", "a&#/b\\.* d.png")]
        [InlineData("%220x8D658F7F87AE5C5%22", "\"0x8D658F7F87AE5C5\"")]
        public static void UnescapeUrl(string path, string expected)
        {
            Assert.Equal(expected, HrefUtility.UnescapeUrl(path));
        }
    }
}
