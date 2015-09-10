// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;

    public class Parser
    {
        private readonly Options _options;
        private readonly InlineLexer _inline;
        private TokensResult _src;
        private IEnumerator<Token> _tokens;
        
        public Parser(Options options, InlineLexer inline = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (inline == null) _inline = new InlineLexer(options);
            else _inline = inline;

            _options = options;
        }

        /// <summary>
        /// Parse Loop
        /// </summary>
        public virtual string Parse(TokensResult src)
        {
            Init(src);
            using (_tokens = _src.Enumerate().GetEnumerator())
            {
                var result = StringBuffer.Empty;
                while (MoveNext())
                {
                    result += Dispatch();
                }

                return result;
            }
        }

        protected bool MoveNext()
        {
            return _tokens.MoveNext();
        }

        protected Token Current { get { return _tokens.Current; } }

        protected IRenderer Renderer { get { return _options.Renderer; } }

        protected Options Options { get { return _options; } }

        protected InlineLexer Inline { get { return _inline; } }

        protected virtual void Init(TokensResult src)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            if (src.Links != null)
            {
                _inline.SetLinks(src.Links);
            }

            _src = src;
        }

        /// <summary>
        /// Parse Text Tokens
        /// </summary>    
        protected virtual StringBuffer ParseText()
        {
            StringBuffer body = Current.Text;

            while (MoveNext() && Current.Type == TokenTypes.Text)
            {
                body = body + "\n" + Current.Text;
            }

            return Inline.ApplyRules(body);
        }

        /// <summary>
        /// Parse Current Token
        /// </summary>    
        protected virtual StringBuffer Dispatch()
        {
            var type = Current.Type;
            if (type == TokenTypes.Space)
            {
                return Space();
            }
            if (type == TokenTypes.Hr)
            {
                return Hr();
            }
            if (type == TokenTypes.Heading)
            {
                return Heading();
            }
            if (type == TokenTypes.Code)
            {
                return Code();
            }
            if (type == TokenTypes.Table)
            {
                return Table();
            }
            if (type == TokenTypes.BlockquoteStart)
            {
                return Blockquote();
            }
            if (type == TokenTypes.ListStart)
            {
                return List();
            }
            if (type == TokenTypes.ListItemStart)
            {
                return ListItem();
            }
            if (type == TokenTypes.LooseItemStart)
            {
                return LooseItem();
            }
            if (type == TokenTypes.Html)
            {
                return Html();
            }
            if (type == TokenTypes.Paragraph)
            {
                return Paragraph();
            }
            if (type == TokenTypes.Text)
            {
                return Text();
            }
            return DispatchExtensions();
        }

        protected virtual StringBuffer DispatchExtensions()
        {
            throw new NotSupportedException("Unknown token:" + _tokens.Current.Type.Name);
        }

        protected virtual StringBuffer Space()
        {
            return StringBuffer.Empty;
        }

        protected virtual StringBuffer Hr()
        {
            return Renderer.Hr();
        }

        protected virtual StringBuffer Heading()
        {
            return Renderer.Heading(Inline.ApplyRules(Current.Text), Current.Depth, Current.Text);
        }

        protected virtual StringBuffer Code()
        {
            return Renderer.Code(Current.Text, Current.Lang, Current.Escaped);
        }

        protected virtual StringBuffer Table()
        {
            var header = StringBuffer.Empty;
            var body = StringBuffer.Empty;

            // header
            var cell = StringBuffer.Empty;
            for (int i = 0; i < Current.Header.Length; i++)
            {
                cell += Renderer.TableCell(
                  Inline.ApplyRules(Current.Header[i]),
                  new TableCellFlags { Header = true, Align = i < Current.Align.Length ? Current.Align[i] : Align.NotSpec }
                );
            }
            header += Renderer.TableRow(cell);

            for (int i = 0; i < Current.Cells.Length; i++)
            {
                var row = Current.Cells[i];

                cell = StringBuffer.Empty;
                for (int j = 0; j < row.Length; j++)
                {
                    cell += Renderer.TableCell(
                      Inline.ApplyRules(row[j]),
                      new TableCellFlags { Header = false, Align = j < Current.Align.Length ? Current.Align[j] : Align.NotSpec }
                    );
                }

                body += Renderer.TableRow(cell);
            }
            return Renderer.Table(header, body);
        }

        protected virtual StringBuffer Blockquote()
        {
            var body = StringBuffer.Empty;

            while (MoveNext() && Current.Type != TokenTypes.BlockquoteEnd)
            {
                body += Dispatch();
            }

            return Renderer.Blockquote(body);
        }

        protected virtual StringBuffer List()
        {
            var body = StringBuffer.Empty;
            var ordered = Current.Ordered;

            while (MoveNext() && Current.Type != TokenTypes.ListEnd)
            {
                body += Dispatch();
            }

            return Renderer.List(body, ordered);
        }

        protected virtual StringBuffer ListItem()
        {
            var body = StringBuffer.Empty;
            if (MoveNext())
            {
                while (Current.Type != TokenTypes.ListItemEnd)
                {
                    if (Current.Type == TokenTypes.Text)
                    {
                        body += ParseText();
                    }
                    else
                    {
                        body += Dispatch();
                        if (!MoveNext())
                        {
                            break;
                        }
                    }
                }
            }

            return Renderer.ListItem(body);
        }

        protected virtual StringBuffer LooseItem()
        {
            var body = StringBuffer.Empty;

            while (MoveNext() && Current.Type != TokenTypes.ListItemEnd)
            {
                body += Dispatch();
            }

            return Renderer.ListItem(body);
        }

        protected virtual StringBuffer Html()
        {
            var html = !Current.Pre && !Options.Pedantic
              ? Inline.ApplyRules(Current.Text)
              : (StringBuffer)Current.Text;
            return Renderer.Html(html);
        }

        protected virtual StringBuffer Paragraph()
        {
            return Renderer.Paragraph(Inline.ApplyRules(Current.Text));
        }

        protected virtual StringBuffer Text()
        {
            return Renderer.Paragraph(ParseText());
        }

    }
}
