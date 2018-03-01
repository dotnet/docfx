// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class AnyCharInRangeMatcher : Matcher, IRepeatable
    {
        private readonly char _start;
        private readonly char _end;

        public AnyCharInRangeMatcher(char start, char end)
        {
            _start = start;
            _end = end;
        }

        public override int Match(MatchContent content)
        {
            if (content.EndOfString())
            {
                return NotMatch;
            }
            var ch = content.GetCurrentChar();
            return ch >= _start && ch <= _end ? 1 : NotMatch;
        }

        public Matcher Repeat(int minOccur, int maxOccur)
        {
            return new AnyCharInRangeRepeatMatcher(_start, _end, minOccur, maxOccur);
        }

        public override string ToString()
        {
            return "[" + EscapeText(_start.ToString()) + "-" + EscapeText(_end.ToString()) + "]";
        }
    }
}
