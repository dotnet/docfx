// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    public class TokenTreeProcessor
    {
        public static string TokenTreePreview(string rawMarkdownContent)
        {
            DfmJsonTokenTreeServiceProvider dfmJsonTokenTreeServiceProvider = new DfmJsonTokenTreeServiceProvider();
            IMarkdownService dfmMarkdownService = dfmJsonTokenTreeServiceProvider.CreateMarkdownService(new MarkdownServiceParameters());
            return dfmMarkdownService.Markup(rawMarkdownContent, null).Html;
        }
    }
}
