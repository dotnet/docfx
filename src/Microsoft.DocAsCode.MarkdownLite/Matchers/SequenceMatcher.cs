// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class SequenceMatcher : Matcher
    {
        private readonly Matcher[] _inner;

        public SequenceMatcher(Matcher[] inner)
        {
            _inner = inner;
        }

        public override int Match(MatchContent content)
        {
            int charCount = 0;
            foreach (var m in _inner)
            {
                var c = m.Match(content.Offset(charCount));
                if (c == NotMatch)
                {
                    return NotMatch;
                }
                else
                {
                    charCount += c;
                }
            }
            return charCount;
        }
    }
}
