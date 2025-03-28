// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Glob.Tests;

[TestClass]
public class GlobMatcherTest
{
    [TestMethod]
    [TestProperty("Related", "Glob")]
    [DataRow("!!!!", false, "")]
    [DataRow("!!!!!abc", true, "abc")]
    [DataRow("abc", false, "abc")]
    public void TestNegateGlobShouldAllowMultipleNegateChars(string pattern, bool expectedNegate, string expected)
    {
        var negate = GlobMatcher.ParseNegate(ref pattern);
        Assert.AreEqual(expectedNegate, negate);
        Assert.AreEqual(expected, pattern);
    }

    [TestMethod]
    [TestProperty("Related", "Glob")]
    [DataRow(@"a\{b,c\}d", new string[] { "a{b,c}d" })]
    [DataRow("a{b,c}d", new string[] { "abd", "acd" })]
    [DataRow("a{b,c,d}e{d}{}", new string[] { "abed", "aced", "aded" })]
    [DataRow("{{a,b}}", new string[] { "a", "b" })]
    [DataRow("z{a,b{,c}d", new string[] { })]
    [DataRow(@"a\{b,c}d", new string[] { })]
    public void TestGroupedGlobShouldExpand(string source, string[] expected)
    {
        var result = GlobMatcher.ExpandGroup(source);
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    [TestProperty("Related", "Glob")]
    [DataRow("", new string[]
    {
        ""
    }, true)]

    [DataRow("\\a", new string[]
    {
        "a"
    }, true)]
    [DataRow("a*", new string[]
    {
        "a", "abc", "abd", "abe"
    }, true)]
    [DataRow(".a*", new string[]
    {
        ".a", ".abc", ".abd", ".abe"
    }, true)]
    [DataRow("b*/", new string[]
    {
        "bdir/"
    }, true)]
    [DataRow("**/a/*/b.cs", new string[]
    {
        "b/a/a/a/b.cs"
    }, true)]
    // ** is a shortcut for **/*
    [DataRow("**", new string[]
    {
        "a", "b", "abc", "bdir/cfile"
    }, true)]

    [DataRow("A/**", new string[]
    {
        "A/"
    }, false)]
    // ** is a shortcut for **/*
    [DataRow("**/*", new string[]
    {
      "a", "ab", "bdir/cfile", "a/b/c"
    }, true)]

    [DataRow("**/*", new string[]
    {
       "abc/"
    }, false)]
    // To match folders, / should be explicitly specified
    [DataRow("**/", new string[]
    {
        "bdir/", "bdir/cdir/"
    }, true)]
    [DataRow("[a-c]b*", new string[]
    {
        "abc", "abd", "abe", "bb", "cb"
    }, true)]
    [DataRow("[a-y]*[^c]", new string[]
    {
        "abd", "abe", "bb", "bcd"
    }, true)]
    [DataRow("!abc", new string[]
    {
        "d", "dd", "def"
    }, true)]
    [DataRow("[^a-c]*", new string[]
    {
        "d", "dd", "def"
    }, true)]

    [DataRow("a\\*b/*", new string[]
    {
        "a*b/ooo"
    }, true)]

    [DataRow("a\\*?/*", new string[]
    {
        "a*b/ooo"
    }, true)]
    [DataRow("a[\\\\b]c", new string[]
    {
        "abc"
    }, true)]
    [DataRow("*.\\*", new string[]
    {
        "r.*"
    }, true)]
    [DataRow("a******?c", new string[]
    {
        "abc"
    }, true)]
    [DataRow("?******??", new string[]
    {
        "abc"
    }, true)]
    [DataRow("*******??", new string[]
    {
        "abc"
    }, true)]
    [DataRow("***?***?c", new string[]
    {
        "abc", "a/b/dec", "a/bcdc"
    }, true)]
    [DataRow("***?***?c", new string[]
    {
        "a/b/c/dc"
    }, false)]
    [DataRow("*******c", new string[]
    {
        "abc", "a/bc", "a/b/c"
    }, true)]
    [DataRow("*******?", new string[]
    {
        "abc", "a/b/c"
    }, true)]
    [DataRow("[a[]", new string[]
    {
        "[", "a"
    }, true)]
    [DataRow("[(]", new string[]
    {
        "("
    }, true)]
    [DataRow("[]]", new string[]
    {
        "]"
    }, true)]
    [DataRow("[", new string[]
    {
        "["
    }, true)]
    [DataRow("[abc[]]a", new string[]
    {
        "a]a", "b]a", "[]a"
    }, true)]
    [DataRow("[\\w]a", new string[]
    {
        "aa", "ba"
    }, true)]
    [DataRow("[abc[]]a", new string[]
    {
        "]"
    }, false)]
    [DataRow(@"\[*", new string[]
    {
        "[abc"
    }, true)]
    [DataRow("b*/", new string[]
    {
        "bfile"
    }, false)]
    [DataRow("**", new string[]
    {
        ".a", "a/.b", ".a/b"
    }, false)]
    [DataRow("a/*", new string[]
    {
        "a/"
    }, false)]
    [DataRow("a/*", new string[]
    {
        "a/.a"
    }, false)]
    [DataRow("*.cs", new string[]
    {
        "acs"
    }, false)]
    public void TestGlobMatchWithoutDotMatchShouldMatchNonDotFiles(string pattern, string[] files, bool expected)
    {
        var glob = new GlobMatcher(pattern);
        foreach (var file in files)
        {
            var match = glob.Match(file);
            Assert.AreEqual(expected, match);
        }
    }

    [TestMethod]
    [DataRow("a/*", new string[]
    {
        "a/.a"
    }, true)]
    [DataRow("*/", new string[]
    {
        ".a/"
    }, true)]
    [DataRow("**", new string[]
    {
        ".a/.a"
    }, true)]
    [DataRow("**J/**", new string[]
    {
        "M", "M/JA", "a/b/c", "a/b/c.csproj"
    }, false)]
    [DataRow("**/A/**", new string[]
    {
        "A/B/C"
    }, true)]
    public void TestGlobMatchWithDotMatchShouldMatchDotFiles(string pattern, string[] files, bool expected)
    {
        var glob = new GlobMatcher(pattern, GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch);
        foreach (var file in files)
        {
            var match = glob.Match(file);
            Assert.AreEqual(expected, match);
        }
    }

    [TestMethod]
    [DataRow("**", new string[]
    {
        ".a/.a"
    }, true)]
    [DataRow("**.csproj", new string[]
    {
        ".a/", "a/", "a/a/", "a/.a/"
    }, true)]
    [DataRow("E/*.md", new string[]
    {
        "E/"
    }, true)]
    [DataRow("*.cs", new string[]
    {
        "a", "a.c"
    }, false)]
    [DataRow("**.md", new string[]
    {
        "Root/"
    }, true)]
    [DataRow("**", new string[]
    {
        "Root/"
    }, true)]
    // partial match must match folder ends with "/"
    [DataRow("**.md", new string[] {
        "a", "a/b"
    }, false)]

    // partial match must match folder ends with "/"
    [DataRow("b/", new string[] {
        "b/c/a"
    }, false)]
    public void TestGlobPartialMatchShouldMatchFolder(string pattern, string[] folders, bool expected)
    {
        var glob = new GlobMatcher(pattern, GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch);
        foreach (var file in folders)
        {
            var match = glob.Match(file, true);
            Assert.AreEqual(expected, match);
        }
    }
}
