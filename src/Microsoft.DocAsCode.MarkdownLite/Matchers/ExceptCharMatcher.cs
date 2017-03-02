// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;

    internal sealed class ExceptCharMatcher : Matcher
    {
        private readonly char[] _ch;

        public ExceptCharMatcher(char[] ch)
        {
            _ch = ch;
        }

        public override int Match(MatchContent content)
        {
            if (content.Eos())
            {
                return NotMatch;
            }
            return Array.BinarySearch(_ch, content[0]) < 0 ? NotMatch : 1;
        }
    }
}
