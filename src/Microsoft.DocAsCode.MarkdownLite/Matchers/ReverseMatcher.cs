// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class ReverseMatcher : Matcher
    {
        private readonly Matcher _inner;

        public ReverseMatcher(Matcher inner)
        {
            _inner = inner;
        }

        public override int Match(MatchContent content)
        {
            return _inner.Match(content.Reverse());
        }

        public override string ToString()
        {
            return "(Reverse:" + _inner + ")";
        }
    }
}
