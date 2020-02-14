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
        public readonly Config Config;
        public readonly FileResolver FileResolver;
        public readonly PackageResolver PackageResolver;
        public readonly ErrorLog ErrorLog;
        public readonly Output Output;
        public readonly Input Input;
        public readonly BuildScope BuildScope;
        public readonly RedirectionProvider RedirectionProvider;
        public readonly WorkQueue<FilePath> BuildQueue;
        public readonly DocumentProvider DocumentProvider;
        public readonly MetadataProvider MetadataProvider;
        public readonly MonikerProvider MonikerProvider;
        public readonly GitCommitProvider GitCommitProvider;
        public readonly BookmarkValidator BookmarkValidator;
        public readonly DependencyMapBuilder DependencyMapBuilder;
        public readonly LinkResolver LinkResolver;
        public readonly XrefResolver XrefResolver;
        public readonly GitHubAccessor GitHubAccessor;
        public readonly MicrosoftGraphAccessor MicrosoftGraphAccessor;
        public readonly ContributionProvider ContributionProvider;
        public readonly PublishModelBuilder PublishModelBuilder;
        public readonly MarkdownEngine MarkdownEngine;
        public readonly TemplateEngine TemplateEngine;
        public readonly FileLinkMapBuilder FileLinkMapBuilder;
        public readonly TableOfContentsLoader TableOfContentsLoader;
        public readonly LocalizationProvider LocalizationProvider;

        public TableOfContentsMap TocMap => _tocMap.Value;

        private readonly Lazy<TableOfContentsMap> _tocMap;

        public Context(string outputPath, ErrorLog errorLog, CommandLineOptions options, Config config, Docset docset, Docset fallbackDocset, Input input, RepositoryProvider repositoryProvider, LocalizationProvider localizationProvider, PackageResolver packageResolver)
        {
            var credentialProvider = config.GetCredentialProvider();

            DependencyMapBuilder = new DependencyMapBuilder();
            _tocMap = new Lazy<TableOfContentsMap>(() => TableOfContentsMap.Create(this));
            BuildQueue = new WorkQueue<FilePath>();

            Config = config;
            ErrorLog = errorLog;
            PackageResolver = packageResolver;
            FileResolver = new FileResolver(docset.DocsetPath, credentialProvider, new OpsConfigAdapter(errorLog, credentialProvider), options.FetchOptions);
            Input = input;
            LocalizationProvider = localizationProvider;
            Output = new Output(outputPath, input, Config.DryRun);
            TemplateEngine = new TemplateEngine(docset.DocsetPath, config, localizationProvider.Locale, PackageResolver);
            MicrosoftGraphAccessor = new MicrosoftGraphAccessor(Config);
            BuildScope = new BuildScope(Config, Input, fallbackDocset);
            DocumentProvider = new DocumentProvider(config, localizationProvider, docset, fallbackDocset, BuildScope, input, repositoryProvider, TemplateEngine);
            MetadataProvider = new MetadataProvider(Config, Input, MicrosoftGraphAccessor, FileResolver, DocumentProvider);
            MonikerProvider = new MonikerProvider(Config, BuildScope, MetadataProvider, FileResolver);
            RedirectionProvider = new RedirectionProvider(docset.DocsetPath, Config.HostName, ErrorLog, BuildScope, repositoryProvider, DocumentProvider, MonikerProvider);
            GitHubAccessor = new GitHubAccessor(Config);
            GitCommitProvider = new GitCommitProvider();
            PublishModelBuilder = new PublishModelBuilder(outputPath, Config, Output);
            BookmarkValidator = new BookmarkValidator(errorLog, PublishModelBuilder);
            ContributionProvider = new ContributionProvider(config, localizationProvider, Input, docset, fallbackDocset, GitHubAccessor, GitCommitProvider);
            FileLinkMapBuilder = new FileLinkMapBuilder(errorLog, MonikerProvider, PublishModelBuilder);
            XrefResolver = new XrefResolver(this, config, FileResolver, DependencyMapBuilder, FileLinkMapBuilder);

            LinkResolver = new LinkResolver(
                config,
                fallbackDocset,
                Input,
                BuildScope,
                BuildQueue,
                RedirectionProvider,
                DocumentProvider,
                GitCommitProvider,
                BookmarkValidator,
                DependencyMapBuilder,
                XrefResolver,
                TemplateEngine,
                FileLinkMapBuilder);

            MarkdownEngine = new MarkdownEngine(Config, FileResolver, LinkResolver, XrefResolver, MonikerProvider, TemplateEngine);

            TableOfContentsLoader = new TableOfContentsLoader(
                Input, LinkResolver, XrefResolver, MarkdownEngine, MonikerProvider, DependencyMapBuilder);
        }

        public void Dispose()
        {
            GitCommitProvider.Dispose();
            GitHubAccessor.Dispose();
            MicrosoftGraphAccessor.Dispose();
        }
    }
}
