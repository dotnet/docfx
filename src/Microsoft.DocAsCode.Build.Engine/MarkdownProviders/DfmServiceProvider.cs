// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Immutable;
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

            private readonly ImmutableDictionary<string, string> _tokens;

            public DfmService(string baseDir, ImmutableDictionary<string, string> tokens)
            {
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir);
                _tokens = tokens;
            }

            public string Markup(string src, string path)
            {
                return _builder.CreateDfmEngine(new DfmRenderer() { Tokens = _tokens }).Markup(src, path);
            }
        }
    }
}
