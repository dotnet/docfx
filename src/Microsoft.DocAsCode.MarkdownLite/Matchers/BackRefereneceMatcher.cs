// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class BackRefereneceMatcher : Matcher
    {
        private readonly string _groupName;

        public BackRefereneceMatcher(string groupName)
        {
            _groupName = groupName;
        }

        public override int Match(MatchContent content)
        {
            var g = content.GetGroup(_groupName);
            if (g == null)
            {
                return NotMatch;
            }
            var text = g.Value.GetValue();
            if (!content.TestLength(text.Length))
            {
                return NotMatch;
            }
            for (int i = 0; i < text.Length; i++)
            {
                if (content[i] != text[i])
                {
                    return NotMatch;
                }
            }
            return text.Length;
        }
    }
}
