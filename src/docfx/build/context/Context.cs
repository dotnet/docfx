// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// An immutable set of behavioral classes that are commonly used by the build pipeline.
    /// </summary>
    internal sealed class Context : IDisposable
    {
        public readonly Config Config;
        public readonly RestoreFileMap RestoreFileMap;
        public readonly ErrorLog ErrorLog;
        public readonly Cache Cache;
        public readonly Output Output;
        public readonly Input Input;
        public readonly BuildScope BuildScope;
        public readonly WorkQueue<Document> BuildQueue;
        public readonly MetadataProvider MetadataProvider;
        public readonly MonikerProvider MonikerProvider;
        public readonly GitCommitProvider GitCommitProvider;
        public readonly BookmarkValidator BookmarkValidator;
        public readonly DependencyMapBuilder DependencyMapBuilder;
        public readonly DependencyResolver DependencyResolver;
        public readonly XrefResolver XrefResolver;
        public readonly GitHubUserCache GitHubUserCache;
        public readonly MicrosoftGraphCache MicrosoftGraphCache;
        public readonly ContributionProvider ContributionProvider;
        public readonly PublishModelBuilder PublishModelBuilder;
        public readonly MarkdownEngine MarkdownEngine;
        public readonly TemplateEngine TemplateEngine;
        public readonly FileLinkMapBuilder FileLinkMapBuilder;

        public TableOfContentsMap TocMap => _tocMap.Value;

        private readonly Lazy<TableOfContentsMap> _tocMap;

        public Context(string outputPath, ErrorLog errorLog, Docset docset, Docset fallbackDocset, Dictionary<string, (Docset docset, bool inScope)> dependencyDocsets, Input input, RepositoryProvider repositoryProvider)
        {
            var restoreFileMap = new RestoreFileMap(input);
            DependencyMapBuilder = new DependencyMapBuilder();
            _tocMap = new Lazy<TableOfContentsMap>(() => TableOfContentsMap.Create(this));
            BuildQueue = new WorkQueue<Document>();

            Config = docset.Config;
            RestoreFileMap = restoreFileMap;
            ErrorLog = errorLog;
            Input = input;
            Output = new Output(outputPath, input);
            Cache = new Cache(Input);
            TemplateEngine = TemplateEngine.Create(docset, repositoryProvider);
            MicrosoftGraphCache = new MicrosoftGraphCache(docset.Config);
            MetadataProvider = new MetadataProvider(docset, Input, Cache, MicrosoftGraphCache, restoreFileMap);
            MonikerProvider = new MonikerProvider(docset, MetadataProvider, restoreFileMap);
            BuildScope = new BuildScope(errorLog, Input, docset, fallbackDocset, dependencyDocsets, TemplateEngine, MonikerProvider);
            GitHubUserCache = new GitHubUserCache(docset.Config);
            GitCommitProvider = new GitCommitProvider();
            PublishModelBuilder = new PublishModelBuilder();
            BookmarkValidator = new BookmarkValidator(errorLog, PublishModelBuilder);
            ContributionProvider = new ContributionProvider(Input, docset, fallbackDocset, GitHubUserCache, GitCommitProvider);
            FileLinkMapBuilder = new FileLinkMapBuilder(MonikerProvider, errorLog);
            XrefResolver = new XrefResolver(this, docset, restoreFileMap, DependencyMapBuilder, FileLinkMapBuilder);

            DependencyResolver = new DependencyResolver(
                docset,
                fallbackDocset,
                dependencyDocsets.
                    ToDictionary(
                        k => k.Key,
                        v => v.Value.docset,
                        PathUtility.PathComparer),
                Input,
                BuildScope,
                BuildQueue,
                GitCommitProvider,
                BookmarkValidator,
                DependencyMapBuilder,
                XrefResolver,
                TemplateEngine,
                FileLinkMapBuilder);

            MarkdownEngine = new MarkdownEngine(Config, RestoreFileMap, DependencyResolver, XrefResolver, MonikerProvider, TemplateEngine);
        }

        public void Dispose()
        {
            GitCommitProvider.Dispose();
            GitHubUserCache.Dispose();
            MicrosoftGraphCache.Dispose();
        }
    }
}
