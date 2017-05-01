// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    internal sealed class BackReferenceMatcher : Matcher
    {
        private readonly string _groupName;

        public BackReferenceMatcher(string groupName)
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
            if (content.Length < text.Length)
            {
                return NotMatch;
            }
            if (content.Direction == MatchDirection.Forward)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (content[i] != text[i])
                    {
                        return NotMatch;
                    }
                }
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (content[i] != text[text.Length - 1 - i])
                    {
                        return NotMatch;
                    }
                }
            }
            return text.Length;
        }

        public override string ToString()
        {
            return "(BackReference:<" + _groupName + ">)";
        }
    }
}
