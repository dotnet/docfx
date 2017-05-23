// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public partial class GfmEmojiInlineRule : IMarkdownRule
    {
        private static readonly Dictionary<string, string> _emoji = LoadEmoji();

        public virtual string Name => "Inline.Gfm.Emoji";

        public virtual Regex Emoji => Regexes.Inline.Gfm.Emoji;

        protected virtual string GetEmoji(string shortCode)
        {
            _emoji.TryGetValue(shortCode, out string result);
            return result;
        }

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Emoji.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var shortCode = match.Groups[1].Value;
            var text = GetEmoji(shortCode);
            if (text == null)
            {
                return null;
            }
            else
            {
                var sourceInfo = context.Consume(match.Length);
                return new GfmEmojiInlineToken(
                    this,
                    parser.Context,
                    shortCode,
                    text,
                    sourceInfo);
            }
        }
    }
}
