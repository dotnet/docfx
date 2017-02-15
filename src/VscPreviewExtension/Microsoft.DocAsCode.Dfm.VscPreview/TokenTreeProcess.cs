// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using Microsoft.DocAsCode.Plugins;

    public class TokenTreeProcess : DocFxPreviewProcess
    {
        public static string TokenTreePreview(IMarkdownService dfmMarkdownService)
        {
            string markdownContent = GetMarkdownContent();
            var result = JsonMarkup(dfmMarkdownService, markdownContent.ToString());
            return result;
        }

        private static string JsonMarkup(IMarkdownService dfmMarkdownService, string markdownContent)
        {
            return dfmMarkdownService.Markup(markdownContent, null).Html;
        }
    }
}
