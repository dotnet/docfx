// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Composition;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export("gfm", typeof(IMarkdownServiceProvider))]
    public class GfmServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new GfmService();
        }

        private sealed class GfmService : IMarkdownService
        {
            private static GfmEngineBuilder Builder { get; } = new GfmEngineBuilder(new Options { XHtml = true, Mangle = false });
            private static HtmlRenderer Renderer { get; } = new HtmlRenderer();

            public string Markup(string src, string path)
            {
                return Builder.CreateEngine(Renderer).Markup(src);
            }
        }
    }
}
