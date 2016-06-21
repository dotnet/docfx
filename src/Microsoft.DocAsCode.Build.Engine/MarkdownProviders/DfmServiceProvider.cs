﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.Composition;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Plugins;

    [Export("dfm", typeof(IMarkdownServiceProvider))]
    public class DfmServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new DfmService(parameters.BasePath, parameters.Tokens);
        }

        private sealed class DfmService : IMarkdownService
        {
            private readonly DfmEngineBuilder _builder;

            public DfmService(string baseDir, IDictionary<string, object> tokens)
            {
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir, tokens);
            }

            public string Markup(string src, string path)
            {
                return _builder.CreateDfmEngine(DocfxFlavoredMarked.Renderer).Markup(src, path);
            }
        }
    }
}
