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
        public Config Config { get; }

        public BuildOptions BuildOptions { get; }

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

        public ContentValidator ContentValidator { get; }

        public TableOfContentsMap TocMap { get; }

        public Context(ErrorLog errorLog, Config config, BuildOptions buildOptions, PackageResolver packageResolver, FileResolver fileResolver)
        {
            DependencyMapBuilder = new DependencyMapBuilder();
            BuildQueue = new WorkQueue<FilePath>();

            Config = config;
            ErrorLog = errorLog;
            BuildOptions = buildOptions;
            PackageResolver = packageResolver;
            FileResolver = fileResolver;
            SourceMap = new SourceMap(new PathString(buildOptions.DocsetPath), Config, FileResolver);
            RepositoryProvider = new RepositoryProvider(buildOptions.Repository);
            Input = new Input(buildOptions, config, SourceMap, packageResolver, RepositoryProvider);
            Output = new Output(buildOptions.OutputPath, Input, Config.DryRun);
            TemplateEngine = new TemplateEngine(config, buildOptions, PackageResolver);
            MicrosoftGraphAccessor = new MicrosoftGraphAccessor(Config);
            BuildScope = new BuildScope(Config, Input, buildOptions);
            DocumentProvider = new DocumentProvider(config, buildOptions, BuildScope, Input, TemplateEngine);
            MetadataProvider = new MetadataProvider(Config, Input, MicrosoftGraphAccessor, FileResolver, DocumentProvider);
            MonikerProvider = new MonikerProvider(Config, BuildScope, MetadataProvider, FileResolver);
            RedirectionProvider = new RedirectionProvider(buildOptions.DocsetPath, Config.HostName, ErrorLog, BuildScope, buildOptions.Repository, DocumentProvider, MonikerProvider);
            GitHubAccessor = new GitHubAccessor(Config);
            PublishModelBuilder = new PublishModelBuilder(buildOptions.OutputPath, Config, Output, ErrorLog);
            BookmarkValidator = new BookmarkValidator(errorLog);
            ContentValidator = new ContentValidator(config, FileResolver, errorLog);
            ContributionProvider = new ContributionProvider(config, buildOptions, Input, GitHubAccessor, RepositoryProvider);
            FileLinkMapBuilder = new FileLinkMapBuilder(errorLog, MonikerProvider, PublishModelBuilder);
            XrefResolver = new XrefResolver(this, config, FileResolver, buildOptions.Repository, DependencyMapBuilder, FileLinkMapBuilder);

            LinkResolver = new LinkResolver(
                config,
                Input,
                BuildOptions,
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
            TocMap = TableOfContentsMap.Create(this);
        }

        public void Dispose()
        {
            ErrorLog.Dispose();
            PackageResolver.Dispose();
            RepositoryProvider.Dispose();
            GitHubAccessor.Dispose();
            MicrosoftGraphAccessor.Dispose();
        }
    }
}
