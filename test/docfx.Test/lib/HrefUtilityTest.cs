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
        [InlineData("a", true)]
        [InlineData("a/b", true)]
        [InlineData("a\\b", true, true)]
        [InlineData("/a", false)]
        [InlineData("\\a", false, true)]
        [InlineData("#a", false)]
        [InlineData("http://a", false)]
        [InlineData("https://a.com", false)]
        [InlineData("c:/a", false, true)]
        [InlineData("c:\\a", false, true)]
        public static void IsRelativeHref(string href, bool expected, bool windowsSpecific = false)
        {
            if (windowsSpecific && (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))) return;
            Assert.Equal(expected, HrefUtility.IsRelativeHref(href));
        }
    }
}
