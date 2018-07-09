// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "vwxyzh")]
    public class UriUtilityTest
    {
        [InlineData("", "", "", "", "", "")]
        [InlineData("abc", "abc", "", "", "", "abc")]
        [InlineData("abc#def", "abc", "", "#def", "#def", "abc")]
        [InlineData("#def", "", "", "#def", "#def", "")]
        [InlineData("abc?def=ghi", "abc", "?def=ghi", "", "?def=ghi", "abc?def=ghi")]
        [InlineData("?def=ghi", "", "?def=ghi", "", "?def=ghi", "?def=ghi")]
        [InlineData("abc?def=ghi#jkl", "abc", "?def=ghi", "#jkl", "?def=ghi#jkl", "abc?def=ghi")]
        [InlineData("?def=ghi#jkl", "", "?def=ghi", "#jkl", "?def=ghi#jkl", "?def=ghi")]
        [InlineData("a#b#c", "a", "", "#b#c", "#b#c", "a")]
        [Theory]
        public void TestUriUtility(string input, string path, string queryString, string fragment, string queryStringAndFragment, string nonFragment)
        {
            Assert.Equal(path, UriUtility.GetPath(input));
            Assert.Equal(queryString, UriUtility.GetQueryString(input));
            Assert.Equal(fragment, UriUtility.GetFragment(input));
            Assert.Equal(queryStringAndFragment, UriUtility.GetQueryStringAndFragment(input));
            Assert.Equal(nonFragment, UriUtility.GetNonFragment(input));
        }

        [InlineData("#target", "#source", "#source")]
        [InlineData("", "#source", "#source")]
        [InlineData("#target", "", "#target")]
        [InlineData("", "", "")]
        [InlineData("?a=1#target", "?b=2#source", "?a=1&b=2#source")]
        [InlineData("?a=1&c=11#target", "?b=2&c=22#source", "?a=1&c=22&b=2#source")]
        [InlineData("?a=1", "#fragment", "?a=1#fragment")]
        [InlineData("#fragment", "?a=1", "?a=1#fragment")]
        [InlineData("a.html", "b.html", "b.html")]
        [InlineData("a.html?a=1&c=11#target", "b.html?b=2&c=22#source", "b.html?a=1&c=22&b=2#source")]
        [InlineData("a.html?a=1&c=11#target", "?b=2&c=22#source", "a.html?a=1&c=22&b=2#source")]
        [Theory]
        public void TestMergeHref(string target, string source, string expected)
        {
            Assert.Equal(expected, UriUtility.MergeHref(target, source));
        }
    }
}
