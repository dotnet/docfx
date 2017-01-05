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
    }
}
