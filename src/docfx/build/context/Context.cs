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
        private readonly Lazy<TableOfContentsMap> _tocMap;

        public Config Config { get; }

        public FileResolver FileResolver { get; }

        public PackageResolver PackageResolver { get; }

        public ErrorLog ErrorLog { get; }

        public Output Output { get; }

        public Input Input { get; }

        public SourceMap SourceMap { get; }

        public BuildScope BuildScope { get; }

        public RedirectionProvider RedirectionProvider { get; }

        public WorkQueue<FilePath> BuildQueue { get; }

        public DocumentProvider DocumentProvider { get; }

        public MetadataProvider MetadataProvider { get; }

        public MonikerProvider MonikerProvider { get; }

        public RepositoryProvider RepositoryProvider { get; }

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

        public ContentValidator ContentValidator { get; }

        public TableOfContentsMap TocMap => _tocMap.Value;

        public Context(string outputPath, ErrorLog errorLog, CommandLineOptions options, Config config, Docset docset, Docset? fallbackDocset, Repository? repository, LocalizationProvider localizationProvider, PackageResolver packageResolver)
        {
            var credentialProvider = config.GetCredentialProvider();

            DependencyMapBuilder = new DependencyMapBuilder();
            _tocMap = new Lazy<TableOfContentsMap>(() => TableOfContentsMap.Create(this));
            BuildQueue = new WorkQueue<FilePath>();

            Config = config;
            ErrorLog = errorLog;
            PackageResolver = packageResolver;
            RepositoryProvider = new RepositoryProvider(repository);
            FileResolver = new FileResolver(docset.DocsetPath, credentialProvider, new OpsConfigAdapter(errorLog, credentialProvider), options.FetchOptions, fallbackDocset);
            SourceMap = new SourceMap(new PathString(docset.DocsetPath), Config, FileResolver);
            Input = new Input(docset.DocsetPath, Config, SourceMap, PackageResolver, RepositoryProvider, localizationProvider);
            LocalizationProvider = localizationProvider;
            Output = new Output(outputPath, Input, Config.DryRun);
            TemplateEngine = new TemplateEngine(docset.DocsetPath, config, localizationProvider.Locale, PackageResolver);
            MicrosoftGraphAccessor = new MicrosoftGraphAccessor(Config);
            BuildScope = new BuildScope(Config, Input, fallbackDocset);
            DocumentProvider = new DocumentProvider(config, localizationProvider, docset, fallbackDocset, BuildScope, Input, TemplateEngine);
            MetadataProvider = new MetadataProvider(Config, Input, MicrosoftGraphAccessor, FileResolver, DocumentProvider);
            MonikerProvider = new MonikerProvider(Config, BuildScope, MetadataProvider, FileResolver);
            RedirectionProvider = new RedirectionProvider(docset.DocsetPath, Config.HostName, ErrorLog, BuildScope, repository, DocumentProvider, MonikerProvider);
            GitHubAccessor = new GitHubAccessor(Config);
            PublishModelBuilder = new PublishModelBuilder(outputPath, Config, Output, ErrorLog);
            BookmarkValidator = new BookmarkValidator(errorLog);
            ContentValidator = new ContentValidator(config, FileResolver, errorLog);
            ContributionProvider = new ContributionProvider(config, localizationProvider, Input, fallbackDocset, GitHubAccessor, RepositoryProvider);
            FileLinkMapBuilder = new FileLinkMapBuilder(errorLog, MonikerProvider, PublishModelBuilder);
            XrefResolver = new XrefResolver(this, config, FileResolver, repository, DependencyMapBuilder, FileLinkMapBuilder);

            LinkResolver = new LinkResolver(
                config,
                fallbackDocset,
                Input,
                SourceMap,
                BuildScope,
                BuildQueue,
                RedirectionProvider,
                DocumentProvider,
                BookmarkValidator,
                DependencyMapBuilder,
                XrefResolver,
                TemplateEngine,
                FileLinkMapBuilder);

            MarkdownEngine = new MarkdownEngine(Config, FileResolver, LinkResolver, XrefResolver, MonikerProvider, TemplateEngine);

            TableOfContentsLoader = new TableOfContentsLoader(Input, LinkResolver, XrefResolver, new TableOfContentsParser(MarkdownEngine), MonikerProvider, DependencyMapBuilder);
        }

        public void Dispose()
        {
            RepositoryProvider.Dispose();
            GitHubAccessor.Dispose();
            MicrosoftGraphAccessor.Dispose();
        }
    }
}
