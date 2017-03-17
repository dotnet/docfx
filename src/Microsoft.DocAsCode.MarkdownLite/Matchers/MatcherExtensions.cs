// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    public static class MatcherExtensions
    {
        public static MatchResult Match(this Matcher matcher, string text, int startIndex = 0)
        {
            var mc = new MatchContent(text, startIndex, MatchDirection.Forward);
            var result = matcher.Match(mc);
            if (result == Matcher.NotMatch)
            {
                return null;
            }
            return new MatchResult(result, mc);
        }

        public static Matcher ToGroup(this Matcher matcher, string groupName)
        {
            return Matcher.CaptureGroup(groupName, matcher);
        }

        public static Matcher Maybe(this Matcher matcher)
        {
            return Matcher.Maybe(matcher);
        }

        public static Matcher RepeatAtLeast(this Matcher matcher, int minOccur) =>
            Matcher.Repeat(matcher, minOccur);

        public static Matcher Repeat(this Matcher matcher, int minOccur, int maxOccur) =>
            Matcher.Repeat(matcher, minOccur, maxOccur);

        public static Matcher ToTest(this Matcher matcher) =>
            Matcher.Test(matcher);

        public static Matcher ToNegativeTest(this Matcher matcher) =>
            Matcher.NegativeTest(matcher);

        public static Matcher ToReverseTest(this Matcher matcher) =>
            Matcher.ReverseTest(matcher);

        public static Matcher ToReverseNegativeTest(this Matcher matcher) =>
            Matcher.ReverseNegativeTest(matcher);

        public static Matcher CompareLength(this Matcher matcher, LengthComparison comparsion, string groupName) =>
            Matcher.CompareLength(matcher, comparsion, groupName);
    }
}
