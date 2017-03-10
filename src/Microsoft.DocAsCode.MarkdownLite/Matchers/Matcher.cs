// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;
    using System.Linq;

    public abstract class Matcher
    {
        public const int NotMatch = -1;

        private static readonly AnyCharMatcher AnyCharMatcher = new AnyCharMatcher();
        private static readonly EndOfStringMatcher EndOfStringMatcher = new EndOfStringMatcher();

        /// <summary>
        /// Match string in content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>Char count of match, <c>-1</c> is not match.</returns>
        public abstract int Match(MatchContent content);

        public static Matcher Char(char ch)
        {
            return new CharMatcher(ch);
        }

        public static Matcher AnyChar() => AnyCharMatcher;

        public static Matcher AnyCharIn(params char[] ch)
        {
            if (ch == null)
            {
                throw new ArgumentNullException(nameof(ch));
            }
            if (ch.Length == 1)
            {
                return new CharMatcher(ch[0]);
            }
            var array = (char[])ch.Clone();
            Array.Sort(array);
            return new AnyCharInMatcher(ch);
        }

        public static Matcher AnyCharInRange(char start, char end)
        {
            if (start > end)
            {
                throw new ArgumentException(nameof(end), $"Should be greater than {start.ToString()}.");
            }
            if (start == end)
            {
                return new CharMatcher(start);
            }
            return new AnyCharInRangeMatcher(start, end);
        }

        public static Matcher AnyCharNotIn(params char[] ch)
        {
            if (ch == null)
            {
                throw new ArgumentNullException(nameof(ch));
            }
            var array = (char[])ch.Clone();
            Array.Sort(array);
            return new AnyCharNotInMatcher(ch);
        }

        public static Matcher String(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (text.Length == 0)
            {
                throw new ArgumentException("Cannot be empty.", nameof(text));
            }
            return new StringMatcher(text);
        }

        public static Matcher CaseInsensitiveString(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (text.Length == 0)
            {
                throw new ArgumentException("Cannot be empty.", nameof(text));
            }
            return new CaseInsensitiveStringMatcher(text);
        }

        public static Matcher EndOfString() => EndOfStringMatcher;

        public static Matcher Maybe(Matcher matcher) =>
            Repeat(matcher, 0, 1);

        public static Matcher Repeat(Matcher matcher, int minOccur) =>
            Repeat(matcher, minOccur, int.MaxValue);

        public static Matcher Repeat(Matcher matcher, int minOccur, int maxOccur)
        {
            if (matcher == null)
            {
                throw new ArgumentNullException(nameof(matcher));
            }
            if (minOccur < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minOccur), "Should be greater than or equals 0.");
            }
            if (minOccur > maxOccur)
            {
                throw new ArgumentOutOfRangeException(nameof(maxOccur), "Should be greater than or equals minOccur.");
            }
            return new RepeatMatcher(matcher, minOccur, maxOccur);
        }

        public static Matcher Any(params Matcher[] matchers)
        {
            ValidateMatcherArray(matchers);
            return new AnyMatcher(matchers);
        }

        public static Matcher Sequence(params Matcher[] matchers)
        {
            ValidateMatcherArray(matchers);
            return new SequenceMatcher(matchers);
        }

        public static Matcher Test(params Matcher[] matchers)
        {
            ValidateMatcherArray(matchers);
            return new TestMatcher(matchers, false);
        }

        public static Matcher NegativeTest(params Matcher[] matchers)
        {
            ValidateMatcherArray(matchers);
            return new TestMatcher(matchers, true);
        }

        public static Matcher ReverseTest(params Matcher[] matchers)
        {
            ValidateMatcherArray(matchers);
            return new ReverseMatcher(new TestMatcher(matchers, false));
        }

        public static Matcher ReverseNegativeTest(params Matcher[] matchers)
        {
            ValidateMatcherArray(matchers);
            return new ReverseMatcher(new TestMatcher(matchers, true));
        }

        public static Matcher CaptureGroup(string name, Matcher matcher)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (matcher == null)
            {
                throw new ArgumentNullException(nameof(matcher));
            }
            return new CaptureGroupMatcher(name, matcher);
        }

        public static Matcher BackReference(string groupName)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }
            return new BackReferenceMatcher(groupName);
        }

        private static void ValidateMatcherArray(Matcher[] matchers)
        {
            if (matchers == null)
            {
                throw new ArgumentNullException(nameof(matchers));
            }
            if (matchers.Length == 0)
            {
                throw new ArgumentException("Cannot be zero length.", nameof(matchers));
            }
            foreach (var m in matchers)
            {
                if (m == null)
                {
                    throw new ArgumentException("Cannot contain null.", nameof(matchers));
                }
            }
        }

        /// <summary>
        /// Sequence.
        /// </summary>
        public static Matcher operator +(Matcher left, Matcher right)
        {
            var seqLeft = left as SequenceMatcher;
            var seqRight = right as SequenceMatcher;
            if (seqLeft != null)
            {
                if (seqRight != null)
                {
                    return Sequence(seqLeft.Inners.Concat(seqRight.Inners).ToArray());
                }
                else
                {
                    return Sequence(seqLeft.Inners.Concat(new[] { right }).ToArray());
                }
            }
            else
            {
                if (seqRight != null)
                {
                    return Sequence(new[] { seqLeft }.Concat(seqRight.Inners).ToArray());
                }
                else
                {
                    return Sequence(left, right);
                }
            }
        }

        /// <summary>
        /// Any.
        /// </summary>
        public static Matcher operator |(Matcher left, Matcher right)
        {
            var seqLeft = left as AnyMatcher;
            var seqRight = right as AnyMatcher;
            if (seqLeft != null)
            {
                if (seqRight != null)
                {
                    return Any(seqLeft.Inners.Concat(seqRight.Inners).ToArray());
                }
                else
                {
                    return Any(seqLeft.Inners.Concat(new[] { right }).ToArray());
                }
            }
            else
            {
                if (seqRight != null)
                {
                    return Any(new[] { seqLeft }.Concat(seqRight.Inners).ToArray());
                }
                else
                {
                    return Any(left, right);
                }
            }
        }

        /// <summary>
        /// Repeat.
        /// </summary>
        public static Matcher operator *(Matcher matcher, int count) =>
            Repeat(matcher, count, count);

        /// <summary>
        /// Repeat.
        /// </summary>
        public static Matcher operator *(Matcher matcher, Tuple<int, int> count) =>
            Repeat(matcher, count.Item1, count.Item2);

        /// <summary>
        /// Test.
        /// </summary>
        public static Matcher operator ~(Matcher matcher) =>
            Test(matcher);

        /// <summary>
        /// Negative test.
        /// </summary>
        public static Matcher operator !(Matcher matcher) =>
            NegativeTest(matcher);

        /// <summary>
        /// Reverse test.
        /// </summary>
        public static Matcher operator -(Matcher matcher)
        {
            if (matcher is TestMatcher)
            {
                return new ReverseMatcher(matcher);
            }
            else
            {
                return ReverseTest(matcher);
            }
        }

        /// <summary>
        /// Group.
        /// </summary>
        public static Matcher operator >>(Matcher matcher, int groupName) =>
            CaptureGroup(groupName.ToString(), matcher);

        public static explicit operator Matcher(string text) =>
            String(text);

        public static explicit operator Matcher(char ch) =>
            Char(ch);
    }
}
