// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export("dfmJsonTokenTree", typeof(IMarkdownServiceProvider))]
    public class DfmJsonTokenTreeServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new DfmJsonTokenTreeService(
                parameters.BasePath,
                MarkdownTokenTreeValidatorFactory.Combine(TokenTreeValidator));
        }

        [ImportMany]
        public IEnumerable<IMarkdownTokenTreeValidator> TokenTreeValidator { get; set; }

        private sealed class DfmJsonTokenTreeService : IMarkdownService
        {
            public string Name => "dfmJsonTokenTree";

            private readonly DfmEngineBuilder _builder;

            public DfmJsonTokenTreeService(string baseDir, IMarkdownTokenTreeValidator tokenTreeValidator)
            {
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir);
                _builder.TokenTreeValidator = tokenTreeValidator;
            }

            public MarkupResult Markup(string src, string path)
            {
                var dependency = new HashSet<string>();
                var json = _builder.CreateDfmEngine(new DfmJsonTokenTreeRender()).Markup(src, path, dependency);
                if (json.Length != 0 && json.EndsWith(",", StringComparison.Ordinal))
                {
                    json = json.Remove(json.Length - 1);
                }
                var result = new MarkupResult
                {
                    // TODO: rename
                    Html = $"{{\"name\":\"0>0>markdown\",\"children\":[{json}]}}",
                };
                if (dependency.Count > 0)
                {
                    result.Dependency = dependency.ToImmutableArray();
                }
                return result;
            }
        }
    }
}
