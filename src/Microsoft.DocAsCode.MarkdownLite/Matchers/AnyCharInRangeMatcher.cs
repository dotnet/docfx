// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class AnyCharInRangeMatcher : Matcher
    {
        private readonly char _start;
        private readonly char _end;

        public AnyCharInRangeMatcher(char start, char end)
        {
            _start = start;
            _end = end;
        }

        public override int Match(MatchContent content)
        {
            if (content.Eos())
            {
                return NotMatch;
            }
            var ch = content[0];
            return ch >= _start && ch <= _end ? 1 : NotMatch;
        }
    }
}
