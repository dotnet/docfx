// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.MergeOverwrite
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export("momd", typeof(IMarkdownServiceProvider))]
    public class MergeOverwriteMarkdownServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            IReadOnlyList<string> fallbackFolders = null;
            if (parameters.Extensions != null && parameters.Extensions.TryGetValue("fallbackFolders", out object obj))
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
                TokenTreeValidator,
                fallbackFolders);
        }

        [ImportMany]
        public IEnumerable<IMarkdownTokenTreeValidator> TokenTreeValidator { get; set; }

        private sealed class DfmService : IMarkdownService
        {
            public string Name => "dfm";

            private readonly DfmEngineBuilder _builder;

            private readonly ImmutableDictionary<string, string> _tokens;

            private readonly YamlHeaderMarkdownRenderer _renderer;

            public DfmService(string baseDir, string templateDir, ImmutableDictionary<string, string> tokens, IEnumerable<IMarkdownTokenTreeValidator> tokenTreeValidator, IReadOnlyList<string> fallbackFolders = null)
            {
                var options = DocfxFlavoredMarked.CreateDefaultOptions();
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir, templateDir, options, fallbackFolders);
                _builder.TokenTreeValidator = MarkdownTokenTreeValidatorFactory.Combine(tokenTreeValidator);
                _tokens = tokens;
                _renderer = new YamlHeaderMarkdownRenderer();
            }

            public MarkupResult Markup(string src, string path)
            {
                var dependency = new HashSet<string>();
                var html = _builder.CreateDfmEngine(_renderer).Markup(src, path, dependency);
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

            public MarkupResult Markup(string src, string path, bool enableValidation)
            {
                throw new NotImplementedException();
            }
        }

        public class YamlHeaderMarkdownRenderer
        {
            private readonly MarkdownRendererAdapter _dfmMarkdownRenderer = new MarkdownRendererAdapter(null, new DfmMarkdownRenderer(), null, null);

            public StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
            {
                if (token is DfmYamlHeaderBlockToken yamlHeader)
                {
                    return RenderYamlHeader(render, yamlHeader, context);
                }

                return StringHelper.HtmlEncode(_dfmMarkdownRenderer.Render(token));
            }

            private StringBuffer RenderYamlHeader(IMarkdownRenderer render, DfmYamlHeaderBlockToken token, IMarkdownContext context)
            {
                if (string.IsNullOrEmpty(token.Content))
                {
                    return StringBuffer.Empty;
                }
                var startLine = token.SourceInfo.LineNumber;
                var endLine = startLine + token.Content.Count(ch => ch == '\n') + 2;
                var sourceFile = token.SourceInfo.File;

                StringBuffer result = $"<yamlheader start=\"{startLine}\" end=\"{endLine}\"";
                if (!string.IsNullOrEmpty(sourceFile))
                {
                    sourceFile = StringHelper.HtmlEncode(sourceFile);
                    result += $" sourceFile=\"{sourceFile}\"";
                }
                result += ">";
                result += StringHelper.HtmlEncode(token.Content);
                return result + "</yamlheader>";
            }
        }
    }
}
