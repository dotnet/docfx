// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// An immutable set of behavioral classes that are commonly used by the build pipeline.
    /// </summary>
    internal sealed class Context : IDisposable
    {
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
        public readonly GitHubUserCache GitHubUserCache;
        public readonly MicrosoftGraphCache MicrosoftGraphCache;
        public readonly ContributionProvider ContributionProvider;
        public readonly PublishModelBuilder PublishModelBuilder;
        public readonly TemplateEngine TemplateEngine;

        public XrefMap XrefMap => _xrefMap.Value;

        public TableOfContentsMap TocMap => _tocMap.Value;

        private readonly Lazy<XrefMap> _xrefMap;
        private readonly Lazy<TableOfContentsMap> _tocMap;

        public Context(string outputPath, ErrorLog errorLog, Docset docset, Docset fallbackDocset, RestoreGitMap restoreGitMap)
        {
            var restoreFileMap = new RestoreFileMap(docset.DocsetPath, fallbackDocset?.DocsetPath);
            DependencyMapBuilder = new DependencyMapBuilder();
            _xrefMap = new Lazy<XrefMap>(() => new XrefMap(this, docset, restoreFileMap, DependencyMapBuilder));
            _tocMap = new Lazy<TableOfContentsMap>(() => TableOfContentsMap.Create(this));
            BuildQueue = new WorkQueue<Document>();

            ErrorLog = errorLog;
            Output = new Output(outputPath);
            Input = new Input(docset.DocsetPath, fallbackDocset?.DocsetPath, docset.Config, restoreGitMap);
            Cache = new Cache(Input);
            TemplateEngine = TemplateEngine.Create(docset, restoreGitMap);
            BuildScope = new BuildScope(errorLog, docset, fallbackDocset, TemplateEngine);
            MicrosoftGraphCache = new MicrosoftGraphCache(docset.Config);
            MetadataProvider = new MetadataProvider(docset, Input, Cache, MicrosoftGraphCache, restoreFileMap);
            MonikerProvider = new MonikerProvider(docset, MetadataProvider, restoreFileMap);
            GitHubUserCache = new GitHubUserCache(docset.Config);
            GitCommitProvider = new GitCommitProvider();
            PublishModelBuilder = new PublishModelBuilder();
            BookmarkValidator = new BookmarkValidator(errorLog, PublishModelBuilder);
            ContributionProvider = new ContributionProvider(docset, fallbackDocset, GitHubUserCache, GitCommitProvider);

            DependencyResolver = new DependencyResolver(
                docset,
                fallbackDocset,
                Input,
                BuildScope,
                BuildQueue,
                GitCommitProvider,
                BookmarkValidator,
                restoreGitMap,
                DependencyMapBuilder,
                _xrefMap,
                TemplateEngine);
        }

        public void Dispose()
        {
            GitCommitProvider.Dispose();
            GitHubUserCache.Dispose();
            MicrosoftGraphCache.Dispose();
        }
    }
}
