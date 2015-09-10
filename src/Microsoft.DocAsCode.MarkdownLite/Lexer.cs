// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class BlockResolverContext : IResolverContext
    {
        public bool top { get; set; }
    }

    public class Lexer
    {
        private readonly Options _options;
        private readonly BlockRules _rules;
        public ResolversCollection<Resolver<TokensResult>> BlockResolvers { get; private set; } = new ResolversCollection<Resolver<TokensResult>>();
        private BlockResolverContext _context = new BlockResolverContext { top = true };
        public Lexer(Options options)
        {
            _options = options ?? new Options();

            _rules = GetDefaultBlockRule(_options);

            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.NewLine, _rules.Newline, ApplyNewLine));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Code, _rules.Сode, ApplyCode));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Fences, _rules.Fences, ApplyFences));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Heading, _rules.Heading, ApplyHeading));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.NoLeadingPipe, _rules.NpTable, ApplyNoLeadingPipe, a => ((BlockResolverContext)a).top));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.LHeading, _rules.LHeading, ApplyLHeading));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Hr, _rules.Hr, ApplyHr));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Blockquote, _rules.Blockquote, ApplyBlockquote));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.List, _rules.List, ApplyList));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Html, _rules.Html, ApplyHtml));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Def, _rules.Def, ApplyDef, a => ((BlockResolverContext)a).top));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Table, _rules.Table, ApplyTable, a => ((BlockResolverContext)a).top));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Paragraph, _rules.Paragraph, ApplyParagraph, a => ((BlockResolverContext)a).top));
            BlockResolvers.Add(new Resolver<TokensResult>(TokenName.Text, _rules.Text, ApplyText));
        }

        /// <summary>
        /// Preprocessing
        /// </summary>
        public virtual TokensResult Lex(string src)
        {
            src = src
                .ReplaceRegex(Regexes.Lexers.NormalizeNewLine, "\n")
                .Replace("\t", "    ")
                .Replace("\u00a0", " ")
                .Replace("\u2424", "\n");
            var tokens = new TokensResult();
            Token(src, true, tokens);
            return tokens;
        }

        protected virtual BlockRules GetDefaultBlockRule(Options options)
        {
            if (options.Gfm)
            {
                if (options.Tables)
                {
                    return new TablesBlockRules();
                }
                else
                {
                    return new GfmBlockRules();
                }
            }
            else
            {
                return new NormalBlockRules();
            }
        }

        /// <summary>
        /// Lexing
        /// </summary>
        protected virtual void Token(string srcOrig, bool top, TokensResult tokens)
        {
            var src = Preprocess(srcOrig);
            _context.top = top;
            while (!string.IsNullOrEmpty(src))
            {
                if (!ApplyRules(ref src, ref tokens, _context))
                {
                    throw new Exception("Cannot find suitable rule for byte: " + ((int)src[0]).ToString());
                }
            }
        }

        protected virtual string Preprocess(string src)
        {
            return Regexes.Lexers.WhiteSpaceLine.Replace(src, string.Empty);
        }

        protected virtual bool ApplyRules(ref string src, ref TokensResult tokens, IResolverContext context)
        {
            foreach (var rule in BlockResolvers)
            {
                if (rule.Apply(ref src, ref tokens, context))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool ApplyNewLine(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            if (match.Groups[0].Value.Length > 1)
            {
                tokens.Add(new Token
                {
                    Type = TokenTypes.Space
                });
            }
            return true;
        }

        protected virtual bool ApplyCode(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            var capStr = Regexes.Lexers.LeadingWhiteSpaces.Replace(match.Groups[0].Value, string.Empty);
            tokens.Add(new Token
            {
                Type = TokenTypes.Code,
                Text = !_options.Pedantic
                  ? Regexes.Lexers.TailingEmptyLines.Replace(capStr, string.Empty)
                  : capStr
            });
            return true;
        }

        protected virtual bool ApplyFences(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Add(new Token
            {
                Type = TokenTypes.Code,
                Lang = match.Groups[2].Value,
                Text = match.Groups[3].Value
            });
            return true;
        }

        protected virtual bool ApplyHeading(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Add(new Token
            {
                Type = TokenTypes.Heading,
                Depth = match.Groups[1].Value.Length,
                Text = match.Groups[2].Value
            });
            return true;
        }

        protected virtual bool ApplyNoLeadingPipe(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            var item = new Token
            {
                Type = TokenTypes.Table,
                Header = match.Groups[1].Value.ReplaceRegex(Regexes.Lexers.UselessTableHeader, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter),
                Align = ParseAligns(match.Groups[2].Value.ReplaceRegex(Regexes.Lexers.UselessTableAlign, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter)),
                Cells = match.Groups[3].Value.ReplaceRegex(Regexes.Lexers.EndWithNewLine, string.Empty).Split('\n').Select(x => new string[] { x }).ToArray()
            };

            for (int i = 0; i < item.Cells.Length; i++)
            {
                item.Cells[i] = item.Cells[i][0].SplitRegex(Regexes.Lexers.TableSplitter);
            }

            tokens.Add(item);

            return true;
        }

        protected virtual bool ApplyLHeading(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Add(new Token
            {
                Type = TokenTypes.Heading,
                Depth = match.Groups[2].Value == "=" ? 1 : 2,
                Text = match.Groups[1].Value
            });
            return true;
        }

        protected virtual bool ApplyHr(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Add(new Token
            {
                Type = TokenTypes.Hr
            });
            return true;
        }

        protected virtual bool ApplyBlockquote(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Add(new Token
            {
                Type = TokenTypes.BlockquoteStart
            });

            var capStr = Regexes.Lexers.LeadingBlockquote.Replace(match.Groups[0].Value, string.Empty);

            // Pass `top` to keep the current
            // "toplevel" state. This is exactly
            // how markdown.pl works.
            Token(capStr, ((BlockResolverContext)context).top, tokens);

            tokens.Add(new Token
            {
                Type = TokenTypes.BlockquoteEnd
            });

            return true;
        }

        protected virtual bool ApplyList(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            var bull = match.Groups[2].Value;

            tokens.Add(new Token
            {
                Type = TokenTypes.ListStart,
                Ordered = bull.Length > 1
            });

            // Get each top-level item.
            var cap = match.Groups[0].Value.Match(_rules.Item);

            var next = false;
            var l = cap.Length;
            int i = 0;

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
                    item = !_options.Pedantic
                      ? Regex.Replace(item, "^ {1," + space + "}", "", RegexOptions.Multiline)
                      : Regex.Replace(item, @"/^ {1,4}", "", RegexOptions.Multiline);
                }

                // Determine whether the next list item belongs here.
                // Backpedal if it does not belong in this list.
                if (_options.SmartLists && i != l - 1)
                {
                    var b = _rules.Bullet.Apply(cap[i + 1])[0]; // !!!!!!!!!!!
                    if (bull != b && !(bull.Length > 1 && b.Length > 1))
                    {
                        src = String.Join("\n", cap.Skip(i + 1)) + src;
                        i = l - 1;
                    }
                }

                // Determine whether item is loose or not.
                // Use: /(^|\n)(?! )[^\n]+\n\n(?!\s*$)/
                // for discount behavior.
                var loose = next || Regex.IsMatch(item, @"\n\n(?!\s*$)");
                if (i != l - 1)
                {
                    next = item[item.Length - 1] == '\n';
                    if (!loose) loose = next;
                }

                tokens.Add(new Token
                {
                    Type = loose
                      ? TokenTypes.LooseItemStart
                      : TokenTypes.ListItemStart
                });

                // Recurse.
                Token(item, false, tokens);

                tokens.Add(new Token
                {
                    Type = TokenTypes.ListItemEnd
                });
            }

            tokens.Add(new Token
            {
                Type = TokenTypes.ListEnd
            });

            return true;
        }

        protected virtual bool ApplyHtml(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Add(new Token
            {
                Type = _options.Sanitize
                  ? TokenTypes.Paragraph
                  : TokenTypes.Html,
                Pre = (_options.Sanitizer == null)
                  && (match.Groups[1].Value == "pre" || match.Groups[1].Value == "script" || match.Groups[1].Value == "style"),
                Text = match.Groups[0].Value
            });
            return true;
        }

        protected virtual bool ApplyDef(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Links[match.Groups[1].Value.ToLower()] = new LinkObj
            {
                Href = match.Groups[2].Value,
                Title = match.Groups[3].Value
            };
            return true;
        }

        protected virtual bool ApplyTable(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            var item = new Token
            {
                Type = TokenTypes.Table,
                Header = match.Groups[1].Value.ReplaceRegex(Regexes.Lexers.UselessTableHeader, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter),
                Align = ParseAligns(match.Groups[2].Value.ReplaceRegex(Regexes.Lexers.UselessTableAlign, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter)),
                Cells = match.Groups[3].Value.ReplaceRegex(Regexes.Lexers.UselessGfmTableCell, string.Empty).Split('\n').Select(x => new string[] { x }).ToArray()
            };

            for (int i = 0; i < item.Cells.Length; i++)
            {
                item.Cells[i] = item.Cells[i][0]
                  .ReplaceRegex(Regexes.Lexers.EmptyGfmTableCell, string.Empty)
                  .SplitRegex(Regexes.Lexers.TableSplitter);
            }

            tokens.Add(item);

            return true;
        }

        protected virtual bool ApplyParagraph(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            tokens.Add(new Token
            {
                Type = TokenTypes.Paragraph,
                Text = match.Groups[1].Value[match.Groups[1].Value.Length - 1] == '\n'
                  ? match.Groups[1].Value.Substring(0, match.Groups[1].Value.Length - 1)
                  : match.Groups[1].Value
            });
            return true;
        }

        protected virtual bool ApplyText(Match match, IResolverContext context, ref string src, ref TokensResult tokens)
        {
            // Top-level should never reach here.
            tokens.Add(new Token
            {
                Type = TokenTypes.Text,
                Text = match.Groups[0].Value
            });
            return true;
        }

        protected virtual Align[] ParseAligns(string[] aligns)
        {
            var result = new Align[aligns.Length];
            for (int i = 0; i < aligns.Length; i++)
            {
                if (Regexes.Lexers.TableAlignRight.IsMatch(aligns[i]))
                {
                    result[i] = Align.Right;
                }
                else if (Regexes.Lexers.TableAlignCenter.IsMatch(aligns[i]))
                {
                    result[i] = Align.Center;
                }
                else if (Regexes.Lexers.TableAlignLeft.IsMatch(aligns[i]))
                {
                    result[i] = Align.Left;
                }
                else
                {
                    result[i] = Align.NotSpec;
                }
            }
            return result;
        }

    }
}
