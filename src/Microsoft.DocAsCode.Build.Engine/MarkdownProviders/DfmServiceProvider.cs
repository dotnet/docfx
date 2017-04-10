// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export("dfm-latest", typeof(IMarkdownServiceProvider))]
    [Export("dfm-2.15", typeof(IMarkdownServiceProvider))]
    public class DfmServiceProvider : IMarkdownServiceProvider
    {
        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            IReadOnlyList<string> fallbackFolders = null;
            object obj;
            if (parameters.Extensions != null && parameters.Extensions.TryGetValue("fallbackFolders", out obj))
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
                fallbackFolders,
                DfmRendererPartProviders,
                parameters.Extensions);
        }

        [ImportMany]
        public IEnumerable<IMarkdownTokenTreeValidator> TokenTreeValidator { get; set; }

        [ImportMany]
        public IEnumerable<IDfmRendererPartProvider> DfmRendererPartProviders { get; set; }

        private sealed class DfmService : IMarkdownService, IHasIncrementalContext, IDisposable
        {
            private readonly DfmEngineBuilder _builder;

            private readonly ImmutableDictionary<string, string> _tokens;

            private readonly DfmRenderer _baseRenderer;
            private readonly object _renderer;

            private readonly string _incrementalContextHash;

            public DfmService(
                string baseDir,
                string templateDir,
                ImmutableDictionary<string, string> tokens,
                IEnumerable<IMarkdownTokenTreeValidator> tokenTreeValidator,
                IReadOnlyList<string> fallbackFolders,
                IEnumerable<IDfmRendererPartProvider> dfmRendererPartProviders,
                IReadOnlyDictionary<string, object> parameters)
            {
                var options = DocfxFlavoredMarked.CreateDefaultOptions();
                options.ShouldExportSourceInfo = true;
                _builder = DocfxFlavoredMarked.CreateBuilder(baseDir, templateDir, options, fallbackFolders);
                _builder.TokenTreeValidator = MarkdownTokenTreeValidatorFactory.Combine(tokenTreeValidator);
                _tokens = tokens;
                _baseRenderer = new DfmRenderer { Tokens = _tokens };
                _renderer = RendererCreator.CreateRenderer(_baseRenderer, dfmRendererPartProviders, parameters);
                _incrementalContextHash = ComputeIncrementalContextHash(baseDir, templateDir, tokenTreeValidator);
            }

            private static string ComputeIncrementalContextHash(string baseDir, string templateDir, IEnumerable<IMarkdownTokenTreeValidator> tokenTreeValidator)
            {
                var content = (StringBuffer)"dfm";
                if (baseDir != null)
                {
                    if (File.Exists(Path.Combine(baseDir, "md.style")))
                    {
                        content += "::";
                        content += File.ReadAllText(Path.Combine(baseDir, "md.style"));
                    }
                }
                if (templateDir != null)
                {
                    var templateStylesFolder = Path.Combine(templateDir, "md.styles");
                    if (Directory.Exists(templateStylesFolder))
                    {
                        foreach (var f in Directory.GetFiles(templateStylesFolder).OrderBy(f => f))
                        {
                            content += "::";
                            content += f.Substring(templateStylesFolder.Length);
                            content += "::";
                            content += File.ReadAllText(f);
                        }
                    }
                }

                var contentText = content.ToString();
                Logger.LogVerbose($"Dfm config content: {content}");
                return contentText.GetMd5String();
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

            public string GetIncrementalContextHash() => _incrementalContextHash;

            public void Dispose()
            {
                _baseRenderer.Dispose();
            }
        }
    }
}
