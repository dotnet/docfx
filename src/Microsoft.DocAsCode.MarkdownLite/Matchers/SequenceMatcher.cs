// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class SequenceMatcher : Matcher
    {
        private readonly Matcher[] _inners;

        public SequenceMatcher(Matcher[] inners)
        {
            _inners = inners;
        }

        public override int Match(MatchContent content)
        {
            int charCount = 0;
            if (content.Direction == MatchDirection.Forward)
            {
                for (int i = 0; i < _inners.Length; i++)
                {
                    var c = _inners[i].Match(content.Offset(charCount));
                    if (c == NotMatch)
                    {
                        return NotMatch;
                    }
                    else
                    {
                        charCount += c;
                    }
                }
            }
            else
            {
                for (int i = _inners.Length - 1; i >= 0; i--)
                {
                    var c = _inners[i].Match(content.Offset(charCount));
                    if (c == NotMatch)
                    {
                        return NotMatch;
                    }
                    else
                    {
                        charCount += c;
                    }
                }
            }
            return charCount;
        }

        internal Matcher[] Inners => _inners;

        public override string ToString()
        {
            return "(" + string.Join<Matcher>(string.Empty, _inners) + ")";
        }
    }
}
