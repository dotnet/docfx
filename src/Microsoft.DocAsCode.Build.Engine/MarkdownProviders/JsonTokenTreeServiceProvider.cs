// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Text;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export("JsonTokenTree", typeof(IMarkdownServiceProvider))]
    public class JsonTokenTreeServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new JsonTokenTreeService(
                parameters.BasePath,
                parameters.Tokens,
                MarkdownTokenTreeValidatorFactory.Combine(TokenTreeValidator));
        }

        [ImportMany]
        public IEnumerable<IMarkdownTokenTreeValidator> TokenTreeValidator { get; set; }

        private sealed class JsonTokenTreeService : IMarkdownService
        {
            private readonly DfmEngineBuilder _builder;

            private readonly ImmutableDictionary<string, string> _tokens;

            public JsonTokenTreeService(string baseDir, ImmutableDictionary<string, string> tokens, IMarkdownTokenTreeValidator tokenTreeValidator)
            {
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir);
                _builder.TokenTreeValidator = tokenTreeValidator;
                _tokens = tokens;
            }

            public MarkupResult Markup(string src, string path)
            {
                var dependency = new HashSet<string>();
                var json = _builder.CreateDfmEngine(new JsonTokenTreeRenderer()).Markup(src, path, dependency);
                if (json.Length != 0 && json.EndsWith(","))
                {
                    json = json.Remove(json.Length - 1);
                }
                var result = new MarkupResult
                {
                    // TODO: rename
                    Html = $"{{\"name\":\"markdown_0\",\"children\":[{json}]}}",
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
