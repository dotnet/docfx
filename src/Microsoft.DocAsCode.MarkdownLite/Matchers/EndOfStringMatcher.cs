// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class EndOfStringMatcher : Matcher
    {
        public override int Match(MatchContent content)
        {
            return content.EndOfString() ? 0 : NotMatch;
        }

        public override string ToString()
        {
            return "$";
        }
    }
}
