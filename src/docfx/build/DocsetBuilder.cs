// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class DocsetBuilder
    {
        private readonly ErrorBuilder _errors;
        private readonly Config _config;
        private readonly BuildOptions _buildOptions;
        private readonly PackageResolver _packageResolver;
        private readonly FileResolver _fileResolver;
        private readonly GitHubAccessor _githubAccessor;
        private readonly OpsAccessor _opsAccessor;
        private readonly MicrosoftGraphAccessor _microsoftGraphAccessor;
        private readonly RepositoryProvider _repositoryProvider;
        private readonly SourceMap _sourceMap;
        private readonly Input _input;
        private readonly BuildScope _buildScope;
        private readonly JsonSchemaLoader _jsonSchemaLoader;
        private readonly MetadataProvider _metadataProvider;

        private DocsetBuilder(
            ErrorBuilder errors,
            Config config,
            BuildOptions buildOptions,
            PackageResolver packageResolver,
            FileResolver fileResolver,
            OpsAccessor opsAccessor,
            RepositoryProvider repositoryProvider)
        {
            _config = config;
            _buildOptions = buildOptions;
            _packageResolver = packageResolver;
            _fileResolver = fileResolver;
            _opsAccessor = opsAccessor;
            _repositoryProvider = repositoryProvider;
            _sourceMap = new SourceMap(errors, new PathString(_buildOptions.DocsetPath), _config, _fileResolver);
            _errors = new ErrorLog(errors, _config, _sourceMap);
            _input = new Input(_buildOptions, _config, _packageResolver, _repositoryProvider, _sourceMap);
            _buildScope = new BuildScope(_config, _input, _buildOptions);
            _githubAccessor = new GitHubAccessor(_config);
            _microsoftGraphAccessor = new MicrosoftGraphAccessor(_config);
            _jsonSchemaLoader = new JsonSchemaLoader(_fileResolver);
            _metadataProvider = new MetadataProvider(_config, _input, _buildScope, _jsonSchemaLoader);
        }

        public static DocsetBuilder? Create(ErrorBuilder errors, string workingDirectory, string docsetPath, string? outputPath, CommandLineOptions options)
        {
            errors = errors.WithDocsetPath(workingDirectory, docsetPath);

            try
            {
                var fetchOptions = options.NoRestore ? FetchOptions.NoFetch : (options.NoCache ? FetchOptions.Latest : FetchOptions.UseCache);
                var (config, buildOptions, packageResolver, fileResolver, opsAccessor) = ConfigLoader.Load(
                    errors, docsetPath, outputPath, options, fetchOptions);

                if (errors.HasError)
                {
                    return null;
                }

                if (!options.NoRestore)
                {
                    Restore.RestoreDocset(errors, config, buildOptions, packageResolver, fileResolver);
                    if (errors.HasError)
                    {
                        return null;
                    }
                }

                var repositoryProvider = new RepositoryProvider(errors, buildOptions, config);

                new OpsPreProcessor(config, errors, buildOptions, repositoryProvider).Run();

                return new DocsetBuilder(errors, config, buildOptions, packageResolver, fileResolver, opsAccessor, repositoryProvider);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex);
                return null;
            }
        }

        [SuppressMessage("Layout", "MEN002:Line is too long", Justification = "Long constructor parameter list")]
        [SuppressMessage("Layout", "MEN003:Method is too long", Justification = "Long constructor list")]
        public void Build()
        {
            try
            {
                PublishUrlMap? publishUrlMap = null;
                JsonSchemaTransformer? jsonSchemaTransformer = null;
                ContentValidator? contentValidator = null;

                var dependencyMapBuilder = new DependencyMapBuilder(_sourceMap);
                var output = new Output(_buildOptions.OutputPath, _input, _config.DryRun);
                var templateEngine = new TemplateEngine(_errors, _config, output, _packageResolver, new Lazy<JsonSchemaTransformer>(() => jsonSchemaTransformer!), _buildOptions, _jsonSchemaLoader);

                var monikerProvider = new MonikerProvider(_config, _buildScope, _metadataProvider, _fileResolver);
                var documentProvider = new DocumentProvider(_input, _errors, _config, _buildOptions, _buildScope, templateEngine, monikerProvider, _metadataProvider);
                var zonePivotProvider = new ZonePivotProvider(_config, _errors, documentProvider, _metadataProvider, _input, new Lazy<PublishUrlMap>(() => publishUrlMap!), new Lazy<ContentValidator>(() => contentValidator!));
                var redirectionProvider = new RedirectionProvider(_buildOptions.DocsetPath, _config.HostName, _errors, _buildScope, _buildOptions.Repository, documentProvider, monikerProvider, new Lazy<PublishUrlMap>(() => publishUrlMap!));
                contentValidator = new ContentValidator(_config, _fileResolver, _errors, documentProvider, monikerProvider, zonePivotProvider, new Lazy<PublishUrlMap>(() => publishUrlMap!));

                var bookmarkValidator = new BookmarkValidator(_errors);
                var contributionProvider = new ContributionProvider(_config, _buildOptions, _input, _githubAccessor, _repositoryProvider);
                var fileLinkMapBuilder = new FileLinkMapBuilder(_errors, documentProvider, monikerProvider, contributionProvider);
                var xrefResolver = new XrefResolver(_config, _fileResolver, _buildOptions.Repository, dependencyMapBuilder, fileLinkMapBuilder, _errors, documentProvider, _metadataProvider, monikerProvider, _buildScope, new Lazy<JsonSchemaTransformer>(() => jsonSchemaTransformer!));
                var linkResolver = new LinkResolver(_config, _buildOptions, _buildScope, redirectionProvider, documentProvider, bookmarkValidator, dependencyMapBuilder, xrefResolver, templateEngine, fileLinkMapBuilder, _metadataProvider);
                var markdownEngine = new MarkdownEngine(_config, _input, _fileResolver, linkResolver, xrefResolver, documentProvider, _metadataProvider, monikerProvider, templateEngine, contentValidator, new Lazy<PublishUrlMap>(() => publishUrlMap!));
                jsonSchemaTransformer = new JsonSchemaTransformer(documentProvider, markdownEngine, linkResolver, xrefResolver, _errors, monikerProvider, templateEngine, _input);

                var tocParser = new TableOfContentsParser(_input, markdownEngine, documentProvider);
                var tableOfContentsLoader = new TableOfContentsLoader(_buildOptions.DocsetPath, _input, linkResolver, xrefResolver, tocParser, monikerProvider, dependencyMapBuilder, contentValidator, _config, _errors, _buildScope);
                var customRuleProvider = new CustomRuleProvider(_config, _fileResolver, documentProvider, new Lazy<PublishUrlMap>(() => publishUrlMap!), monikerProvider, _errors);
                _errors.CustomRuleProvider = customRuleProvider; // TODO use better way to inject

                var tocMap = new TableOfContentsMap(_config, _errors, _input, _buildScope, dependencyMapBuilder, tocParser, tableOfContentsLoader, documentProvider, contentValidator);
                publishUrlMap = new PublishUrlMap(_config, _errors, _buildScope, redirectionProvider, documentProvider, monikerProvider, tocMap);
                var publishModelBuilder = new PublishModelBuilder(_config, _errors, monikerProvider, _buildOptions, publishUrlMap, _sourceMap, documentProvider, linkResolver);
                var metadataValidator = new MetadataValidator(_config, _microsoftGraphAccessor, _jsonSchemaLoader, monikerProvider, customRuleProvider);
                var searchIndexBuilder = new SearchIndexBuilder(_config, _errors, documentProvider, _metadataProvider);

                var resourceBuilder = new ResourceBuilder(_input, documentProvider, _config, output, publishModelBuilder);
                var pageBuilder = new PageBuilder(_config, _buildOptions, _input, output, documentProvider, _metadataProvider, monikerProvider, templateEngine, tocMap, linkResolver, contributionProvider, bookmarkValidator, publishModelBuilder, contentValidator, metadataValidator, markdownEngine, searchIndexBuilder, redirectionProvider, jsonSchemaTransformer);
                var tocBuilder = new TableOfContentsBuilder(_config, tableOfContentsLoader, contentValidator, _metadataProvider, metadataValidator, documentProvider, monikerProvider, publishModelBuilder, templateEngine, output);
                var redirectionBuilder = new RedirectionBuilder(publishModelBuilder, redirectionProvider, documentProvider);

                using (Progress.Start("Building files"))
                {
                    ParallelUtility.ForEach(_errors, publishUrlMap.GetAllFiles(), file => BuildFile(file, _errors, contentValidator, documentProvider, resourceBuilder, pageBuilder, tocBuilder, redirectionBuilder));
                    ParallelUtility.ForEach(_errors, linkResolver.GetAdditionalResources(), file => resourceBuilder.Build(file));
                }

                Parallel.Invoke(
                    () => bookmarkValidator.Validate(),
                    () => contentValidator.PostValidate(),
                    () => _errors.AddRange(metadataValidator.PostValidate()),
                    () => contributionProvider.Save(),
                    () => _repositoryProvider.Save(),
                    () => _errors.AddRange(_githubAccessor.Save()),
                    () => _errors.AddRange(_microsoftGraphAccessor.Save()),
                    () => jsonSchemaTransformer.PostValidate());

                // TODO: explicitly state that ToXrefMapModel produces errors
                var xrefMapModel = xrefResolver.ToXrefMapModel(_buildOptions.IsLocalizedBuild);
                var (publishModel, fileManifests) = publishModelBuilder.Build();

                if (_config.DryRun)
                {
                    return;
                }

                // TODO: decouple files and dependencies from legacy.
                var dependencyMap = dependencyMapBuilder.Build();
                var legacyContext = new LegacyContext(_config, _buildOptions, output, _sourceMap, monikerProvider, documentProvider);

                MemoryCache.Clear();

                Parallel.Invoke(
                    () => templateEngine.CopyAssetsToOutput(),
                    () => output.WriteJson(".xrefmap.json", xrefMapModel),
                    () => output.WriteJson(".publish.json", publishModel),
                    () => output.WriteJson(".dependencymap.json", dependencyMap.ToDependencyMapModel()),
                    () => output.WriteJson(".links.json", fileLinkMapBuilder.Build(publishUrlMap.GetAllFiles())),
                    () => output.WriteText(".lunr.json", searchIndexBuilder.Build()),
                    () => Legacy.ConvertToLegacyModel(_buildOptions.DocsetPath, legacyContext, fileManifests, dependencyMap));

                using (Progress.Start("Waiting for pending outputs"))
                {
                    output.WaitForCompletion();
                }

                new OpsPostProcessor(_config, _errors, _buildOptions, _opsAccessor, jsonSchemaTransformer.GetValidateExternalXrefs()).Run();
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                _errors.AddRange(dex);
            }
        }

        private static void BuildFile(
            FilePath file,
            ErrorBuilder errors,
            ContentValidator contentValidator,
            DocumentProvider documentProvider,
            ResourceBuilder resourceBuilder,
            PageBuilder pageBuilder,
            TableOfContentsBuilder tocBuilder,
            RedirectionBuilder redirectionBuilder)
        {
            var contentType = documentProvider.GetContentType(file);

            Telemetry.TrackBuildFileTypeCount(file, contentType, documentProvider.GetMime(file));
            contentValidator.ValidateManifest(file);

            switch (contentType)
            {
                case ContentType.TableOfContents:
                    tocBuilder.Build(errors, file);
                    break;
                case ContentType.Resource:
                    resourceBuilder.Build(file);
                    break;
                case ContentType.Page:
                    pageBuilder.Build(errors, file);
                    break;
                case ContentType.Redirection:
                    redirectionBuilder.Build(errors, file);
                    break;
            }
        }
    }
}
