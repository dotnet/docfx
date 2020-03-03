// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// An immutable set of behavioral classes that are commonly used by the build pipeline.
    /// </summary>
    internal sealed class Context : IDisposable
    {
        private readonly Lazy<TableOfContentsMap> _tocMap;

        public Config Config { get; }

        public FileResolver FileResolver { get; }

        public PackageResolver PackageResolver { get; }

        public ErrorLog ErrorLog { get; }

        public Output Output { get; }

        public Input Input { get; }

        public BuildScope BuildScope { get; }

        public RedirectionProvider RedirectionProvider { get; }

        public WorkQueue<FilePath> BuildQueue { get; }

        public DocumentProvider DocumentProvider { get; }

        public MetadataProvider MetadataProvider { get; }

        public MonikerProvider MonikerProvider { get; }

        public GitCommitProvider GitCommitProvider { get; }

        public BookmarkValidator BookmarkValidator { get; }

        public DependencyMapBuilder DependencyMapBuilder { get; }

        public LinkResolver LinkResolver { get; }

        public XrefResolver XrefResolver { get; }

        public GitHubAccessor GitHubAccessor { get; }

        public MicrosoftGraphAccessor MicrosoftGraphAccessor { get; }

        public ContributionProvider ContributionProvider { get; }

        public PublishModelBuilder PublishModelBuilder { get; }

        public MarkdownEngine MarkdownEngine { get; }

        public TemplateEngine TemplateEngine { get; }

        public FileLinkMapBuilder FileLinkMapBuilder { get; }

        public TableOfContentsLoader TableOfContentsLoader { get; }

        public LocalizationProvider LocalizationProvider { get; }

        public TableOfContentsMap TocMap => _tocMap.Value;

        public Context(string outputPath, ErrorLog errorLog, CommandLineOptions options, Config config, Docset docset, Docset fallbackDocset, Input input, RepositoryProvider repositoryProvider, LocalizationProvider localizationProvider, PackageResolver packageResolver)
        {
            var credentialProvider = config.GetCredentialProvider();

            DependencyMapBuilder = new DependencyMapBuilder();
            _tocMap = new Lazy<TableOfContentsMap>(() => TableOfContentsMap.Create(this));
            BuildQueue = new WorkQueue<FilePath>();

            Config = config;
            ErrorLog = errorLog;
            PackageResolver = packageResolver;
            FileResolver = new FileResolver(docset.DocsetPath, credentialProvider, new OpsConfigAdapter(errorLog, credentialProvider), options.FetchOptions, fallbackDocset);
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
            ContributionProvider = new ContributionProvider(config, localizationProvider, Input, fallbackDocset, GitHubAccessor, GitCommitProvider);
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
