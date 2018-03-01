// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class TestMatcher : Matcher
    {
        private readonly Matcher[] _inner;
        private readonly bool _isNegative;

        public TestMatcher(Matcher[] inner, bool isNegative)
        {
            _inner = inner;
            _isNegative = isNegative;
        }

        public override int Match(MatchContent content)
        {
            foreach (var m in _inner)
            {
                if (_isNegative ^ (m.Match(content) == NotMatch))
                {
                    return NotMatch;
                }
            }
            return 0;
        }

        public override string ToString()
        {
            return (_isNegative ? "(?!" : "(?=") + string.Join<Matcher>("|", _inner) + ")";
        }
    }
}
