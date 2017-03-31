// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;

    internal sealed class AnyCharNotMatcher : Matcher, IRepeatable
    {
        private readonly char _ch;

        public AnyCharNotMatcher(char ch)
        {
            _ch = ch;
        }

        public override int Match(MatchContent content)
        {
            if (content.EndOfString())
            {
                return NotMatch;
            }
            return content.GetCurrentChar() != _ch ? 1 : NotMatch;
        }

        public Matcher Repeat(int minOccur, int maxOccur)
        {
            return new AnyCharNotRepeatMatcher(_ch, minOccur, maxOccur);
        }

        public override string ToString()
        {
            return "[^" + EscapeText(_ch.ToString()) + "]";
        }
    }
}
