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
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("aabc", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("aabc", 1, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharMatcher()
        {
            var m = Matcher.AnyChar();
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharInMatcher()
        {
            var m = Matcher.AnyCharIn('a', 'b');
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharInRangeMatcher()
        {
            var m = Matcher.AnyCharInRange('a', 'z');
            Assert.Equal(1, m.Match(new MatchContent("azX", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 0, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("azX", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("azX", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 2, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("azX", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharNotInMatcher()
        {
            var m = Matcher.AnyCharNotIn('a', 'b');
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestStringMatcher()
        {
            var m = Matcher.String("abc");
            Assert.Equal(3, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("aabc", 1, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("aabc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestEndOfStringMatcher()
        {
            var m = Matcher.EndOfString();
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestMaybeMatcher()
        {
            var m = Matcher.Maybe(Matcher.String("abc"));
            Assert.Equal(3, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("aabc", 1, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("aabc", 1, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestRepeatMatcher()
        {
            var m = Matcher.Repeat(Matcher.AnyCharInRange('a', 'b'), 1);
            Assert.Equal(2, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(2, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(2, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
        }

        [Fact]
        public void TestAnyMatcher()
        {
            var m = Matcher.Any(Matcher.Char('a'), Matcher.String("bc"));
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(2, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(2, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
        }

        [Fact]
        public void TestSequenceMatcher()
        {
            var m = Matcher.Sequence(Matcher.Char('a'), Matcher.EndOfString());
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
        }

        [Fact]
        public void TestTestMatcher()
        {
            var m = Matcher.Test(Matcher.Char('a'));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
        }

        [Fact]
        public void TestNegativeTestMatcher()
        {
            var m = Matcher.NegativeTest(Matcher.Char('a'));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
        }

        [Fact]
        public void TestReverseTestMatcher()
        {
            var m = Matcher.ReverseTest(Matcher.Char('a'));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
        }

        [Fact]
        public void TestReverseNegativeTestMatcher()
        {
            var m = Matcher.ReverseNegativeTest(Matcher.Char('a'));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, ScanDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, ScanDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, ScanDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, ScanDirection.Backward)));
        }

        [Fact]
        public void TestComplexMatcher()
        {
            var m = Matcher.Sequence(
                Matcher.Repeat(
                    Matcher.String("abc"),
                    1,
                    2),
                Matcher.Test(
                    Matcher.Any(
                        Matcher.Char('\n'),
                        Matcher.EndOfString()
                    )
                )
            );
            Assert.Equal(3, m.Match(new MatchContent("abc", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abcd", 0, ScanDirection.Forward)));
            Assert.Equal(6, m.Match(new MatchContent("abcabc", 0, ScanDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("abc\nd", 0, ScanDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abcabcabc", 0, ScanDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("abc\nabcabc", 0, ScanDirection.Forward)));
        }
    }
}
