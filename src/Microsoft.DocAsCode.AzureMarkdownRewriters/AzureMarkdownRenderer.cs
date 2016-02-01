// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
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

        private StringBuffer RenderAzureIncludeToken(AzureIncludeBasicToken token, IMarkdownContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            object path;
            if (!context.Variables.TryGetValue("path", out path))
            {
                Logger.LogWarning($"Can't get path for the file that ref azure include file {token.Src}. Raw: {token.RawMarkdown}");
                return null;
            }

            if (PathUtility.IsRelativePath(token.Src))
            {
                var includeFilePath = Path.Combine(Path.GetDirectoryName(path.ToString()), token.Src);
                if (!File.Exists(includeFilePath))
                {
                    Logger.LogWarning($"Can't get include file in path {includeFilePath}. Raw: {token.RawMarkdown}");
                    return null;
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
