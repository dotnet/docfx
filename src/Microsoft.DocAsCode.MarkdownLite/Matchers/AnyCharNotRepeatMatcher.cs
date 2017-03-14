// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class AnyCharNotRepeatMatcher : Matcher
    {
        private readonly char _ch;
        private readonly int _minOccur;
        private readonly int _maxOccur;

        public AnyCharNotRepeatMatcher(char ch, int minOccur, int maxOccur)
        {
            _ch = ch;
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

        public override int Match(MatchContent content)
        {
            var count = content.CountUntil(_ch);
            if (count < _minOccur)
            {
                return NotMatch;
            }
            if (count > _maxOccur)
            {
                return _maxOccur;
            }
            return count;
        }
    }
}
