// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class GfmEmojiInlineRule : IMarkdownRule
    {
        private static readonly Dictionary<string, string> _emoji =
            new Dictionary<string, string>
            {
                ["smile"] = "😄",
            };

        public virtual string Name => "Inline.Gfm.Emoji";

        public virtual Regex Emoji => Regexes.Inline.Gfm.Emoji;

        protected virtual string GetEmoji(string shortCode)
        {
            string result;
            _emoji.TryGetValue(shortCode, out result);
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
                var sourceInfo = context.Consume(1);
                return new MarkdownTextToken(
                    this,
                    parser.Context,
                    sourceInfo.Markdown,
                    sourceInfo);
            }
            else
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownTextToken(
                    this,
                    parser.Context,
                    text,
                    sourceInfo);
            }
        }
    }
}
