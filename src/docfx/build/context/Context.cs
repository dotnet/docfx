// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

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

        public XrefResolver XrefResolver => _xrefResolver.Value;

        public TableOfContentsMap TocMap => _tocMap.Value;

        private readonly Lazy<XrefResolver> _xrefResolver;
        private readonly Lazy<TableOfContentsMap> _tocMap;

        public Context(string outputPath, ErrorLog errorLog, Docset docset, Docset fallbackDocset, RestoreGitMap restoreGitMap)
        {
            var restoreFileMap = new RestoreFileMap(docset.DocsetPath, fallbackDocset?.DocsetPath);
            DependencyMapBuilder = new DependencyMapBuilder();
            _xrefResolver = new Lazy<XrefResolver>(() => new XrefResolver(this, docset, restoreFileMap, DependencyMapBuilder));
            _tocMap = new Lazy<TableOfContentsMap>(() => TableOfContentsMap.Create(this));
            BuildQueue = new WorkQueue<Document>();

            ErrorLog = errorLog;
            Output = new Output(outputPath);
            Cache = new Cache();
            TemplateEngine = TemplateEngine.Create(docset, restoreGitMap);
            BuildScope = new BuildScope(errorLog, docset, fallbackDocset, TemplateEngine);
            MicrosoftGraphCache = new MicrosoftGraphCache(docset.Config);
            MetadataProvider = new MetadataProvider(docset, Cache, MicrosoftGraphCache, restoreFileMap);
            MonikerProvider = new MonikerProvider(docset, MetadataProvider, restoreFileMap);
            GitHubUserCache = new GitHubUserCache(docset.Config);
            GitCommitProvider = new GitCommitProvider();
            PublishModelBuilder = new PublishModelBuilder();
            BookmarkValidator = new BookmarkValidator(errorLog, PublishModelBuilder);
            ContributionProvider = new ContributionProvider(docset, fallbackDocset, GitHubUserCache, GitCommitProvider);

            DependencyResolver = new DependencyResolver(
                docset,
                fallbackDocset,
                BuildScope,
                BuildQueue,
                GitCommitProvider,
                BookmarkValidator,
                restoreGitMap,
                DependencyMapBuilder,
                _xrefResolver,
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
