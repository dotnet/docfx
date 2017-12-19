// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigMarkdownRewriters
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    public class MarkdigMarkdownRenderer : DfmMarkdownRenderer
    {
        private static HttpClient _client = new HttpClient();
        private static readonly string _requestTemplate = "https://xref.docs.microsoft.com/query?uid={0}";
        private static DfmRenderer _dfmHtmlRender = new DfmRenderer();
        private static readonly Regex _headingRegex = new Regex(@"^(?<pre> *#{1,6}(?<whitespace> *))(?<text>[^\n]+?)(?<post>(?: +#*)? *(?:\n+|$))", RegexOptions.Compiled);
        private static readonly Regex _lheading = new Regex(@"^(?<text>[^\n]+)(?<post>\n *(?:=|-){2,} *(?:\n+|$))", RegexOptions.Compiled);

        public virtual StringBuffer Render(IMarkdownRenderer render, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            if (token.Rule is DfmXrefShortcutInlineRule)
            {
                if (TryResolveUid(token.Href))
                {
                    return $"@\"{token.Href}\"";
                }
            }

            return base.Render(render, token, context);
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            switch (token.LinkType)
            {
                case MarkdownLinkType.AutoLink:
                    return RenderAutoLink(render, token, context);
                case MarkdownLinkType.NormalLink:
                    return RenderLinkNormalLink(render, token, context);
                default:
                    return base.Render(render, token, context);
            }
        }

        private StringBuffer RenderAutoLink(IMarkdownRenderer render, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var content = RenderInlineTokens(token.Content, render);
            return $"<{content}>";
        }

        private StringBuffer RenderLinkNormalLink(IMarkdownRenderer render, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var content = StringBuffer.Empty;
            content += "[";
            content += RenderInlineTokens(token.Content, render);
            content += "](";
            content += StringHelper.EscapeMarkdownHref(token.Href);

            if (!string.IsNullOrEmpty(token.Title))
            {
                content += " \"";
                content += token.Title;
                content += "\"";
            }
            content += ")";

            return content;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            const string BlockQuoteStartString = "> ";
            const string BlockQuoteJoinString = "\n" + BlockQuoteStartString;

            var content = StringBuffer.Empty;
            var tokens = (from t in token.Tokens
                          where !(t is MarkdownNewLineBlockToken)
                          select t).ToList();
            for (var index = 0; index < tokens.Count; index++)
            {
                var t = tokens[index];
                if (index == tokens.Count - 1 && t is DfmVideoBlockToken videoToken)
                {
                    content += render.Render(t).ToString().TrimEnd();
                }
                else
                {
                    content += render.Render(t);
                }
            }
            var contents = content.ToString().Split('\n');
            content = StringBuffer.Empty;
            foreach (var item in contents)
            {
                if (content == StringBuffer.Empty)
                {
                    content += BlockQuoteStartString;
                    content += item;
                }
                else
                {
                    content += BlockQuoteJoinString;
                    content += item;
                }
            }
            return content + "\n\n";
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            var inside = false;
            foreach (var inline in token.Content.Tokens)
            {
                if (inline is MarkdownTagInlineToken)
                {
                    inside = !inside;
                    result += MarkupInlineToken(render, inline);
                }
                else
                {
                    result += inside ? MarkupInlineToken(render, inline)
                                     : Render(render, inline, inline.Context);
                }
            }

            return result;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var source = token.SourceInfo.Markdown;
            var symbol = source.StartsWith("_") ? "__" : "**";
            var content = StringBuffer.Empty;
            content += symbol;
            content += RenderInlineTokens(token.Content, render);
            content += symbol;
            return content;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var source = token.SourceInfo.Markdown;
            var symbol = source.StartsWith("_") ? "_" : "*";
            var content = StringBuffer.Empty;
            content += symbol;
            content += RenderInlineTokens(token.Content, render);
            content += symbol;
            return content;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            if (token.Rule is MarkdownLHeadingBlockRule)
            {
                return RenderLHeadingToken(render, token, context);
            }
            else
            {
                return RenderHeadingToken(render, token, context);
            }
        }

        private StringBuffer RenderHeadingToken(IMarkdownRenderer render, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            var source = token.SourceInfo.Markdown;
            var match = _headingRegex.Match(source);
            if (match.Success)
            {
                var result = StringBuffer.Empty;
                var whitespace = match.Groups["whitespace"].Value;
                var content = RenderInlineTokens(token.Content.Tokens, render);

                result += match.Groups["pre"].Value;
                if (string.IsNullOrEmpty(whitespace))
                {
                    result += " ";
                }
                result += content;
                result += match.Groups["post"].Value;

                return result;
            }

            return base.Render(render, token, context);

        }

        private StringBuffer RenderLHeadingToken(IMarkdownRenderer render, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            var source = token.SourceInfo.Markdown;
            var match = _lheading.Match(source);
            if (match.Success)
            {
                var result = RenderInlineTokens(token.Content.Tokens, render);
                result += match.Groups["post"].Value;

                return result;
            }

            return base.Render(render, token, context);
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            const int SpaceCount = 2;
            var rowCount = token.Cells.Length + 2;
            var columnCount = token.Header.Length;
            var maxLengths = new int[columnCount];
            var matrix = new StringBuffer[rowCount, columnCount];

            for (var column = 0; column < columnCount; column++)
            {
                var header = token.Header[column];
                var content = RenderInlineTokens(header.Content.Tokens, render);
                matrix[0, column] = content;
                maxLengths[column] = Math.Max(1, content.GetLength()) + SpaceCount;
            }

            for (var row = 0; row < token.Cells.Length; row++)
            {
                var cell = token.Cells[row];
                for (var column = 0; column < columnCount; column++)
                {
                    var item = cell[column];
                    var content = RenderInlineTokens(item.Content.Tokens, render);
                    matrix[row + 2, column] = content;
                    maxLengths[column] = Math.Max(maxLengths[column], content.GetLength() + SpaceCount);
                }
            }

            for (var column = 0; column < columnCount; column++)
            {
                var align = token.Align[column];
                switch (align)
                {
                    case Align.NotSpec:
                        matrix[1, column] = "---";
                        break;
                    case Align.Left:
                        matrix[1, column] = ":--";
                        break;
                    case Align.Right:
                        matrix[1, column] = "--:";
                        break;
                    case Align.Center:
                        matrix[1, column] = ":-:";
                        break;
                    default:
                        throw new NotSupportedException($"align:{align} doesn't support in GFM table");
                }
            }

            return BuildTable(matrix, maxLengths, rowCount, columnCount);
        }

        private StringBuffer BuildTable(StringBuffer[,] matrix, int[] maxLenths, int rowCount, int nCol)
        {
            var content = StringBuffer.Empty;
            for (var row = 0; row < rowCount; row++)
            {
                content += "|";
                for (var j = 0; j < nCol; j++)
                {
                    var align = matrix[1, j];
                    if (row == 1)
                    {
                        content += BuildAlign(align, maxLenths[j]);
                    }
                    else
                    {
                        content += BuildItem(align, matrix[row, j], maxLenths[j]);
                    }
                    content += "|";
                }
                content += "\n";
            }

            return content + "\n";
        }

        private string BuildAlign(StringBuffer align, int maxLength)
        {
            switch (align)
            {
                case "---":
                    return new string('-', maxLength);
                case ":--":
                    return ":" + new string('-', maxLength - 1);
                case "--:":
                    return new string('-', maxLength - 1) + ":";
                case ":-:":
                    return ":" + new string('-', maxLength - 2) + ":";
                default:
                    throw new NotSupportedException($"align:{align} doesn't support in GFM table");
            }
        }

        private StringBuffer BuildItem(StringBuffer align, StringBuffer value, int maxLength)
        {
            var length = value.GetLength();
            var totalPad = maxLength - value.GetLength();

            switch (align)
            {
                case "---":
                case ":-:":
                    var leftPad = totalPad / 2;
                    return BuildItem(value, leftPad, totalPad - leftPad);
                case ":--":
                    return BuildItem(value, 1, totalPad - 1);
                case "--:":
                    return BuildItem(value, totalPad - 1, 1);
                default:
                    throw new NotSupportedException($"align:{align} doesn't support in GFM table");
            }
        }

        private StringBuffer BuildItem(StringBuffer value, int leftPad, int rightPad)
        {
            var leftValue = leftPad == 1 ? " " : new string(' ', leftPad);
            var rightValue = rightPad == 1 ? " " : new string(' ', rightPad);
            return StringBuffer.Empty + leftValue + value + rightValue;
        }

        private StringBuffer MarkupInlineTokens(IMarkdownRenderer render, ImmutableArray<IMarkdownToken> tokens)
        {
            var result = StringBuffer.Empty;
            if (tokens != null)
            {
                foreach (var t in tokens)
                {
                    result += MarkupInlineToken(render, t);
                }
            }

            return result;
        }

        private StringBuffer MarkupInlineToken(IMarkdownRenderer render, IMarkdownToken token)
        {
            return _dfmHtmlRender.Render((dynamic)render, (dynamic)token, (dynamic)token.Context);
        }

        private StringBuffer RenderInlineTokens(ImmutableArray<IMarkdownToken> tokens, IMarkdownRenderer render)
        {
            var result = StringBuffer.Empty;
            if (tokens != null)
            {
                foreach (var t in tokens)
                {
                    result += render.Render(t);
                }
            }

            return result;
        }

        private bool TryResolveUid(string uid)
        {
            var task = CanResolveUidWithRetryAsync(uid);
            return task.Result;
        }

        private async Task<bool> CanResolveUidWithRetryAsync(string uid)
        {
            var retryCount = 3;
            var delay = TimeSpan.FromSeconds(3);

            var count = 1;
            while (true)
            {
                try
                {
                    return await CanResolveUidAsync(uid);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error occured while resolving uid:{uid}: ${ex.Message}. Retry {count}");

                    if (count >= retryCount)
                    {
                        throw;
                    }

                    count++;
                }

                await Task.Delay(delay);
            }
        }

        private async Task<bool> CanResolveUidAsync(string uid)
        {
            var requestUrl = string.Format(_requestTemplate, Uri.EscapeDataString(uid));
            using (var response = await _client.GetAsync(requestUrl))
            {
                response.EnsureSuccessStatusCode();
                using (var content = response.Content)
                {
                    var result = await content.ReadAsStringAsync();
                    if (!string.Equals("[]", result))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
