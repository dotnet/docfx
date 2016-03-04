// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class AzureMarkdownRenderer : DfmMarkdownRenderer
    {
        public virtual StringBuffer Render(IMarkdownRenderer render, AzureIncludeInlineToken token, MarkdownInlineContext context)
        {
            return RenderAzureIncludeToken(token, context);
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, AzureIncludeBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = RenderAzureIncludeToken(token, context);
            return content + "\n\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, AzureVideoBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = StringBuffer.Empty;

            object path;
            if (!context.Variables.TryGetValue("path", out path))
            {
                path = string.Empty;
            }

            if (!context.Variables.ContainsKey("azureVideoInfoMapping"))
            {
                Logger.LogWarning($"Can't fild azure video info mapping. Raw: {token.RawMarkdown}");
                content = token.RawMarkdown;
                return content + "\n\n";
            }

            var azureVideoInfoMapping = (IReadOnlyDictionary<string, AzureVideoInfo>)context.Variables["azureVideoInfoMapping"];
            if (azureVideoInfoMapping == null || !azureVideoInfoMapping.ContainsKey(token.VideoId))
            {
                Logger.LogWarning($"Can't fild azure video info mapping for file {path}. Raw: {token.RawMarkdown}");
                content = token.RawMarkdown;
                return content + "\n\n";
            }

            var azureVideoInfo = azureVideoInfoMapping[token.VideoId];
            content += $@"<iframe width=""{azureVideoInfo.Width}"" height=""{azureVideoInfo.Height}"" src=""{azureVideoInfo.Link}"" frameborder=""0"" allowfullscreen=""true""></iframe>";
            return content + "\n\n";
        }

        private StringBuffer RenderAzureIncludeToken(AzureIncludeBasicToken token, IMarkdownContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            object path;
            if (!context.Variables.TryGetValue("path", out path))
            {
                Logger.LogWarning($"Can't get path for the file that ref azure include file {token.Src}. Raw: {token.RawMarkdown}");
                return token.RawMarkdown;
            }

            if (PathUtility.IsRelativePath(token.Src))
            {
                var includeFilePath = Path.Combine(Path.GetDirectoryName(path.ToString()), token.Src);
                if (!File.Exists(includeFilePath))
                {
                    Logger.LogWarning($"Can't get include file in path {includeFilePath}. Raw: {token.RawMarkdown}");
                    return token.RawMarkdown;
                }

                // TODO: We should handle Azure syntax in the include file. Such as Azure Selector
                content += File.ReadAllText(includeFilePath);
            }
            else
            {
                Logger.LogWarning($"include path {token.Src} is not a relative path, can't expand it");
                return null;
            }
            return content;
        }
    }
}
