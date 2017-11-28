// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigMarkdownRewriters
{
    using System;
    using System.Collections.Immutable;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    public class MarkdigMarkdownRenderer : DfmMarkdownRenderer
    {
        private static HttpClient _client = new HttpClient();

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
                    if (token.SourceInfo.Markdown.StartsWith("<mailto:", StringComparison.OrdinalIgnoreCase))
                    {
                        var mailText = RenderInlineTokens(token.Content, render, context);

                        return $"<{mailText}>";
                    }
                    goto default;

                case MarkdownLinkType.NormalLink:
                    var href = Uri.EscapeUriString(Uri.UnescapeDataString(token.Href ?? string.Empty));
                    var text = RenderInlineTokens(token.Content, render, context);

                    return $"[{text}]({href})";

                default:
                    return base.Render(render, token, context);
            }
        }

        public override StringBuffer Render(IMarkdownRenderer render, DfmVideoBlockToken token, MarkdownBlockContext context)
        {
            return $"[!VIDEO {token.Link}]";
        }

        private StringBuffer RenderInlineTokens(ImmutableArray<IMarkdownToken> tokens, IMarkdownRenderer render, MarkdownInlineContext context)
        {
            var result = StringBuffer.Empty;
            if (tokens != null)
            {
                foreach (var t in tokens)
                {
                    result += base.Render(render, t, context);
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
            for (; ; )
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
            var page = $"https://xref.docs.microsoft.com/query?uid={uid}";
            using (var response = await _client.GetAsync(page))
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
