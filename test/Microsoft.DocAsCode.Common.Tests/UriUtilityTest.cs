// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;

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

        [Fact]
        public void TestIsAbsoluteUri()
        {
            Assert.True(UriUtility.IsAbsoluteUri("http://www.contoso.com/"));
            Assert.True(UriUtility.IsAbsoluteUri("ftp://example.org/resource.txt"));
            Assert.True(UriUtility.IsAbsoluteUri("urn:ISSN:1535-3613"));

            Assert.False(UriUtility.IsAbsoluteUri("relative/path/to/resource.txt"));
            Assert.False(UriUtility.IsAbsoluteUri("#frag01"));
            Assert.False(UriUtility.IsAbsoluteUri(string.Empty));
        }

        [Fact]
        public void TestIsAbsolutePath()
        {
            Assert.True(UriUtility.IsAbsolutePath("/catalog/shownew.htm"));

            Assert.False(UriUtility.IsAbsolutePath("http://www.contoso.com/"));
            Assert.False(UriUtility.IsAbsolutePath("ftp://example.org/resource.txt"));
            Assert.False(UriUtility.IsAbsolutePath("urn:ISSN:1535-3613"));
            Assert.False(UriUtility.IsAbsolutePath("#frag01"));
            Assert.False(UriUtility.IsAbsolutePath(string.Empty));
        }
    }
}
