// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;

    internal sealed class AnyCharInMatcher : Matcher, IRepeatable
    {
        private readonly char[] _ch;

        public AnyCharInMatcher(char[] ch)
        {
            _ch = ch;
        }

        public override int Match(MatchContent content)
        {
            if (content.EndOfString())
            {
                return NotMatch;
            }
            return Array.BinarySearch(_ch, content.GetCurrentChar()) >= 0 ? 1 : NotMatch;
        }

        public Matcher Repeat(int minOccur, int maxOccur)
        {
            return new AnyCharInRepeatMatcher(_ch, minOccur, maxOccur);
        }

        public override string ToString()
        {
            return "[" + EscapeText(string.Join(string.Empty, _ch)) + "]";
        }
    }
}
