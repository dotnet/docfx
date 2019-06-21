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
        public readonly WorkQueue<Document> BuildQueue;
        public readonly MetadataProvider MetadataProvider;
        public readonly MonikerProvider MonikerProvider;
        public readonly GitCommitProvider GitCommitProvider;
        public readonly BookmarkValidator BookmarkValidator;
        public readonly DependencyMapBuilder DependencyMapBuilder;
        public readonly LinkResolver LinkResolver;
        public readonly GitHubUserCache GitHubUserCache;
        public readonly ContributionProvider ContributionProvider;
        public readonly PublishModelBuilder PublishModelBuilder;
        public readonly TemplateEngine Template;

        public Context(string outputPath, ErrorLog errorLog,  Docset docset, Func<XrefMap> xrefMap)
        {
            ErrorLog = errorLog;
            Output = new Output(outputPath);
            Cache = new Cache();
            BuildQueue = new WorkQueue<Document>();
            MetadataProvider = new MetadataProvider(docset, Cache);
            MonikerProvider = new MonikerProvider(docset, MetadataProvider);
            GitHubUserCache = new GitHubUserCache(docset.Config);
            GitCommitProvider = new GitCommitProvider();
            PublishModelBuilder = new PublishModelBuilder();
            BookmarkValidator = new BookmarkValidator();
            DependencyMapBuilder = new DependencyMapBuilder();
            LinkResolver = new LinkResolver(BuildQueue, GitCommitProvider, BookmarkValidator, DependencyMapBuilder, new Lazy<XrefMap>(xrefMap));
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
