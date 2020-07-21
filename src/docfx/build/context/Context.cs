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

        public PublishUrlMap PublishUrlMap { get; }

        public PublishModelBuilder PublishModelBuilder { get; }

        public MarkdownEngine MarkdownEngine { get; }

        public TemplateEngine TemplateEngine { get; }

        public FileLinkMapBuilder FileLinkMapBuilder { get; }

        public TableOfContentsLoader TableOfContentsLoader { get; }

        public ContentValidator ContentValidator { get; }

        public TableOfContentsMap TocMap { get; }

        public SourceMap SourceMap { get; }

        public MetadataValidator MetadataValidator { get; }

        public JsonSchemaTransformer JsonSchemaTransformer { get; }

        public Context(
            ErrorLog errorLog, Config config, BuildOptions buildOptions, PackageResolver packageResolver, FileResolver fileResolver, SourceMap sourceMap)
        {
            DependencyMapBuilder = new DependencyMapBuilder(sourceMap);

            Config = config;
            ErrorLog = errorLog;
            BuildOptions = buildOptions;
            PackageResolver = packageResolver;
            FileResolver = fileResolver;
            SourceMap = sourceMap;

            RepositoryProvider = new RepositoryProvider(errorLog, buildOptions, config);
            Input = new Input(buildOptions, config, packageResolver, RepositoryProvider);
            Output = new Output(buildOptions.OutputPath, Input, Config.DryRun);
            MicrosoftGraphAccessor = new MicrosoftGraphAccessor(Config);
            TemplateEngine = new TemplateEngine(config, buildOptions, PackageResolver, new Lazy<JsonSchemaTransformer>(() => JsonSchemaTransformer));

            BuildScope = new BuildScope(Config, Input, buildOptions);
            MetadataProvider = new MetadataProvider(Config, Input, FileResolver, BuildScope);
            MonikerProvider = new MonikerProvider(Config, BuildScope, MetadataProvider, FileResolver);
            DocumentProvider = new DocumentProvider(config, buildOptions, BuildScope, TemplateEngine, MonikerProvider);
            RedirectionProvider = new RedirectionProvider(
                buildOptions.DocsetPath,
                Config.HostName,
                ErrorLog,
                BuildScope,
                buildOptions.Repository,
                DocumentProvider,
                MonikerProvider,
                new Lazy<PublishUrlMap>(() => PublishUrlMap));

            ContentValidator = new ContentValidator(
                config, FileResolver, errorLog, MonikerProvider, MetadataProvider, new Lazy<PublishUrlMap>(() => PublishUrlMap));
            GitHubAccessor = new GitHubAccessor(Config);
            BookmarkValidator = new BookmarkValidator(errorLog);
            ContributionProvider = new ContributionProvider(config, buildOptions, Input, GitHubAccessor, RepositoryProvider, sourceMap);
            FileLinkMapBuilder = new FileLinkMapBuilder(errorLog, MonikerProvider, ContributionProvider);
            XrefResolver = new XrefResolver(
                config,
                FileResolver,
                buildOptions.Repository,
                DependencyMapBuilder,
                FileLinkMapBuilder,
                ErrorLog,
                TemplateEngine,
                DocumentProvider,
                MetadataProvider,
                MonikerProvider,
                Input,
                BuildScope,
                new Lazy<JsonSchemaTransformer>(() => JsonSchemaTransformer));

            LinkResolver = new LinkResolver(
                config,
                BuildOptions,
                BuildScope,
                RedirectionProvider,
                DocumentProvider,
                BookmarkValidator,
                DependencyMapBuilder,
                XrefResolver,
                TemplateEngine,
                FileLinkMapBuilder,
                MetadataProvider);

            MarkdownEngine = new MarkdownEngine(
                Config,
                Input,
                FileResolver,
                LinkResolver,
                XrefResolver,
                DocumentProvider,
                MetadataProvider,
                MonikerProvider,
                TemplateEngine,
                ContentValidator,
                new Lazy<PublishUrlMap>(() => PublishUrlMap));

            JsonSchemaTransformer = new JsonSchemaTransformer(MarkdownEngine, LinkResolver, XrefResolver, errorLog, MonikerProvider);
            var tocParser = new TableOfContentsParser(Input, MarkdownEngine, DocumentProvider);
            TableOfContentsLoader = new TableOfContentsLoader(LinkResolver, XrefResolver, tocParser, MonikerProvider, DependencyMapBuilder, ContentValidator);
            TocMap = new TableOfContentsMap(
                ErrorLog, Input, BuildScope, DependencyMapBuilder, tocParser, TableOfContentsLoader, DocumentProvider, ContentValidator);
            PublishUrlMap = new PublishUrlMap(Config, ErrorLog, BuildScope, RedirectionProvider, DocumentProvider, MonikerProvider, TocMap);
            PublishModelBuilder = new PublishModelBuilder(
                config, errorLog, MonikerProvider, buildOptions, ContentValidator, PublishUrlMap, DocumentProvider, SourceMap);
            MetadataValidator = new MetadataValidator(
                Config, MicrosoftGraphAccessor, FileResolver, BuildScope, DocumentProvider, MonikerProvider, PublishUrlMap);
        }

        public void Dispose()
        {
            ErrorLog.Dispose();
            PackageResolver.Dispose();
            RepositoryProvider.Dispose();
            GitHubAccessor.Dispose();
            MicrosoftGraphAccessor.Dispose();
            TemplateEngine.Dispose();
        }
    }
}
