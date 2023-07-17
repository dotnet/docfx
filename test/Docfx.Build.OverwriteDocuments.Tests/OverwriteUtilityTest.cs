// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Build.OverwriteDocuments.Tests;

public class OverwriteUtilityTest
{
    [Fact]
    public void ParseOPathTest()
    {
        var OPathString = "a/f[c= \"d\"]/g/b[c =\"d/d_d d\"]/e";
        var OPathSegments = OverwriteUtility.ParseOPath(OPathString);
        Assert.Equal(5, OPathSegments.Count);
        Assert.Equal("a,f,g,b,e", OPathSegments.Select(o => o.SegmentName).Aggregate((a, b) => a + "," + b));
        Assert.Equal("c", OPathSegments[1].Key);
        Assert.Equal("d", OPathSegments[1].Value);
        Assert.Equal("d/d_d d", OPathSegments[3].Value);
    }

    [Theory]
    [InlineData("abc[]d=\"e\"/g]")]
    [InlineData("abc[d='e']/g")]
    [InlineData("abc[d=\"e\"]/g/")]
    [InlineData("abc[d=2]/g/")]
    [InlineData("abc[d=true]/g/")]
    [InlineData("abc[a=\"b]\b")]
    [InlineData("abc/efg[a=\"b\"]")]
    [InlineData("abc/efg[a=\"b\"]e/g")]
    [InlineData("abc/[efg=\"hij\"]/e")]
    public void ParseInvalidOPathsTest(string OPathString)
    {
        var ex = Assert.Throws<ArgumentException>(() => OverwriteUtility.ParseOPath(OPathString));
        Assert.Equal($"{OPathString} is not a valid OPath", ex.Message);
    }
}
