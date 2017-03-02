// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    using Xunit;

    public class MatcherTest
    {
        [Fact]
        public void TestCharMatcher()
        {
            var m = Matcher.Char('a');
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, true)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, false)));
            Assert.Equal(1, m.Match(new MatchContent("aabc", 1, true)));
            Assert.Equal(1, m.Match(new MatchContent("aabc", 1, false)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 2, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, true)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 3, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 2, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 2, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, false)));
        }

        [Fact]
        public void TestStringMatcher()
        {
            var m = Matcher.String("abc");
            Assert.Equal(3, m.Match(new MatchContent("abc", 0, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, false)));
            Assert.Equal(3, m.Match(new MatchContent("aabc", 1, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("aabc", 1, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, true)));
            Assert.Equal(3, m.Match(new MatchContent("cba", 3, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, false)));
        }

        [Fact]
        public void TestEofMatcher()
        {
            var m = Matcher.Eof();
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, true)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, false)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, false)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, true)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, false)));
        }
    }
}
