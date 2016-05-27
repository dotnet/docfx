// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class MarkdownListBlockRule : IMarkdownRule
    {
        public virtual string Name => "List";

        public virtual Regex OrderList => Regexes.Block.OrderList;

        public virtual Regex UnorderList => Regexes.Block.UnorderList;

        public virtual Regex Item => Regexes.Block.Item;

        public virtual Regex Bullet => Regexes.Block.Bullet;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = OrderList.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                match = UnorderList.Match(context.CurrentMarkdown);
                if (match.Length == 0)
                {
                    return null;
                }
            }
            var sourceInfo = context.Consume(match.Length);

            var bull = match.Groups[2].Value;

            var cap = match.Groups[0].Value.Match(Item);

            var next = false;
            var l = cap.Length;
            int i = 0;
            var tokens = new List<IMarkdownToken>();
            var lineOffset = 0;
            var lines = 0;
            for (; i < l; i++)
            {
                var item = cap[i];
                lines = item.Count(ch => ch == '\n') + 1;
                // Remove the list item's bullet
                // so it is seen as the next token.
                var space = item.Length;
                item = item.ReplaceRegex(Regexes.Lexers.LeadingBullet, string.Empty);

                // Outdent whatever the
                // list item contains. Hacky.
                if (item.IndexOf("\n ") > -1)
                {
                    space -= item.Length;
                    item = !parser.Options.Pedantic
                      ? Regex.Replace(item, "^ {1," + space + "}", "", RegexOptions.Multiline)
                      : Regex.Replace(item, @"^ {1,4}", "", RegexOptions.Multiline);
                }

                // Determine whether item is loose or not.
                // Use: /(^|\n)(?! )[^\n]+\n\n(?!\s*$)/
                // for discount behavior.
                var loose = next || Regex.IsMatch(item, @"\n\n(?!\s*$)");
                if (i != l - 1 && item.Length != 0)
                {
                    next = item[item.Length - 1] == '\n';
                    if (!loose) loose = next;
                }

                tokens.Add(
                    new TwoPhaseBlockToken(
                        this,
                        parser.Context,
                        sourceInfo.Copy(item, lineOffset),
                        (p, t) =>
                        {
                            p.SwitchContext(MarkdownBlockContext.IsTop, false);
                            var blockTokens = p.Tokenize(t.SourceInfo.Copy(item));
                            blockTokens = TokenHelper.ParseInlineToken(p, this, blockTokens, loose, t.SourceInfo);
                            return new MarkdownListItemBlockToken(t.Rule, t.Context, blockTokens, loose, t.SourceInfo);
                        }));
                lineOffset += lines;
            }

            return new MarkdownListBlockToken(this, parser.Context, tokens.ToImmutableArray(), bull.Length > 1, sourceInfo);
        }
    }
}
