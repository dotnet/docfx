// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    public class TokenTreeProcessor
    {
        private static readonly DfmJsonTokenTreeServiceProvider DfmJsonTokenTreeServiceProvider = new DfmJsonTokenTreeServiceProvider();
        private static readonly IMarkdownService DfmMarkdownService = DfmJsonTokenTreeServiceProvider.CreateMarkdownService(new MarkdownServiceParameters());

        public static string TokenTreePreview(string rawMarkdownContent)
        {
            return DfmMarkdownService.Markup(rawMarkdownContent, null).Html;
        }
    }
}
