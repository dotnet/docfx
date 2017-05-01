// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class RepeatMatcher : Matcher
    {
        private readonly Matcher _inner;
        private readonly int _minOccur;
        private readonly int _maxOccur;

        public RepeatMatcher(Matcher inner, int minOccur, int maxOccur)
        {
            _inner = inner;
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

        public override int Match(MatchContent content)
        {
            int totalCharCount = 0;
            int count = 0;
            while (true)
            {
                var currentCharCount = _inner.Match(content.Offset(totalCharCount));
                if (currentCharCount <= 0)
                {
                    return count >= _minOccur ? totalCharCount : NotMatch;
                }
                totalCharCount += currentCharCount;
                count++;
                if (count >= _maxOccur)
                {
                    return totalCharCount;
                }
                if (content.Length == totalCharCount)
                {
                    return count >= _minOccur ? totalCharCount : NotMatch;
                }
            }
        }

        public override string ToString()
        {
            return _inner.ToString() +
                "{" +
                _minOccur.ToString() +
                "," +
                (_maxOccur == int.MaxValue ? string.Empty : _maxOccur.ToString()) +
                "}";
        }
    }
}
