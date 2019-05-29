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
        public readonly MetadataProvider MetadataProvider;
        public readonly MonikerProvider MonikerProvider;
        public readonly GitCommitProvider GitCommitProvider;
        public readonly BookmarkValidator BookmarkValidator;
        public readonly DependencyMapBuilder DependencyMapBuilder;
        public readonly DependencyResolver DependencyResolver;
        public readonly DependencyResolver LandingPageDependencyResolver;
        public readonly GitHubUserCache GitHubUserCache;
        public readonly ContributionProvider ContributionProvider;
        public readonly PublishModelBuilder PublishModelBuilder;
        public readonly TemplateEngine Template;

        public Context(string outputPath, ErrorLog errorLog, Docset docset, Func<XrefMap> xrefMap)
        {
            ErrorLog = errorLog;
            Output = new Output(outputPath);
            Cache = new Cache();
            MetadataProvider = new MetadataProvider(docset);
            MonikerProvider = new MonikerProvider(docset);
            GitHubUserCache = GitHubUserCache.Create(docset);
            GitCommitProvider = new GitCommitProvider();
            BookmarkValidator = new BookmarkValidator();
            DependencyMapBuilder = new DependencyMapBuilder();
            DependencyResolver = new DependencyResolver(GitCommitProvider, BookmarkValidator, DependencyMapBuilder, new Lazy<XrefMap>(xrefMap));
            LandingPageDependencyResolver = new DependencyResolver(GitCommitProvider, BookmarkValidator, DependencyMapBuilder, new Lazy<XrefMap>(xrefMap), forLandingPage: true);
            ContributionProvider = new ContributionProvider(docset, GitHubUserCache, GitCommitProvider);
            PublishModelBuilder = new PublishModelBuilder();
            Template = TemplateEngine.Create(docset);
        }

        public void Dispose()
        {
            GitCommitProvider.Dispose();
            GitHubUserCache.Dispose();
        }
    }
}
