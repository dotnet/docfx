﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;

    internal sealed class AnyCharNotInMatcher : Matcher
    {
        private readonly char[] _ch;

        public AnyCharNotInMatcher(char[] ch)
        {
            _ch = ch;
        }

        public override int Match(MatchContent content)
        {
            if (content.EndOfString())
            {
                return NotMatch;
            }
            return Array.BinarySearch(_ch, content.GetCurrentChar()) >= 0 ? NotMatch : 1;
        }
    }
}
