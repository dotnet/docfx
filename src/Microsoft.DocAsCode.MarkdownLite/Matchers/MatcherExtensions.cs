// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    public static class MatcherExtensions
    {
        public static MatchResult Match(this Matcher matcher, string text, int startIndex = 0)
        {
            var mc = new MatchContent(text, startIndex, ScanDirection.Forward);
            var result = matcher.Match(mc);
            if (result == Matcher.NotMatch)
            {
                return null;
            }
            return new MatchResult(result, mc);
        }
    }
}
