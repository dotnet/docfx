// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMarkdownRenderer : DfmMarkdownRenderer
    {
        public virtual StringBuffer Render(IMarkdownRenderer render, AzureIncludeInlineToken token, MarkdownInlineContext context)
        {
            return RenderAzureIncludeToken(render, token, context);
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, AzureIncludeBlockToken token, MarkdownBlockContext context)
        {
            // Don't add \n\n here because if we render AzureIncludeBlockToken, it will always be wrappered by paragraph. Which already append \n\n
            return RenderAzureIncludeToken(render, token, context);
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, AzureVideoBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = StringBuffer.Empty;

            if (!context.Variables.TryGetValue("path", out object path))
            {
                path = string.Empty;
                content += token.SourceInfo.Markdown;
                return content += "\n\n";
            }

            if (!context.Variables.ContainsKey("azureVideoInfoMapping"))
            {
                Logger.LogWarning($"Can't fild azure video info mapping. Raw: {token.SourceInfo.Markdown}");
                content = token.SourceInfo.Markdown;
                return content + "\n\n";
            }

            var azureVideoInfoMapping = (IReadOnlyDictionary<string, AzureVideoInfo>)context.Variables["azureVideoInfoMapping"];
            if (azureVideoInfoMapping == null || !azureVideoInfoMapping.ContainsKey(token.VideoId))
            {
                Logger.LogWarning($"Can't fild azure video info mapping for file {path}. Raw: {token.SourceInfo.Markdown}");
                content = token.SourceInfo.Markdown;
                return content + "\n\n";
            }

            var azureVideoInfo = azureVideoInfoMapping[token.VideoId];
            content += $@"<iframe width=""{azureVideoInfo.Width}"" height=""{azureVideoInfo.Height}"" src=""{azureVideoInfo.Link}"" frameborder=""0"" allowfullscreen=""true""></iframe>";
            return content + "\n\n";
        }

        private StringBuffer RenderAzureIncludeToken(IMarkdownRenderer render, AzureIncludeBasicToken token, IMarkdownContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            foreach(var t in token.Tokens)
            {
                content += render.Render(t);
            }
            return content;
        }
    }
}
