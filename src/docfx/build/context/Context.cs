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
        public readonly Report Report;
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

        public Context(
            Report report,
            Output output,
            Cache cache,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            GitCommitProvider gitCommitProvider,
            BookmarkValidator bookmarkValidator,
            DependencyMapBuilder dependencyMapBuilder,
            DependencyResolver dependencyResolver,
            DependencyResolver landingPageDependencyResolver,
            GitHubUserCache gitHubUserCache,
            ContributionProvider contributionProvider,
            PublishModelBuilder publishModelBuilder)
        {
            Report = report;
            Output = output;
            Cache = cache;
            MetadataProvider = metadataProvider;
            MonikerProvider = monikerProvider;
            GitCommitProvider = gitCommitProvider;
            BookmarkValidator = bookmarkValidator;
            DependencyMapBuilder = dependencyMapBuilder;
            LandingPageDependencyResolver = landingPageDependencyResolver;
            DependencyResolver = dependencyResolver;
            GitHubUserCache = gitHubUserCache;
            ContributionProvider = contributionProvider;
            PublishModelBuilder = publishModelBuilder;
        }

        public static async Task<Context> Create(string outputPath, Report report, Docset docset, Func<XrefMap> xrefMap)
        {
            var output = new Output(outputPath);
            var cache = new Cache();
            var metadataProvider = new MetadataProvider(docset.Config);
            var monikerProvider = await MonikerProvider.Create(docset);
            var gitHubUserCache = await GitHubUserCache.Create(docset);
            var gitCommitProvider = new GitCommitProvider();
            var bookmarkValidator = new BookmarkValidator();
            var dependencyMapBuilder = new DependencyMapBuilder();
            var dependencyResolver = new DependencyResolver(gitCommitProvider, bookmarkValidator, dependencyMapBuilder, new Lazy<XrefMap>(xrefMap));
            var landingPageDependencyResolver = new DependencyResolver(gitCommitProvider, bookmarkValidator, dependencyMapBuilder, new Lazy<XrefMap>(xrefMap), forLandingPage: true);
            var contributionProvider = await ContributionProvider.Create(docset, gitHubUserCache, gitCommitProvider);
            var publishModelBuilder = new PublishModelBuilder();

            return new Context(
                report,
                output,
                cache,
                metadataProvider,
                monikerProvider,
                gitCommitProvider,
                bookmarkValidator,
                dependencyMapBuilder,
                dependencyResolver,
                landingPageDependencyResolver,
                gitHubUserCache,
                contributionProvider,
                publishModelBuilder);
        }

        public void Dispose()
        {
            GitCommitProvider.Dispose();
            GitHubUserCache.Dispose();
        }
    }
}
