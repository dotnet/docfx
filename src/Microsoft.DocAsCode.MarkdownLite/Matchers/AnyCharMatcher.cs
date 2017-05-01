// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class AnyCharMatcher : Matcher
    {
        public AnyCharMatcher() { }

        public override int Match(MatchContent content)
        {
            return content.EndOfString() ? NotMatch : 1;
        }

        public override string ToString()
        {
            return ".";
        }
    }
}
