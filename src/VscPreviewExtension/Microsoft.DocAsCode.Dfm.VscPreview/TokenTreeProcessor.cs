// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    public class TokenTreeProcessor
    {
        private readonly IMarkdownService _dfmMarkdownService;

        public TokenTreeProcessor()
        {
            DfmJsonTokenTreeServiceProvider dfmJsonTokenTreeServiceProvider = new DfmJsonTokenTreeServiceProvider();
            _dfmMarkdownService = dfmJsonTokenTreeServiceProvider.CreateMarkdownService(new MarkdownServiceParameters());
        }

        public string TokenTreePreview(string rawMarkdownContent)
        {
            return _dfmMarkdownService.Markup(rawMarkdownContent, null).Html;
        }
    }
}
