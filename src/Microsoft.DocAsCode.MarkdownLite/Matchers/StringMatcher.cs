// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class StringMatcher : Matcher
    {
        private readonly string _text;

        public StringMatcher(string text)
        {
            _text = text;
        }

        public override int Match(MatchContent content)
        {
            if (!content.TestLength(_text.Length))
            {
                return NotMatch;
            }
            for (int i = 0; i < _text.Length; i++)
            {
                if (content[i] != _text[i])
                {
                    return NotMatch;
                }
            }
            return _text.Length;
        }
    }
}
