// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigMarkdownRewriters
{
    using System;
    using System.Collections.Immutable;
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
            for (var index = 0; index < token.Tokens.Length; index++)
            {
                var t = token.Tokens[index];
                if (index == token.Tokens.Length - 1 && t is DfmVideoBlockToken videoToken)
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
            var nSpace = 2;
            var nRow = token.Cells.Length + 2;
            var nCol = token.Header.Length;
            var maxLengths = new int[nCol];
            var matrix = new StringBuffer[nRow, nCol];

            for (var j = 0; j < nCol; j++)
            {
                var header = token.Header[j];
                var content = RenderInlineTokens(header.Content.Tokens, render);
                matrix[0, j] = content;
                maxLengths[j] = Math.Max(3, content.GetLength()) + nSpace;
            }

            for (var i = 0; i < token.Cells.Length; i++)
            {
                var cell = token.Cells[i];
                for (var j = 0; j < nCol; j++)
                {
                    var item = cell[j];
                    var content = RenderInlineTokens(item.Content.Tokens, render);
                    matrix[i + 2, j] = content;
                    maxLengths[j] = Math.Max(maxLengths[j], content.GetLength() + nSpace);
                }
            }

            for (var j = 0; j < nCol; j++)
            {
                var align = token.Align[j];
                switch (align)
                {
                    case Align.NotSpec:
                       matrix[1, j] = "---";
                        break;
                    case Align.Left:
                        matrix[1, j] = ":--";
                        break;
                    case Align.Right:
                        matrix[1, j] = "--:";
                        break;
                    case Align.Center:
                        matrix[1, j] = ":-:";
                        break;
                    default:
                        throw new NotSupportedException($"align:{align} doesn't support in GFM table");
                }
            }

            return BuildTable(matrix, maxLengths, nRow, nCol);
        }

        private StringBuffer BuildTable(StringBuffer[,] matrix, int[] maxLenths, int nRow, int nCol)
        {
            var content = StringBuffer.Empty;
            for (var i = 0; i < nRow; i++)
            {
                content += "|";
                for (var j = 0; j < nCol; j++)
                {
                    var align = matrix[1, j];
                    var item = i == 1 ? BuildAlign(align, maxLenths[j])
                                      : BuildItem(align, matrix[i, j], maxLenths[j]);
 
                    content += item;
                    content += "|";
                }
                content += "\n";
            }

            return content + "\n";
        }

        private StringBuffer BuildAlign(StringBuffer align, int maxLength)
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
            var leftPad = (maxLength - value.GetLength()) / 2;
            Func<int, int> GetRightPad = left => maxLength - left - length;

            switch (align)
            {
                case "---":
                case ":-:":
                    return BuildItem(value, leftPad, GetRightPad(leftPad));
                case ":--":
                    leftPad = 1;
                    return BuildItem(value, leftPad, GetRightPad(leftPad));
                case "--:":
                    leftPad = 1;
                    return BuildItem(value, GetRightPad(leftPad), leftPad);
                default:
                    throw new NotSupportedException($"align:{align} doesn't support in GFM table");
            }
        }

        private StringBuffer BuildItem(StringBuffer value, int leftPad, int rightPad)
        {
            var leftValue = new string(' ', leftPad);
            var rightValue = new string(' ', rightPad);
            return leftValue + value + rightValue;
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
