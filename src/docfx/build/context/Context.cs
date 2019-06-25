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
        public readonly ContributionProvider ContributionProvider;
        public readonly PublishModelBuilder PublishModelBuilder;
        public readonly TemplateEngine Template;

        public XrefMap XrefMap => _xrefMap.Value;

        public TableOfContentsMap TocMap => _tocMap.Value;

        private readonly Lazy<XrefMap> _xrefMap;
        private readonly Lazy<TableOfContentsMap> _tocMap;

        public Context(string outputPath, ErrorLog errorLog, Docset docset, Func<Context, Document, Task> buildFile)
        {
            _xrefMap = new Lazy<XrefMap>(() => XrefMapBuilder.Build(this, docset));
            _tocMap = new Lazy<TableOfContentsMap>(() => TableOfContentsMap.Create(this));
            BuildQueue = new WorkQueue<Document>(doc => buildFile(this, doc));

            ErrorLog = errorLog;
            Output = new Output(outputPath);
            Cache = new Cache();
            BuildScope = new BuildScope(errorLog, docset);
            MetadataProvider = new MetadataProvider(docset, Cache);
            MonikerProvider = new MonikerProvider(docset, MetadataProvider);
            GitHubUserCache = new GitHubUserCache(docset.Config);
            GitCommitProvider = new GitCommitProvider();
            PublishModelBuilder = new PublishModelBuilder();
            BookmarkValidator = new BookmarkValidator();
            DependencyMapBuilder = new DependencyMapBuilder();
            DependencyResolver = new DependencyResolver(
                docset.Config, BuildScope, BuildQueue, GitCommitProvider, BookmarkValidator, DependencyMapBuilder, _xrefMap);
            ContributionProvider = new ContributionProvider(docset, GitHubUserCache, GitCommitProvider);
            Template = TemplateEngine.Create(docset);
        }

        public void Dispose()
        {
            GitCommitProvider.Dispose();
            GitHubUserCache.Dispose();
        }
    }
}
