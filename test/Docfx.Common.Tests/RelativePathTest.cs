// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.Tests;

[TestProperty("Related", "RelativePath")]
[TestClass]
public class RelativePathTest
{
    [TestMethod]
    public void TestRelativePathWithBasicScenarios()
    {
        {
            var s = "../a/b.txt";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.ParentDirectoryCount);
            Assert.AreEqual(s, r.ToString());
            Assert.AreEqual(s, r);
        }
        {
            var s = "a.txt";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual(s, r.ToString());
            Assert.AreEqual(s, r);
        }
        {
            var s = "a.dir/";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual(s, r.ToString());
            Assert.AreEqual(s, r);
        }
        {
            var s = @"a\b\.\d\..\\c.dir\";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual("a/b/c.dir/", r.ToString());
            Assert.AreEqual("a/b/c.dir/", r);
        }
        {
            var s = "";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual(s, r.ToString());
            Assert.AreEqual(s, r);
            Assert.AreSame(RelativePath.Empty, r);
        }
        {
            var s = ".";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual(string.Empty, r.ToString());
            Assert.AreEqual(string.Empty, r);
            Assert.AreSame(RelativePath.Empty, r);
        }
        {
            var s = "a/../";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual(string.Empty, r.ToString());
            Assert.AreEqual(string.Empty, r);
            Assert.AreSame(RelativePath.Empty, r);
        }
    }

    [TestMethod]
    [DataRow("d/e.txt", "a/b/c/", "a/b/c/d/e.txt")]
    [DataRow("../d/e.txt", "a/b/c/", "a/b/d/e.txt")]
    [DataRow("d/e.txt", "a/b/c.txt", "a/b/d/e.txt")]
    [DataRow("../e.txt", "a/b/c.txt", "a/e.txt")]
    [DataRow("../e.txt", "../c.txt", "../../e.txt")]
    [DataRow("../a.txt", "", "../a.txt")]
    [DataRow("", "../a/b.txt", "../a/")]
    [DataRow("../", "a/", "")]
    [DataRow("", "", "")]
    [DataRow("~/a.txt", "a/b/c/", "~/a.txt")]
    [DataRow("d.txt", "~/a/b/c/", "~/a/b/c/d.txt")]
    public void TestRelativePathBasedOn(string thisPath, string basedOnPath, string expected)
    {
        var actual = ((RelativePath)thisPath).BasedOn((RelativePath)basedOnPath);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected, actual.ToString());
    }

    [TestMethod]
    [DataRow("a/b/c.txt", "d/e.txt", "../a/b/c.txt")]
    [DataRow("a/b/c.txt", "a/d.txt", "b/c.txt")]
    [DataRow("../../a.txt", "../b.txt", "../a.txt")]
    [DataRow("../../a.txt", "../b/c.txt", "../../a.txt")]
    [DataRow("a.txt", "../b.txt", null)]
    [DataRow("a/b.txt", "", "a/b.txt")]
    [DataRow("", "a/b.txt", "../")]
    [DataRow("a/", "a/", "")]
    [DataRow("", "", "")]
    [DataRow("~/a/b.txt", "~/a/c.txt", "b.txt")]
    [DataRow("~/a/b.txt", "a/c.txt", "~/a/b.txt")]
    public void TestRelativePathMakeRelativeTo(string thisPath, string relativeToPath, string expected)
    {
        try
        {
            var actual = ((RelativePath)thisPath).MakeRelativeTo((RelativePath)relativeToPath);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual.ToString());
        }
        catch (NotSupportedException)
        {
            if (expected != null)
            {
                throw;
            }
        }
    }

    [TestMethod]
    [DataRow("a/b/c", "a/b/")]
    [DataRow("~/a/b/c", "~/a/b/")]
    [DataRow("~/../a/b/c", "~/../a/b/")]
    [DataRow("~/../a/", "~/../a/")]
    [DataRow("~/../", "~/../")]
    [DataRow("~/a/../b", "~/")]
    public void TestRelativePathGetDirectoryPath(string file, string expected)
    {
        var relativePath = (RelativePath)file;
        var result = relativePath.GetDirectoryPath();
        Assert.AreEqual(expected, result.ToString());
    }

    [TestMethod]
    public void TestRelativePathGetDirectoryPathWithInvalidParentDirectoryShouldFail()
    {
        var relativePath = (RelativePath)"~/..";
        Assert.Throws<InvalidOperationException>(() => relativePath.GetDirectoryPath());
    }

    [TestMethod]
    public void TestRelativePathChangeFileNameWithInvalidFileNameShouldFail()
    {
        var relativePath = (RelativePath)"~/a/b/c";
        Assert.Throws<ArgumentException>(() => relativePath.ChangeFileName("d/"));
        Assert.Throws<ArgumentException>(() => relativePath.ChangeFileName(".."));
        Assert.Throws<ArgumentException>(() => relativePath.ChangeFileName("../d/"));
    }

    [TestMethod]
    [DataRow("a/b/c", "d", "a/b/d")]
    [DataRow("~/a/b/c", "d", "~/a/b/d")]
    [DataRow("../a/b/c", "d", "../a/b/d")]
    public void TestRelativePathChangeFileName(string file, string changedFileName, string expected)
    {
        var relativePath = (RelativePath)file;
        var result = relativePath.ChangeFileName(changedFileName);
        Assert.AreEqual(expected, result.ToString());
    }

    [TestMethod]
    public void TestRelativePathOperatorAdd()
    {
        // a/b/c/ + d/e.txt = a/b/c/d/e.txt
        {
            var r1 = (RelativePath)"a/b/c/";
            var r2 = (RelativePath)"d/e.txt";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("a/b/c/d/e.txt", r3.ToString());
        }
        // a/b/c/ + ../d/e.txt = a/b/d/e.txt
        {
            var r1 = (RelativePath)"a/b/c/";
            var r2 = (RelativePath)"../d/e.txt";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("a/b/d/e.txt", r3.ToString());
        }
        // a/b/c.txt + d/e.txt = a/b/d/e.txt
        {
            var r1 = (RelativePath)"a/b/c.txt";
            var r2 = (RelativePath)"d/e.txt";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("a/b/d/e.txt", r3.ToString());
        }
        // a/b/c.txt + ../e.txt = a/e.txt
        {
            var r1 = (RelativePath)"a/b/c.txt";
            var r2 = (RelativePath)"../e.txt";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("a/e.txt", r3.ToString());
        }
        // ../c.txt + ../e.txt = ../../e.txt
        {
            var r1 = (RelativePath)"../c.txt";
            var r2 = (RelativePath)"../e.txt";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(2, r3.ParentDirectoryCount);
            Assert.AreEqual("../../e.txt", r3.ToString());
        }
        // "" + ../a.txt = ../a.txt
        {
            var r1 = (RelativePath)"";
            var r2 = (RelativePath)"../a.txt";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(1, r3.ParentDirectoryCount);
            Assert.AreEqual("../a.txt", r3.ToString());
        }
        // ../a/b.txt + "" = ../a/
        {
            var r1 = (RelativePath)"../a/b.txt";
            var r2 = (RelativePath)"";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(1, r3.ParentDirectoryCount);
            Assert.AreEqual("../a/", r3.ToString());
        }
        // a/ + ../ = ""
        {
            var r1 = (RelativePath)"a/";
            var r2 = (RelativePath)"../";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("", r3.ToString());
            Assert.AreSame(RelativePath.Empty, r3);
        }
        // "" + "" = ""
        {
            var r1 = (RelativePath)"";
            var r2 = (RelativePath)"";
            var r3 = r1 + r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("", r3.ToString());
            Assert.AreSame(RelativePath.Empty, r1);
            Assert.AreSame(RelativePath.Empty, r2);
            Assert.AreSame(RelativePath.Empty, r3);
        }
    }

    [TestMethod]
    public void TestRelativePathOperatorSub()
    {
        // a/b/c.txt - d/e.txt = ../a/b/c.txt
        {
            var r1 = (RelativePath)"a/b/c.txt";
            var r2 = (RelativePath)"d/e.txt";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(1, r3.ParentDirectoryCount);
            Assert.AreEqual("../a/b/c.txt", r3.ToString());
        }
        // a/b/c.txt - a/d.txt = b/c.txt
        {
            var r1 = (RelativePath)"a/b/c.txt";
            var r2 = (RelativePath)"a/d.txt";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("b/c.txt", r3.ToString());
        }
        // ../../a.txt - ../b.txt = ../a.txt
        {
            var r1 = (RelativePath)"../../a.txt";
            var r2 = (RelativePath)"../b.txt";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(1, r3.ParentDirectoryCount);
            Assert.AreEqual("../a.txt", r3.ToString());
        }
        // ../../a.txt - ../b/c.txt = ../../a.txt
        {
            var r1 = (RelativePath)"../../a.txt";
            var r2 = (RelativePath)"../b/c.txt";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(2, r3.ParentDirectoryCount);
            Assert.AreEqual("../../a.txt", r3.ToString());
        }
        // a.txt - ../b.txt = Oop...
        {
            var r1 = (RelativePath)"a.txt";
            var r2 = (RelativePath)"../b.txt";
            Assert.Throws<NotSupportedException>(() => _ = r1 - r2);
        }
        // a/b.txt - "" = a/b.txt
        {
            var r1 = (RelativePath)"a/b.txt";
            var r2 = (RelativePath)"";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("a/b.txt", r3.ToString());
        }
        // "" - a/b.txt = ../
        {
            var r1 = (RelativePath)"";
            var r2 = (RelativePath)"a/b.txt";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(1, r3.ParentDirectoryCount);
            Assert.AreEqual("../", r3.ToString());
        }
        // "a/" - "a/" = ""
        {
            var r1 = (RelativePath)"a/";
            var r2 = (RelativePath)"a/";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("", r3.ToString());
            Assert.AreSame(RelativePath.Empty, r3);
        }
        // "" - "" = ""
        {
            var r1 = (RelativePath)"";
            var r2 = (RelativePath)"";
            var r3 = r1 - r2;
            Assert.IsNotNull(r3);
            Assert.AreEqual(0, r3.ParentDirectoryCount);
            Assert.AreEqual("", r3.ToString());
            Assert.AreSame(RelativePath.Empty, r1);
            Assert.AreSame(RelativePath.Empty, r2);
            Assert.AreSame(RelativePath.Empty, r3);
        }
    }

    [TestMethod]
    public void TestRelativePathRebase()
    {
        // a/b/c.txt rebase from x/y.txt to d/e.txt = ../x/a/b/c.txt
        {
            var r1 = (RelativePath)"a/b/c.txt";
            var from = (RelativePath)"x/y.txt";
            var to = (RelativePath)"d/e.txt";
            var r2 = r1.Rebase(from, to);
            Assert.IsNotNull(r2);
            Assert.AreEqual(1, r2.ParentDirectoryCount);
            Assert.AreEqual("../x/a/b/c.txt", r2.ToString());
        }
    }

    [TestMethod]
    public void TestRelativePathFromWorkingFolder()
    {
        {
            var s = "~/";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.IsTrue(r.IsFromWorkingFolder());
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual(s, r.ToString());
            Assert.AreEqual(s, r);
            Assert.AreSame(RelativePath.WorkingFolder, r);
        }
        {
            var s = "~/../a.txt";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.IsTrue(r.IsFromWorkingFolder());
            Assert.AreEqual(1, r.ParentDirectoryCount);
            Assert.AreEqual(s, r.ToString());
            Assert.AreEqual(s, r);
        }
        {
            var s = "~/a.dir/";
            var r = (RelativePath)s;
            Assert.IsNotNull(r);
            Assert.IsTrue(r.IsFromWorkingFolder());
            Assert.AreEqual(0, r.ParentDirectoryCount);
            Assert.AreEqual(s, r.ToString());
            Assert.AreEqual(s, r);
        }
    }

    [TestMethod]
    [DataRow("a/b/c", "a/b/c")]
    [DataRow("../a/b/c", "../a/b/c")]
    [DataRow("a/b/c d", "a/b/c%20d")]
    [DataRow("../a+b/c/d", "../a%2Bb/c/d")]
    [DataRow("a%3fb", "a%253fb")]
    public void TestUrlEncode(string path, string expected)
    {
        Assert.AreEqual(expected, ((RelativePath)path).UrlEncode());
    }

    [TestMethod]
    [DataRow("a/b/c", "a/b/c")]
    [DataRow("../a/b/c", "../a/b/c")]
    [DataRow("a/b/c%20d", "a/b/c d")]
    [DataRow("../a%2Bb/c/d", "../a+b/c/d")]
    [DataRow("a%253fb", "a%3fb")]
    [DataRow("a%2fb", "a%2fb")]
    [DataRow("%2A%2F%3A%3F%5C", "%2A%2F%3A%3F%5C")] //*/:?\
    [DataRow("%2a%2f%3a%3f%5c", "%2a%2f%3a%3f%5c")]
    public void TestUrlDecode(string path, string expected)
    {
        Assert.AreEqual(expected, ((RelativePath)path).UrlDecode());
    }

    [TestMethod]
    [DataRow("a/b/c", "a/b/", true)]
    [DataRow("~/a/b/c", "~/a/b/", true)]
    [DataRow("a/b/c", "~/a/b/", false)]
    [DataRow("~/a/b/c", "a/b/", false)]
    [DataRow("a/b", "a/b", false)]
    [DataRow("a/b/", "a/b", false)]
    [DataRow("a/b", "a/b/", false)]
    [DataRow("a/b/", "a/b/", true)]
    [DataRow("a/b/c", "a/b/c", false)]
    [DataRow("a/b/c", "a/b/c/d", false)]
    [DataRow("a/b/c", "a/b/d", false)]
    [DataRow("a/../b/c", "b/", true)]
    [DataRow("../a/b", "../a", false)]
    [DataRow("../a/b", "../", false)]
    [DataRow("../a/b", "../../a", false)]
    [DataRow("../../", "../", false)]
    [DataRow("../", "../../", false)]
    [DataRow("~/a/b", "~/../", false)]
    public void TestStartsWith(string source, string dest, bool isStartsWith)
    {
        Assert.AreEqual(isStartsWith, ((RelativePath)source).InDirectory((RelativePath)dest));
    }
}
