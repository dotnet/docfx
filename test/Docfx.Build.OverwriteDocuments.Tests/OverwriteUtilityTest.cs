// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.OverwriteDocuments.Tests;

[TestClass]
public class OverwriteUtilityTest
{
    [TestMethod]
    public void ParseOPathTest()
    {
        var OPathString = "a/f[c= \"d\"]/g/b[c =\"d/d_d d\"]/e";
        var OPathSegments = OverwriteUtility.ParseOPath(OPathString);
        Assert.AreEqual(5, OPathSegments.Count);
        Assert.AreEqual("a,f,g,b,e", OPathSegments.Select(o => o.SegmentName).Aggregate((a, b) => a + "," + b));
        Assert.AreEqual("c", OPathSegments[1].Key);
        Assert.AreEqual("d", OPathSegments[1].Value);
        Assert.AreEqual("d/d_d d", OPathSegments[3].Value);
    }

    [TestMethod]
    [DataRow("abc[]d=\"e\"/g]")]
    [DataRow("abc[d='e']/g")]
    [DataRow("abc[d=\"e\"]/g/")]
    [DataRow("abc[d=2]/g/")]
    [DataRow("abc[d=true]/g/")]
    [DataRow("abc[a=\"b]\b")]
    [DataRow("abc/efg[a=\"b\"]")]
    [DataRow("abc/efg[a=\"b\"]e/g")]
    [DataRow("abc/[efg=\"hij\"]/e")]
    public void ParseInvalidOPathsTest(string OPathString)
    {
        var ex = Assert.Throws<ArgumentException>(() => OverwriteUtility.ParseOPath(OPathString));
        Assert.AreEqual($"{OPathString} is not a valid OPath", ex.Message);
    }
}
