// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class AnyMatcher : Matcher
    {
        private readonly Matcher[] _inner;

        public AnyMatcher(Matcher[] inner)
        {
            _inner = inner;
        }

        public override int Match(MatchContent content)
        {
            bool matchSuccess = false;
            foreach (var m in _inner)
            {
                var c = m.Match(content);
                if (c > 0)
                {
                    return c;
                }
                if (c == 0)
                {
                    matchSuccess = true;
                }
            }
            return matchSuccess ? 0 : NotMatch;
        }
    }
}
