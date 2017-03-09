﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class CharMatcher : Matcher
    {
        private readonly char _ch;

        public CharMatcher(char ch)
        {
            _ch = ch;
        }

        public override int Match(MatchContent content)
        {
            if (content.EndOfString())
            {
                return NotMatch;
            }
            return content.GetCurrentChar() == _ch ? 1 : NotMatch;
        }
    }
}
