// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class CaseInsensitiveStringMatcher : Matcher
    {
        private readonly string _upperCaseText;
        private readonly string _lowerCaseText;

        public CaseInsensitiveStringMatcher(string text)
        {
            _upperCaseText = text.ToUpper();
            _lowerCaseText = text.ToLower();
        }

        public override int Match(MatchContent content)
        {
            if (content.Length < _upperCaseText.Length)
            {
                return NotMatch;
            }
            for (int i = 0; i < _upperCaseText.Length; i++)
            {
                var ch = content[i];
                if (ch != _upperCaseText[i] && ch != _lowerCaseText[i])
                {
                    return NotMatch;
                }
            }
            return _upperCaseText.Length;
        }

        public override string ToString()
        {
            return "(CaseInsensitive:" + EscapeText(_lowerCaseText) + ")";
        }
    }
}
