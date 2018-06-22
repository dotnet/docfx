// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class HrefUtilityTest
    {
        [Theory]
        [InlineData("", "", "", "")]
        [InlineData("a", "a", "", "")]
        [InlineData("a#b", "a", "#b", "")]
        [InlineData("a#b<", "a", "#b<", "")]
        [InlineData("a?c", "a", "", "?c")]
        [InlineData("a?b#c", "a", "#c", "?b")]
        public static void SplitHref(string href, string path, string fragment, string query)
        {
            var (apath, afragment, aquery) = HrefUtility.SplitHref(href);

            Assert.Equal(path, apath);
            Assert.Equal(fragment, afragment);
            Assert.Equal(query, aquery);
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
    }
}
