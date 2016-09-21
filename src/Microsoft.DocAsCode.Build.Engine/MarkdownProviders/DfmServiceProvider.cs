// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export("dfm", typeof(IMarkdownServiceProvider))]
    public class DfmServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            IReadOnlyList<string> fallbackFolders = null;
            object obj;
            if (parameters.Extensions.TryGetValue("fallbackFolders", out obj))
            {
                try
                {
                    fallbackFolders = ((IEnumerable)obj).Cast<string>().ToList();
                }
                catch
                {
                    // Swallow cast exception. 
                }
            }

            return new DfmService(
                parameters.BasePath,
                parameters.TemplateDir,
                parameters.Tokens,
                MarkdownTokenTreeValidatorFactory.Combine(TokenTreeValidator),
                fallbackFolders);
        }

        [ImportMany]
        public IEnumerable<IMarkdownTokenTreeValidator> TokenTreeValidator { get; set; }

        private sealed class DfmService : IMarkdownService
        {
            private readonly DfmEngineBuilder _builder;

            private readonly ImmutableDictionary<string, string> _tokens;

            public DfmService(string baseDir, string templateDir, ImmutableDictionary<string, string> tokens, IMarkdownTokenTreeValidator tokenTreeValidator, IReadOnlyList<string> fallbackFolders = null)
            {
                var options = DocfxFlavoredMarked.CreateDefaultOptions();
                options.ShouldExportSourceInfo = true;
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir, templateDir, options, fallbackFolders);
                _builder.TokenTreeValidator = tokenTreeValidator;
                _tokens = tokens;
            }

            public MarkupResult Markup(string src, string path)
            {
                var dependency = new HashSet<string>();
                var html = _builder.CreateDfmEngine(new DfmRenderer() { Tokens = _tokens }).Markup(src, path, dependency);
                var result = new MarkupResult
                {
                    Html = html,
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
