// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class AnyCharInRangeRepeatMatcher : Matcher
    {
        private readonly char _start;
        private readonly char _end;
        private readonly int _minOccur;
        private readonly int _maxOccur;

        public AnyCharInRangeRepeatMatcher(char start, char end, int minOccur, int maxOccur)
        {
            _start = start;
            _end = end;
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

        public override int Match(MatchContent content)
        {
            var count = content.CountWhileInRange(_start, _end, _maxOccur);
            if (count < _minOccur)
            {
                return NotMatch;
            }
            return count;
        }

        public override string ToString()
        {
            return "[" +
                EscapeText(_start.ToString()) +
                "-" +
                EscapeText(_end.ToString()) +
                "]{" +
                _minOccur.ToString() +
                "," +
                (_maxOccur == int.MaxValue ? string.Empty : _maxOccur.ToString()) +
                "}";
        }
    }
}
