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
        public string Name => "List";

        public virtual Regex List => Regexes.Block.List;

        public virtual Regex Item => Regexes.Block.Item;

        public virtual Regex Bullet => Regexes.Block.Bullet;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Regexes.Block.List.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var bull = match.Groups[2].Value;

            var cap = match.Groups[0].Value.Match(Item);

            var next = false;
            var l = cap.Length;
            int i = 0;
            var tokens = new List<IMarkdownToken>();
            for (; i < l; i++)
            {
                var item = cap[i];

                // Remove the list item's bullet
                // so it is seen as the next token.
                var space = item.Length;
                item = item.ReplaceRegex(Regexes.Lexers.LeadingBullet, string.Empty);

                // Outdent whatever the
                // list item contains. Hacky.
                if (item.IndexOf("\n ") > -1)
                {
                    space -= item.Length;
                    item = !engine.Options.Pedantic
                      ? Regex.Replace(item, "^ {1," + space + "}", "", RegexOptions.Multiline)
                      : Regex.Replace(item, @"^ {1,4}", "", RegexOptions.Multiline);
                }

                // Determine whether the next list item belongs here.
                // Backpedal if it does not belong in this list.
                if (engine.Options.SmartLists && i != l - 1)
                {
                    var b = Bullet.Apply(cap[i + 1])[0]; // !!!!!!!!!!!
                    if (bull != b && !(bull.Length > 1 && b.Length > 1))
                    {
                        source = string.Join("\n", cap.Skip(i + 1)) + source;
                        i = l - 1;
                    }
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

                var c = engine.SwitchContext(MarkdownBlockContext.IsTop, false);
                tokens.Add(new MarkdownListItemBlockToken(this, engine.Context, engine.Tokenize(item), loose));
                engine.SwitchContext(c);
            }

            return new MarkdownListBlockToken(this, engine.Context, tokens.ToImmutableArray(), bull.Length > 1);
        }
    }
}
