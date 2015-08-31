// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class Lexer
    {

        private readonly Options _options;
        private readonly BlockRules _rules;

        public Lexer(Options options)
        {
            _options = options ?? new Options();
            _rules = new NormalBlockRules();

            if (_options.Gfm)
            {
                if (_options.Tables)
                {
                    _rules = new TablesBlockRules();
                }
                else
                {
                    _rules = new GfmBlockRules();
                }
            }
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

        /// <summary>
        /// Lexing
        /// </summary>
        protected virtual void Token(string srcOrig, bool top, TokensResult tokens)
        {
            var src = Preprocess(srcOrig);

            while (!string.IsNullOrEmpty(src))
            {
                if (!ApplyRules(top, ref src, tokens))
                {
                    throw new Exception("Cannot find suitable rule for byte: " + ((int)src[0]).ToString());
                }
            }
        }

        protected virtual string Preprocess(string src)
        {
            return Regexes.Lexers.WhiteSpaceLine.Replace(src, string.Empty);
        }

        protected virtual bool ApplyRules(bool top, ref string src, TokensResult tokens)
        {
            return
                // newline
                ApplyNewLine(top, ref src, tokens) ||
                // code
                ApplyCode(top, ref src, tokens) ||
                // fences (gfm)
                ApplyFences(top, ref src, tokens) ||
                // heading
                ApplyHeading(top, ref src, tokens) ||
                // table no leading pipe (gfm)
                ApplyNoLeadingPipe(top, ref src, tokens) ||
                // lheading
                ApplyLHeading(top, ref src, tokens) ||
                // hr
                ApplyHr(top, ref src, tokens) ||
                // blockquote
                ApplyBlockquote(top, ref src, tokens) ||
                // list
                ApplyList(top, ref src, tokens) ||
                // html
                ApplyHtml(top, ref src, tokens) ||
                // def
                ApplyDef(top, ref src, tokens) ||
                // table (gfm)
                ApplyTable(top, ref src, tokens) ||
                // top-level paragraph
                ApplyParagraph(top, ref src, tokens) ||
                // text
                ApplyText(top, ref src, tokens);
        }

        protected virtual bool ApplyNewLine(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Newline.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                if (cap[0].Length > 1)
                {
                    tokens.Add(new Token
                    {
                        Type = TokenTypes.Space
                    });
                }
                return true;
            }
            return false;
        }

        protected virtual bool ApplyCode(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Сode.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                var capStr = Regexes.Lexers.LeadingWhiteSpaces.Replace(cap[0], string.Empty);
                tokens.Add(new Token
                {
                    Type = TokenTypes.Code,
                    Text = !_options.Pedantic
                      ? Regexes.Lexers.TailingEmptyLines.Replace(capStr, string.Empty)
                      : capStr
                });
                return true;
            }
            return false;
        }

        protected virtual bool ApplyFences(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Fences.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                tokens.Add(new Token
                {
                    Type = TokenTypes.Code,
                    Lang = cap[2],
                    Text = cap[3]
                });
                return true;
            }
            return false;
        }

        protected virtual bool ApplyHeading(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Heading.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                tokens.Add(new Token
                {
                    Type = TokenTypes.Heading,
                    Depth = cap[1].Length,
                    Text = cap[2]
                });
                return true;
            }
            return false;
        }

        protected virtual bool ApplyNoLeadingPipe(bool top, ref string src, TokensResult tokens)
        {
            if (!top)
            {
                return false;
            }
            var cap = _rules.NpTable.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);

                var item = new Token
                {
                    Type = TokenTypes.Table,
                    Header = cap[1].ReplaceRegex(Regexes.Lexers.UselessTableHeader, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter),
                    Align = ParseAligns(cap[2].ReplaceRegex(Regexes.Lexers.UselessTableAlign, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter)),
                    Cells = cap[3].ReplaceRegex(Regexes.Lexers.EndWithNewLine, string.Empty).Split('\n').Select(x => new string[] { x }).ToArray()
                };

                for (int i = 0; i < item.Cells.Length; i++)
                {
                    item.Cells[i] = item.Cells[i][0].SplitRegex(Regexes.Lexers.TableSplitter);
                }

                tokens.Add(item);

                return true;
            }
            return false;
        }

        protected virtual bool ApplyLHeading(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.LHeading.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                tokens.Add(new Token
                {
                    Type = TokenTypes.Heading,
                    Depth = cap[2] == "=" ? 1 : 2,
                    Text = cap[1]
                });
                return true;
            }
            return false;
        }

        protected virtual bool ApplyHr(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Hr.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                tokens.Add(new Token
                {
                    Type = TokenTypes.Hr
                });
                return true;
            }
            return false;
        }

        protected virtual bool ApplyBlockquote(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Blockquote.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);

                tokens.Add(new Token
                {
                    Type = TokenTypes.BlockquoteStart
                });

                var capStr = Regexes.Lexers.LeadingBlockquote.Replace(cap[0], string.Empty);

                // Pass `top` to keep the current
                // "toplevel" state. This is exactly
                // how markdown.pl works.
                Token(capStr, top, tokens);

                tokens.Add(new Token
                {
                    Type = TokenTypes.BlockquoteEnd
                });

                return true;
            }
            return false;
        }

        protected virtual bool ApplyList(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.List.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                var bull = cap[2];

                tokens.Add(new Token
                {
                    Type = TokenTypes.ListStart,
                    Ordered = bull.Length > 1
                });

                // Get each top-level item.
                cap = cap[0].Match(_rules.Item);

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
            return false;
        }

        protected virtual bool ApplyHtml(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Html.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                tokens.Add(new Token
                {
                    Type = _options.Sanitize
                      ? TokenTypes.Paragraph
                      : TokenTypes.Html,
                    Pre = (_options.Sanitizer == null)
                      && (cap[1] == "pre" || cap[1] == "script" || cap[1] == "style"),
                    Text = cap[0]
                });
                return true;
            }
            return false;
        }

        protected virtual bool ApplyDef(bool top, ref string src, TokensResult tokens)
        {
            if (!top)
            {
                return false;
            }
            var cap = _rules.Def.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                tokens.Links[cap[1].ToLower()] = new LinkObj
                {
                    Href = cap[2],
                    Title = cap[3]
                };
                return true;
            }
            return false;
        }

        protected virtual bool ApplyTable(bool top, ref string src, TokensResult tokens)
        {
            if (!top)
            {
                return false;
            }
            var cap = _rules.Table.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);

                var item = new Token
                {
                    Type = TokenTypes.Table,
                    Header = cap[1].ReplaceRegex(Regexes.Lexers.UselessTableHeader, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter),
                    Align = ParseAligns(cap[2].ReplaceRegex(Regexes.Lexers.UselessTableAlign, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter)),
                    Cells = cap[3].ReplaceRegex(Regexes.Lexers.UselessGfmTableCell, string.Empty).Split('\n').Select(x => new string[] { x }).ToArray()
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
            return false;
        }

        protected virtual bool ApplyParagraph(bool top, ref string src, TokensResult tokens)
        {
            if (!top)
            {
                return false;
            }
            var cap = _rules.Paragraph.Apply(src);
            if (cap.Length > 0)
            {
                src = src.Substring(cap[0].Length);
                tokens.Add(new Token
                {
                    Type = TokenTypes.Paragraph,
                    Text = cap[1][cap[1].Length - 1] == '\n'
                      ? cap[1].Substring(0, cap[1].Length - 1)
                      : cap[1]
                });
                return true;
            }
            return false;
        }

        protected virtual bool ApplyText(bool top, ref string src, TokensResult tokens)
        {
            var cap = _rules.Text.Apply(src);
            if (cap.Length > 0)
            {
                // Top-level should never reach here.
                src = src.Substring(cap[0].Length);
                tokens.Add(new Token
                {
                    Type = TokenTypes.Text,
                    Text = cap[0]
                });
                return true;
            }
            return false;
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
