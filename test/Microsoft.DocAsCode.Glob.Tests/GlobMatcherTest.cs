// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Glob.Tests
{
    using Glob;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class GlobMatcherTest
    {
        [Theory]
        [Trait("Related", "Glob")]
        [InlineData("!!!!", false, "")]
        [InlineData("!!!!!abc", true, "abc")]
        [InlineData("abc", false, "abc")]
        public void TestNegateGlobShouldAllowMultipleNegateChars(string pattern, bool expectedNegate, string expected)
        {
            var negate = GlobMatcher.ParseNegate(ref pattern);
            Assert.Equal(expectedNegate, negate);
            Assert.Equal(expected, pattern);
        }

        [Theory]
        [Trait("Related", "Glob")]
        [InlineData(@"a\{b,c\}d", new string[] { "a{b,c}d" })]
        [InlineData("a{b,c}d", new string[] { "abd", "acd" })]
        [InlineData("a{b,c,d}e{d}{}", new string[] { "abed", "aced", "aded" })]
        [InlineData("{{a,b}}", new string[] { "a", "b" })]
        [InlineData("z{a,b{,c}d", new string[] { } )]
        [InlineData(@"a\{b,c}d", new string[] { })]
        public void TestGroupedGlobShouldExpand(string source, string[] expected)
        {
            var result = GlobMatcher.ExpandGroup(source);
            Assert.Equal(expected, result);
        }

        [Theory]
        [Trait("Related", "Glob")]
        [InlineData("", new string[]
        {
            ""
        }, true)]

        [InlineData("\\a", new string[]
        {
            "a"
        }, true)]
        [InlineData("a*", new string[] 
        {
            "a", "abc", "abd", "abe"
        }, true)]
        [InlineData(".a*", new string[]
        {
            ".a", ".abc", ".abd", ".abe"
        }, true)]
        [InlineData("b*/", new string[]
        {
            "bdir/"
        }, true)]
        [InlineData("**/a/*/b.cs", new string[]
        {
            "b/a/a/a/b.cs"
        }, true)]
        // ** is a shortcut for **/*
        [InlineData("**", new string[] 
        {
            "a", "b", "abc", "bdir/cfile"
        }, true)]

        [InlineData("A/**", new string[]
        {
            "A/"
        }, false)]
        // ** is a shortcut for **/*
        [InlineData("**/*", new string[]
        {
          "a", "ab", "bdir/cfile", "a/b/c"
        }, true)]

        [InlineData("**/*", new string[]
        {
           "abc/"
        }, false)]
        // To match folders, / should be explictly specified
        [InlineData("**/", new string[]
        {
            "bdir/", "bdir/cdir/"
        }, true)]
        [InlineData("[a-c]b*", new string[]
        {
            "abc", "abd", "abe", "bb", "cb"
        }, true)]
        [InlineData("[a-y]*[^c]", new string[]
        {
            "abd", "abe", "bb", "bcd"
        }, true)]
        [InlineData("!abc", new string[]
        {
            "d", "dd", "def"
        }, true)]
        [InlineData("[^a-c]*", new string[]
        {
            "d", "dd", "def"
        }, true)]

        [InlineData("a\\*b/*", new string[]
        {
            "a*b/ooo"
        }, true)]

        [InlineData("a\\*?/*", new string[]
        {
            "a*b/ooo"
        }, true)]
        [InlineData("a[\\\\b]c", new string[]
        {
            "abc"
        }, true)]
        [InlineData("*.\\*", new string[]
        {
            "r.*"
        }, true)]
        [InlineData("a******?c", new string[]
        {
            "abc"
        }, true)]
        [InlineData("?******??", new string[]
        {
            "abc"
        }, true)]
        [InlineData("*******??", new string[]
        {
            "abc"
        }, true)]
        [InlineData("***?***?c", new string[]
        {
            "abc", "a/b/dec", "a/bcdc"
        }, true)]
        [InlineData("***?***?c", new string[]
        {
            "a/b/c/dc"
        }, false)]
        [InlineData("*******c", new string[]
        {
            "abc", "a/bc", "a/b/c"
        }, true)]
        [InlineData("*******?", new string[]
        {
            "abc", "a/b/c"
        }, true)]
        [InlineData("[a[]", new string[]
        {
            "[", "a"
        }, true)]
        [InlineData("[(]", new string[]
        {
            "("
        }, true)]
        [InlineData("[]]", new string[]
        {
            "]"
        }, true)]
        [InlineData("[", new string[]
        {
            "["
        }, true)]
        [InlineData("[abc[]]a", new string[]
        {
            "a]a", "b]a", "[]a"
        }, true)]
        [InlineData("[\\w]a", new string[]
        {
            "aa", "ba"
        }, true)]
        [InlineData("[abc[]]a", new string[]
        {
            "]"
        }, false)]
        [InlineData(@"\[*", new string[]
        {
            "[abc"
        }, true)]
        [InlineData("b*/", new string[]
        {
            "bfile"
        }, false)]
        [InlineData("**", new string[]
        {
            ".a", "a/.b", ".a/b"
        }, false)]
        [InlineData("a/*", new string[]
        {
            "a/"
        }, false)]
        [InlineData("a/*", new string[]
        {
            "a/.a"
        }, false)]
        [InlineData("*.cs", new string[]
        {
            "acs"
        }, false)]
        public void TestGlobMatchWithoutDotMatchShouldMatchNonDotFiles(string pattern, string[] files, bool expected)
        {
            var glob = new GlobMatcher(pattern);
            foreach(var file in files)
            {
                var match = glob.Match(file);
                Assert.Equal(expected, match);
            }
        }

        [Theory]
        [InlineData("a/*", new string[]
        {
            "a/.a"
        }, true)]
        [InlineData("*/", new string[]
        {
            ".a/"
        }, true)]
        [InlineData("**", new string[]
        {
            ".a/.a"
        }, true)]
        [InlineData("**J/**", new string[]
        {
            "M", "M/JA", "a/b/c", "a/b/c.csproj"
        }, false)]
        [InlineData("**/A/**", new string[]
        {
            "A/B/C"
        }, true)]
        public void TestGlobMatchWithDotMatchShouldMatchDotFiles(string pattern, string[] files, bool expected)
        {
            var glob = new GlobMatcher(pattern, GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch);
            foreach (var file in files)
            {
                var match = glob.Match(file);
                Assert.Equal(expected, match);
            }
        }

        [Theory]
        [InlineData("**", new string[]
        {
            ".a/.a"
        }, true)]
        [InlineData("**.csproj", new string[]
        {
            ".a/", "a/", "a/a/", "a/.a/"
        }, true)]
        [InlineData("E/*.md", new string[]
        {
            "E/"
        }, true)]
        [InlineData("*.cs", new string[]
        {
            "a", "a.c"
        }, false)]
        [InlineData("**.md", new string[]
        {
            "Root/"
        }, true)]
        [InlineData("**", new string[]
        {
            "Root/"
        }, true)]
        // partial match must match folder ends with "/"
        [InlineData("**.md", new string[] {
            "a", "a/b"
        }, false)]

        // partial match must match folder ends with "/"
        [InlineData("b/", new string[] {
            "b/c/a"
        }, false)]
        public void TestGlobPartialMatchShouldMatchFolder(string pattern, string[] folders, bool expected)
        {
            var glob = new GlobMatcher(pattern, GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch);
            foreach (var file in folders)
            {
                var match = glob.Match(file, true);
                Assert.Equal(expected, match);
            }
        }
    }
}
