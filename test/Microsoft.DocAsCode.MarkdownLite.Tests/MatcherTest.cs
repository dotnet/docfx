// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    using Xunit;

    public class MatcherTest
    {
        [Fact]
        public void TestCharMatcher()
        {
            var m = Matcher.Char('a');
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("aabc", 1, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("aabc", 1, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharMatcher()
        {
            var m = Matcher.AnyChar;
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharInMatcher()
        {
            var m = Matcher.AnyCharIn('a', 'b');
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharInRangeMatcher()
        {
            var m = Matcher.AnyCharInRange('a', 'z');
            Assert.Equal(1, m.Match(new MatchContent("azX", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 0, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("azX", 1, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("azX", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 2, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("azX", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("azX", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestAnyCharNotInMatcher()
        {
            var m = Matcher.AnyCharNotIn('a', 'b');
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestStringMatcher()
        {
            var m = Matcher.String("abc");
            Assert.Equal(3, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("aabc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("aabc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("bbc", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("bbc", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestCaseInsensitiveStringMatcher()
        {
            var m = Matcher.CaseInsensitiveString("aBc");
            Assert.Equal(3, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("aBc", 0, MatchDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("ABC", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("aabc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("aabc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("Cba", 3, MatchDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("cbA", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cbb", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestEndOfStringMatcher()
        {
            var m = Matcher.EndOfString;
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestMaybeMatcher()
        {
            var m = Matcher.Maybe(Matcher.String("abc"));
            Assert.Equal(3, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(3, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(3, m.Match(new MatchContent("aabc", 1, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("aabc", 1, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestRepeatMatcher()
        {
            var m = Matcher.Repeat(Matcher.AnyCharInRange('a', 'b'), 1);
            Assert.Equal(2, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(2, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(2, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
        }

        [Fact]
        public void TestAnyMatcher()
        {
            var m = Matcher.Any(Matcher.Char('a'), Matcher.String("bc"));
            Assert.Equal(1, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(2, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(2, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
        }

        [Fact]
        public void TestSequenceMatcher()
        {
            var m = Matcher.Sequence(Matcher.Char('a'), Matcher.EndOfString);
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(1, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
        }

        [Fact]
        public void TestTestMatcher()
        {
            var m = Matcher.Test(Matcher.Char('a'));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
        }

        [Fact]
        public void TestNegativeTestMatcher()
        {
            var m = Matcher.NegativeTest(Matcher.Char('a'));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
        }

        [Fact]
        public void TestReverseTestMatcher()
        {
            var m = Matcher.ReverseTest(Matcher.Char('a'));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
        }

        [Fact]
        public void TestReverseNegativeTestMatcher()
        {
            var m = Matcher.ReverseNegativeTest(Matcher.Char('a'));
            Assert.Equal(0, m.Match(new MatchContent("abc", 0, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 0, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("abc", 1, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 1, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 2, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("abc", 3, MatchDirection.Backward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 3, MatchDirection.Forward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 3, MatchDirection.Backward)));
            Assert.Equal(0, m.Match(new MatchContent("cba", 2, MatchDirection.Forward)));
            Assert.Equal(Matcher.NotMatch, m.Match(new MatchContent("cba", 2, MatchDirection.Backward)));
        }

        [Fact]
        public void TestCaptureGroupMatcher()
        {
            var m = Matcher.CaptureGroup("g", Matcher.Repeat(Matcher.Char('a'), 1));
            {
                var c = new MatchContent("abc", 0, MatchDirection.Forward);
                Assert.Equal(1, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(1, g.Value.Count);
                Assert.Equal("a", g.Value.GetValue());
            }
            {
                var c = new MatchContent("abc", 0, MatchDirection.Backward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.Null(g);
            }
            {
                var c = new MatchContent("abc", 1, MatchDirection.Forward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.Null(g);
            }
            {
                var c = new MatchContent("abc", 1, MatchDirection.Backward);
                Assert.Equal(1, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(1, g.Value.Count);
                Assert.Equal("a", g.Value.GetValue());
            }
            {
                var c = new MatchContent("aaa", 0, MatchDirection.Forward);
                Assert.Equal(3, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(3, g.Value.Count);
                Assert.Equal("aaa", g.Value.GetValue());
            }
            {
                var c = new MatchContent("aaa", 3, MatchDirection.Backward);
                Assert.Equal(3, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(3, g.Value.Count);
                Assert.Equal("aaa", g.Value.GetValue());
            }
        }

        [Fact]
        public void TestBackReferenceMatcher()
        {
            var m = Matcher.Sequence(
                Matcher.CaptureGroup(
                    "g",
                    Matcher.Repeat(
                        Matcher.Char('a'),
                        1
                    )
                ),
                Matcher.Char('b'),
                Matcher.BackReference("g")
            );
            {
                var c = new MatchContent("abc", 0, MatchDirection.Forward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(1, g.Value.Count);
                Assert.Equal("a", g.Value.GetValue());
            }
            {
                var c = new MatchContent("abc", 0, MatchDirection.Backward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.Null(g);
            }
            {
                var c = new MatchContent("abc", 1, MatchDirection.Forward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.Null(g);
            }
            {
                var c = new MatchContent("abc", 1, MatchDirection.Backward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.Null(g);
            }
            {
                var c = new MatchContent("aba", 0, MatchDirection.Forward);
                Assert.Equal(3, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(1, g.Value.Count);
                Assert.Equal("a", g.Value.GetValue());
            }
            {
                var c = new MatchContent("aabaa", 0, MatchDirection.Forward);
                Assert.Equal(5, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(2, g.Value.Count);
                Assert.Equal("aa", g.Value.GetValue());
            }
            {
                var c = new MatchContent("aaba", 0, MatchDirection.Forward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(2, g.Value.Count);
                Assert.Equal("aa", g.Value.GetValue());
            }
            {
                var c = new MatchContent("aababb", 0, MatchDirection.Forward);
                Assert.Equal(Matcher.NotMatch, m.Match(c));
                var g = c.GetGroup("g");
                Assert.NotNull(g);
                Assert.Equal(0, g.Value.StartIndex);
                Assert.Equal(2, g.Value.Count);
                Assert.Equal("aa", g.Value.GetValue());
            }
        }

        [Fact]
        public void TestComplexMatcher()
        {
            var m = ((Matcher)"abc").Repeat(1, 2).ToGroup("g") + (Matcher.NewLine | Matcher.EndOfString).ToTest();
            {
                var result = m.Match("abc");
                Assert.NotNull(result);
                Assert.Equal(3, result.Length);
                Assert.NotNull(result["g"]);
                Assert.Equal(0, result["g"].StartIndex);
                Assert.Equal(3, result["g"].Count);
                Assert.Equal("abc", result["g"].GetValue());
            }
            {
                var result = m.Match("abcd");
                Assert.Null(result);
            }
            {
                var result = m.Match("abcabc");
                Assert.NotNull(result);
                Assert.Equal(6, result.Length);
                Assert.NotNull(result["g"]);
                Assert.Equal(0, result["g"].StartIndex);
                Assert.Equal(6, result["g"].Count);
                Assert.Equal("abcabc", result["g"].GetValue());
            }
            {
                var result = m.Match("abc\nd");
                Assert.NotNull(result);
                Assert.Equal(3, result.Length);
                Assert.NotNull(result["g"]);
                Assert.Equal(0, result["g"].StartIndex);
                Assert.Equal(3, result["g"].Count);
                Assert.Equal("abc", result["g"].GetValue());
            }
            {
                var result = m.Match("abcabcabc");
                Assert.Null(result);
            }
            {
                var result = m.Match("abc\nabcabc");
                Assert.NotNull(result);
                Assert.Equal(3, result.Length);
                Assert.NotNull(result["g"]);
                Assert.Equal(0, result["g"].StartIndex);
                Assert.Equal(3, result["g"].Count);
                Assert.Equal("abc", result["g"].GetValue());
            }
        }
    }
}
