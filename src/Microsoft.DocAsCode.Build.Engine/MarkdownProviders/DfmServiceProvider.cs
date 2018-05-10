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
            object obj = null;
            if (parameters.Extensions?.TryGetValue("fallbackFolders", out obj) == true)
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

            if (parameters.Extensions?.TryGetValue("shouldFixId", out obj) == true)
            {
                ShouldFixId = obj as bool? ?? true;
            }

            return new DfmService(
                this,
                parameters.BasePath,
                parameters.TemplateDir,
                Container,
                parameters.Tokens,
                fallbackFolders,
                parameters.Extensions);
        }

        protected virtual bool LegacyMode => false;

        protected virtual bool ShouldFixId { get; set; } = true;

        [ImportMany]
        public IEnumerable<IMarkdownTokenTreeValidator> TokenTreeValidator { get; set; } = Enumerable.Empty<IMarkdownTokenTreeValidator>();

        [ImportMany]
        public IEnumerable<IDfmCustomizedRendererPartProvider> DfmRendererPartProviders { get; set; } = Enumerable.Empty<IDfmCustomizedRendererPartProvider>();

        [ImportMany]
        public IEnumerable<IDfmEngineCustomizer> DfmEngineCustomizers { get; set; } = Enumerable.Empty<IDfmEngineCustomizer>();

        [Import]
        public ICompositionContainer Container { get; set; }

        public sealed class DfmService : IMarkdownService, IHasIncrementalContext, IDisposable
        {
            public string Name => "dfm";

            public DfmEngineBuilder Builder { get; }

            public object Renderer { get; }

            private readonly ImmutableDictionary<string, string> _tokens;

            private readonly string _incrementalContextHash;

            public DfmService(
                DfmServiceProvider provider,
                string baseDir,
                string templateDir,
                ICompositionContainer container,
                ImmutableDictionary<string, string> tokens,
                IReadOnlyList<string> fallbackFolders,
                IReadOnlyDictionary<string, object> parameters)
            {
                var options = DocfxFlavoredMarked.CreateDefaultOptions();
                options.LegacyMode = provider.LegacyMode;
                options.ShouldFixId = provider.ShouldFixId;
                options.ShouldExportSourceInfo = true;
                options.XHtml = true;
                Builder = new DfmEngineBuilder(
                    options,
                    baseDir,
                    templateDir,
                    fallbackFolders,
                    container)
                {
                    TokenTreeValidator = MarkdownTokenTreeValidatorFactory.Combine(provider.TokenTreeValidator)
                };
                _tokens = tokens;
                Renderer = CustomizedRendererCreator.CreateRenderer(
                    new DfmRenderer { Tokens = _tokens },
                    provider.DfmRendererPartProviders,
                    parameters);
                foreach (var c in provider.DfmEngineCustomizers)
                {
                    c.Customize(Builder, parameters);
                }
                _incrementalContextHash = ComputeIncrementalContextHash(baseDir, templateDir, provider.TokenTreeValidator, parameters);
            }

            private static string ComputeIncrementalContextHash(
                string baseDir,
                string templateDir,
                IEnumerable<IMarkdownTokenTreeValidator> tokenTreeValidator,
                IReadOnlyDictionary<string, object> parameters)
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
                if (parameters.Count > 0)
                {
                    content += "::" + nameof(parameters) + "::";
                    content += JsonUtility.Serialize(
                        parameters
                        .Where(p => p.Key != "fallbackFolders")
                        .ToDictionary(p => p.Key, p => p.Value));
                }
                var contentText = content.ToString();
                Logger.LogVerbose($"Dfm config content: {content}");
                return contentText.GetMd5String();
            }

            public MarkupResult Markup(string src, string path)
            {
                var dependency = new HashSet<string>();
                var html = Builder.CreateDfmEngine(Renderer).Markup(src, path, dependency);
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

            public string GetIncrementalContextHash() => _incrementalContextHash;

            public void Dispose()
            {
                (Renderer as IDisposable)?.Dispose();
            }
        }
    }
}
