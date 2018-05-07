// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Composition;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export("jsonTokenTree", typeof(IMarkdownServiceProvider))]
    public class JsonTokenTreeServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new JsonTokenTreeService();
        }

        private sealed class JsonTokenTreeService : IMarkdownService
        {
            public string Name => "jsonTokenTree";

            private static GfmEngineBuilder builder { get; } = new GfmEngineBuilder(new Options { XHtml = true, Mangle = false });

            private static JsonTokenTreeRenderer Renderer { get; } = new JsonTokenTreeRenderer();

            public MarkupResult Markup(string src, string path)
            {
                var json = builder.CreateEngine(Renderer).Markup(src, path);
                if (json.Length != 0 && json.EndsWith(","))
                {
                    json = json.Remove(json.Length - 1);
                }
                return new MarkupResult
                {
                    // TODO: rename
                    Html = $"{{\"name\":\"0>0>markdown\",\"children\":[{json}]}}",
                };
            }

            public MarkupResult Markup(string src, string path, bool enableValidation)
            {
                throw new NotImplementedException();
            }
        }
    }
}
