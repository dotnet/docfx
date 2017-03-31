// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class AnyCharNotInRepeatMatcher : Matcher
    {
        private readonly char[] _ch;
        private readonly int _minOccur;
        private readonly int _maxOccur;

        public AnyCharNotInRepeatMatcher(char[] ch, int minOccur, int maxOccur)
        {
            _ch = ch;
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

        public override int Match(MatchContent content)
        {
            var count = content.CountUntilAny(_ch, _maxOccur);
            if (count < _minOccur)
            {
                return NotMatch;
            }
            return count;
        }

        public override string ToString()
        {
            return "[^" +
                EscapeText(string.Join(string.Empty, _ch)) +
                "]{" +
                _minOccur.ToString() +
                "," +
                (_maxOccur == int.MaxValue ? string.Empty : _maxOccur.ToString()) +
                "}";
        }
    }
}
