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
            if (content.Length < _text.Length)
            {
                return NotMatch;
            }
            if (content.Direction == MatchDirection.Forward)
            {
                for (int i = 0; i < _text.Length; i++)
                {
                    if (content[i] != _text[i])
                    {
                        return NotMatch;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _text.Length; i++)
                {
                    if (content[i] != _text[_text.Length - 1 - i])
                    {
                        return NotMatch;
                    }
                }
            }
            return _text.Length;
        }

        public override string ToString()
        {
            return EscapeText(_text);
        }
    }
}
