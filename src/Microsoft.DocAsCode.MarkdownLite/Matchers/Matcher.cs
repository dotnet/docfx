// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;
    using System.Linq;

    public abstract class Matcher
    {
        public const int NotMatch = -1;

        /// <summary>
        /// Match string in content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>Char count of match, <c>-1</c> is not match.</returns>
        public abstract int Match(MatchContent content);

        protected string EscapeText(string text)
        {
            return text
                .Replace(@"\", @"\\")
                .Replace("\n", @"\n")
                .Replace("\r", @"\r")
                .Replace("\t", @"\t")
                .Replace("\"", @"\""")
                .Replace("\'", @"\'")
                .Replace("\a", @"\a")
                .Replace("\b", @"\b")
                .Replace("\f", @"\f")
                .Replace("\v", @"\v")
                .Replace("\0", @"\0")
                .Replace("[", @"\[")
                .Replace("]", @"\]")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace("{", @"\{")
                .Replace("}", @"\}")
                .Replace("<", @"\<")
                .Replace(">", @"\>")
                .Replace(":", @"\:")
                .Replace("|", @"\|")
                .Replace("-", @"\-")
                .Replace("^", @"\^")
                .Replace("$", @"\$");
        }

        public static Matcher Char(char ch)
        {
            return new CharMatcher(ch);
        }

        public static Matcher AnyChar { get; } = new AnyCharMatcher();

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
            return new AnyCharInMatcher(array);
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

        public static Matcher AnyCharNot(char ch)
        {
            return new AnyCharNotMatcher(ch);
        }

        public static Matcher AnyCharNotIn(params char[] ch)
        {
            if (ch == null)
            {
                throw new ArgumentNullException(nameof(ch));
            }
            var array = (char[])ch.Clone();
            Array.Sort(array);
            return new AnyCharNotInMatcher(array);
        }

        public static Matcher WhiteSpace { get; } = new CharMatcher(' ');

        public static Matcher WhiteSpaces { get; } = Repeat(WhiteSpace, 1);

        public static Matcher WhiteSpacesOrEmpty { get; } = Repeat(WhiteSpace, 0);

        public static Matcher NewLine { get; } = new CharMatcher('\n');

        public static Matcher BlankCharacter { get; } = new AnyCharInMatcher(new[] { ' ', '\n' });

        public static Matcher Blank { get; } = Repeat(BlankCharacter, 1);

        public static Matcher BlankOrEmpty { get; } = Repeat(BlankCharacter, 0);

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

        public static Matcher AnyStringInSingleLine { get; } = AnyCharNot('\n').RepeatAtLeast(1);

        public static Matcher AnyStringInSingleLineOrEmpty { get; } = AnyCharNot('\n').RepeatAtLeast(0);

        public static Matcher AnyWordCharacter { get; } =
            AnyCharIn(
                (from i in Enumerable.Range(0, 26)
                 select (char)('a' + i))
                .Concat(from i in Enumerable.Range(0, 26)
                        select (char)('A' + i))
                .Concat(from i in Enumerable.Range(0, 10)
                        select (char)('0' + i))
                .Concat(new[] { '_' })
                .ToArray());

        public static Matcher EndOfString { get; } = new EndOfStringMatcher();

        public static Matcher WordBoundary { get; } = AnyWordCharacter.ToNegativeTest() | EndOfString;

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
            var repeatable = matcher as IRepeatable;
            if (repeatable != null)
            {
                return repeatable.Repeat(minOccur, maxOccur);
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

        public static Matcher CompareLength(Matcher inner, LengthComparison comparsion, string groupName)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }
            return new LengthComparisonMatcher(inner, comparsion, groupName);
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
            if (left == null)
            {
                return right;
            }
            if (right == null)
            {
                return left;
            }
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
                    return Sequence(new[] { left }.Concat(seqRight.Inners).ToArray());
                }
                else
                {
                    return Sequence(left, right);
                }
            }
        }

        /// <summary>
        /// Sequence.
        /// </summary>
        public static Matcher operator +(Matcher left, char right)
        {
            return left + new CharMatcher(right);
        }

        /// <summary>
        /// Sequence.
        /// </summary>
        public static Matcher operator +(Matcher left, string right)
        {
            return left + new StringMatcher(right);
        }

        /// <summary>
        /// Any.
        /// </summary>
        public static Matcher operator |(Matcher left, Matcher right)
        {
            if (left == null)
            {
                return right;
            }
            if (right == null)
            {
                return left;
            }
            var anyLeft = left as AnyMatcher;
            var anyRight = right as AnyMatcher;
            if (anyLeft != null)
            {
                if (anyRight != null)
                {
                    return Any(anyLeft.Inners.Concat(anyRight.Inners).ToArray());
                }
                else
                {
                    return Any(anyLeft.Inners.Concat(new[] { right }).ToArray());
                }
            }
            else
            {
                if (anyRight != null)
                {
                    return Any(new[] { left }.Concat(anyRight.Inners).ToArray());
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

        public static explicit operator Matcher(string text) =>
            String(text);

        public static explicit operator Matcher(char ch) =>
            Char(ch);
    }
}
