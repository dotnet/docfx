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

        public SourceMap SourceMap { get; }

        public Context(ErrorLog errorLog, Config config, BuildOptions buildOptions, PackageResolver packageResolver, FileResolver fileResolver, SourceMap sourceMap)
        {
            DependencyMapBuilder = new DependencyMapBuilder(sourceMap);
            BuildQueue = new WorkQueue<FilePath>(errorLog);

            Config = config;
            ErrorLog = errorLog;
            BuildOptions = buildOptions;
            PackageResolver = packageResolver;
            FileResolver = fileResolver;
            SourceMap = sourceMap;

            RepositoryProvider = new RepositoryProvider(buildOptions.Repository);
            Input = new Input(buildOptions, config, packageResolver, RepositoryProvider);
            Output = new Output(buildOptions.OutputPath, Input, Config.DryRun);
            TemplateEngine = new TemplateEngine(config, buildOptions, PackageResolver);
            MicrosoftGraphAccessor = new MicrosoftGraphAccessor(Config);

            BuildScope = new BuildScope(ErrorLog, Config, Input, buildOptions);
            MetadataProvider = new MetadataProvider(Config, Input, MicrosoftGraphAccessor, FileResolver, BuildScope);
            MonikerProvider = new MonikerProvider(Config, BuildScope, MetadataProvider, FileResolver);
            DocumentProvider = new DocumentProvider(config, buildOptions, BuildScope, TemplateEngine, MonikerProvider);
            RedirectionProvider = new RedirectionProvider(buildOptions.DocsetPath, Config.HostName, ErrorLog, BuildScope, buildOptions.Repository, DocumentProvider, MonikerProvider);
            GitHubAccessor = new GitHubAccessor(Config);
            ContentValidator = new ContentValidator(config, FileResolver, errorLog);
            PublishModelBuilder = new PublishModelBuilder(buildOptions.OutputPath, Config, Output, ErrorLog, ContentValidator);
            BookmarkValidator = new BookmarkValidator(errorLog);
            ContributionProvider = new ContributionProvider(config, buildOptions, Input, GitHubAccessor, RepositoryProvider, sourceMap);
            FileLinkMapBuilder = new FileLinkMapBuilder(errorLog, MonikerProvider, PublishModelBuilder, ContributionProvider);
            XrefResolver = new XrefResolver(this, config, FileResolver, buildOptions.Repository, DependencyMapBuilder, FileLinkMapBuilder);

            LinkResolver = new LinkResolver(
                config,
                Input,
                BuildOptions,
                BuildScope,
                BuildQueue,
                RedirectionProvider,
                DocumentProvider,
                BookmarkValidator,
                DependencyMapBuilder,
                XrefResolver,
                TemplateEngine,
                FileLinkMapBuilder);

            MarkdownEngine = new MarkdownEngine(Config, Input, FileResolver, LinkResolver, XrefResolver, MonikerProvider, TemplateEngine, ContentValidator);

            var tocParser = new TableOfContentsParser(Input, MarkdownEngine);
            TableOfContentsLoader = new TableOfContentsLoader(LinkResolver, XrefResolver, tocParser, MonikerProvider, DependencyMapBuilder, config.ReduceTOCChildMonikers);
            TocMap = new TableOfContentsMap(ErrorLog, Input, BuildScope, DependencyMapBuilder, tocParser, TableOfContentsLoader, DocumentProvider);
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
