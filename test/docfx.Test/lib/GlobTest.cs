// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class GlobTest
    {
        [Theory]
        [InlineData("\\a", "a", false)]
        [InlineData("a*", "a abc abd abe", true)]
        [InlineData("**/a/*/b.cs", "b/a/a/a/b.cs", true)]

        // ** is a shortcut for **/*
        [InlineData("**", "a b abc bdir/cfile", true)]
        [InlineData("**/*", "a ab bdir/cfile a/b/c", true)]

        // Ignore files starting with dot
        [InlineData("**", ".git .git/a a/.git a\\.git", false)]

        // Do not support negate pattern
        [InlineData("!abc", "d dd def", false)]

        // To match folders, / should be explictly specified
        [InlineData("[a-c]b*", "abc abd abe bb cb", true)]
        [InlineData("[a-y]*[!c]", "abd abe bb bcd", true)]
        [InlineData("a*?c", "abc", true)]
        [InlineData("?*??", "abc", true)]
        [InlineData("*??", "abc", true)]
        [InlineData("**/*?c", "abc a/b/dec a/bcdc a/b/c/dc", true)]
        [InlineData("*c", "abc a/bc a/b/c", true)]
        [InlineData("*?", "abc a/b/c", true)]
        [InlineData("a/*", "a", false)]
        [InlineData("a/*", "a/.a", false)]
        [InlineData("*.cs", "acs", false)]

        // Expand groups
        [InlineData("a{b,c}d", "abd acd", true)]
        [InlineData("a{b,c}d", "a", false)]

        // File glob
        [InlineData("**/*.md", "a.md Root/J/K.md Root/M/N.md Root/M/L/O.md", true)]
        [InlineData("**/*.md", "Root/A.cs Root/B.cs Root/C/D.cs Root/E/F.cs", false)]
        [InlineData("**/J/**", "Root/J/K.md Root/J\\K.md", true)]
        [InlineData("**/J/**", "Root/JK/K.md Root/JK\\K.md", false)]
        [InlineData("**/[EJ]/*.{md,cs,csproj}", "Root/E/K.md Root/J\\K.cs", true)]
        [InlineData("**/[EJ]/*.{md,cs,csproj}", "Root/M/K.md Root/J\\K.csp", false)]

        public void MatchFilesUsingGlobPattern(string pattern, string files, bool match)
        {
            var glob = GlobUtility.CreateGlobMatcher(new[] { pattern }, Array.Empty<string>());
            foreach (var file in files.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                Assert.Equal(match, glob(file));
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("[^a-c]*")]
        [InlineData("[abc[]]a")]
        [InlineData("[a[]")]
        [InlineData("[(]")]
        [InlineData("[]]")]
        [InlineData("[")]
        [InlineData("[\\w]a")]
        [InlineData("a[\\\\b]c")]
        [InlineData("{{a,b}}")]
        [InlineData("z{a,b{,c}d")]
        [InlineData("**/")]
        [InlineData("\\[*")]
        [InlineData("a{b,c,d}e{d}{}")]
        [InlineData("b*/")]
        public void InvalidGlobPattern(string pattern)
        {
            Assert.Equal(
                "invalid-glob",
                Assert.Throws<DocfxException>(() => GlobUtility.CreateGlobMatcher(new[] { pattern }, Array.Empty<string>())).Error.Code);
        }
    }
}
