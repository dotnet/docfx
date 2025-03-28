// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.Tests;

[TestClass]
public class UriUtilityTest
{
    [DataRow("", "", "", "", "", "")]
    [DataRow("abc", "abc", "", "", "", "abc")]
    [DataRow("abc#def", "abc", "", "#def", "#def", "abc")]
    [DataRow("#def", "", "", "#def", "#def", "")]
    [DataRow("abc?def=ghi", "abc", "?def=ghi", "", "?def=ghi", "abc?def=ghi")]
    [DataRow("?def=ghi", "", "?def=ghi", "", "?def=ghi", "?def=ghi")]
    [DataRow("abc?def=ghi#jkl", "abc", "?def=ghi", "#jkl", "?def=ghi#jkl", "abc?def=ghi")]
    [DataRow("?def=ghi#jkl", "", "?def=ghi", "#jkl", "?def=ghi#jkl", "?def=ghi")]
    [DataRow("a#b#c", "a", "", "#b#c", "#b#c", "a")]
    [TestMethod]
    public void TestUriUtility(string input, string path, string queryString, string fragment, string queryStringAndFragment, string nonFragment)
    {
        Assert.AreEqual(path, UriUtility.GetPath(input));
        Assert.AreEqual(queryString, UriUtility.GetQueryString(input));
        Assert.AreEqual(fragment, UriUtility.GetFragment(input));
        Assert.AreEqual(queryStringAndFragment, UriUtility.GetQueryStringAndFragment(input));
        Assert.AreEqual(nonFragment, UriUtility.GetNonFragment(input));
    }

    [DataRow("#target", "#source", "#source")]
    [DataRow("", "#source", "#source")]
    [DataRow("#target", "", "#target")]
    [DataRow("", "", "")]
    [DataRow("?a=1#target", "?b=2#source", "?a=1&b=2#source")]
    [DataRow("?a=1&c=11#target", "?b=2&c=22#source", "?a=1&c=22&b=2#source")]
    [DataRow("?a=1", "#fragment", "?a=1#fragment")]
    [DataRow("#fragment", "?a=1", "?a=1#fragment")]
    [DataRow("a.html", "b.html", "b.html")]
    [DataRow("a.html?a=1&c=11#target", "b.html?b=2&c=22#source", "b.html?a=1&c=22&b=2#source")]
    [DataRow("a.html?a=1&c=11#target", "?b=2&c=22#source", "a.html?a=1&c=22&b=2#source")]
    [TestMethod]
    public void TestMergeHref(string target, string source, string expected)
    {
        Assert.AreEqual(expected, UriUtility.MergeHref(target, source));
    }
}
