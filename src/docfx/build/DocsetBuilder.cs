// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    [SuppressMessage("Layout", "MEN003:Method is too long", Justification = "Long constructor list")]
    [SuppressMessage("Layout", "MEN002:Line is too long", Justification = "Long constructor parameter list")]
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
        private readonly MonikerProvider _monikerProvider;
        private readonly TemplateEngine _templateEngine;
        private readonly DocumentProvider _documentProvider;
        private readonly ContributionProvider _contributionProvider;
        private readonly RedirectionProvider _redirectionProvider;
        private readonly PublishUrlMap _publishUrlMap;
        private readonly CustomRuleProvider _customRuleProvider;

        public BuildOptions BuildOptions => _buildOptions;

        private DocsetBuilder(
            ErrorBuilder errors,
            Config config,
            BuildOptions buildOptions,
            PackageResolver packageResolver,
            FileResolver fileResolver,
            OpsAccessor opsAccessor,
            RepositoryProvider repositoryProvider,
            Package package)
        {
            _config = config;
            _buildOptions = buildOptions;
            _packageResolver = packageResolver;
            _fileResolver = fileResolver;
            _opsAccessor = opsAccessor;
            _repositoryProvider = repositoryProvider;
            _sourceMap = new SourceMap(errors, new PathString(_buildOptions.DocsetPath), _config, _fileResolver);
            _input = new Input(_buildOptions, _config, _packageResolver, _repositoryProvider, _sourceMap, package);
            _buildScope = new BuildScope(_config, _input, _buildOptions);
            _githubAccessor = new GitHubAccessor(_config);
            _microsoftGraphAccessor = new MicrosoftGraphAccessor(_config);
            _jsonSchemaLoader = new JsonSchemaLoader(_fileResolver);
            _metadataProvider = new MetadataProvider(_config, _input, _buildScope, _jsonSchemaLoader);
            _monikerProvider = new MonikerProvider(_config, _buildScope, _metadataProvider, _fileResolver);
            _errors = new ErrorLog(errors, _config, _sourceMap, _metadataProvider, () => Ensure(_customRuleProvider));
            _templateEngine = new TemplateEngine(_errors, _config, _packageResolver, _buildOptions, _jsonSchemaLoader);
            _documentProvider = new DocumentProvider(_input, _errors, _config, _buildOptions, _buildScope, _templateEngine, _monikerProvider, _metadataProvider);
            _contributionProvider = new ContributionProvider(_config, _buildOptions, _input, _githubAccessor, _repositoryProvider);
            _redirectionProvider = new RedirectionProvider(_config, _buildOptions, _errors, _buildScope, _documentProvider, _monikerProvider, () => Ensure(_publishUrlMap));
            _publishUrlMap = new PublishUrlMap(_config, _errors, _buildScope, _redirectionProvider, _documentProvider, _monikerProvider);
            _customRuleProvider = new CustomRuleProvider(_config, errors, _fileResolver, _documentProvider, _publishUrlMap, _monikerProvider);
        }

        public static DocsetBuilder? Create(
            ErrorBuilder errors,
            string workingDirectory,
            string docsetPath,
            string? outputPath,
            Package package,
            CommandLineOptions options)
        {
            errors = errors.WithDocsetPath(workingDirectory, docsetPath);

            try
            {
                var fetchOptions = options.NoRestore ? FetchOptions.NoFetch : (options.NoCache ? FetchOptions.Latest : FetchOptions.UseCache);
                var (config, buildOptions, packageResolver, fileResolver, opsAccessor) = ConfigLoader.Load(
                    errors, docsetPath, outputPath, options, fetchOptions, package);

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

                return new DocsetBuilder(errors, config, buildOptions, packageResolver, fileResolver, opsAccessor, repositoryProvider, package);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex);
                return null;
            }
        }

        public void Build(params string[] files)
        {
            try
            {
                JsonSchemaTransformer? jsonSchemaTransformer = null;
                ContentValidator? contentValidator = null;

                var dependencyMapBuilder = new DependencyMapBuilder(_sourceMap);
                var output = new Output(_buildOptions.OutputPath, _input, _config.DryRun);

                var zonePivotProvider = new ZonePivotProvider(_errors, _documentProvider, _metadataProvider, _input, _publishUrlMap, () => Ensure(contentValidator));
                contentValidator = new ContentValidator(_config, _fileResolver, _errors, _documentProvider, _monikerProvider, zonePivotProvider, _publishUrlMap);

                var bookmarkValidator = new BookmarkValidator(_errors);
                var fileLinkMapBuilder = new FileLinkMapBuilder(_errors, _documentProvider, _monikerProvider, _contributionProvider);
                var xrefResolver = new XrefResolver(_config, _fileResolver, _buildOptions.Repository, dependencyMapBuilder, fileLinkMapBuilder, _errors, _documentProvider, _metadataProvider, _monikerProvider, _buildScope, () => Ensure(jsonSchemaTransformer));
                var linkResolver = new LinkResolver(_config, _buildOptions, _buildScope, _redirectionProvider, _documentProvider, bookmarkValidator, dependencyMapBuilder, xrefResolver, _templateEngine, fileLinkMapBuilder, _metadataProvider);
                var markdownEngine = new MarkdownEngine(_config, _input, _fileResolver, linkResolver, xrefResolver, _documentProvider, _metadataProvider, _monikerProvider, _templateEngine, contentValidator, _publishUrlMap);
                jsonSchemaTransformer = new JsonSchemaTransformer(_documentProvider, markdownEngine, linkResolver, xrefResolver, _errors, _monikerProvider, _templateEngine, _input);

                var tocParser = new TocParser(_input, markdownEngine);
                var tocLoader = new TocLoader(_buildOptions.DocsetPath, _input, linkResolver, xrefResolver, tocParser, _monikerProvider, dependencyMapBuilder, contentValidator, _config, _errors, _buildScope);

                var tocMap = new TocMap(_config, _errors, _input, _buildScope, dependencyMapBuilder, tocParser, tocLoader, _documentProvider, contentValidator, _publishUrlMap);
                var publishModelBuilder = new PublishModelBuilder(_config, _errors, _monikerProvider, _buildOptions, _sourceMap, _documentProvider);
                var metadataValidator = new MetadataValidator(_config, _microsoftGraphAccessor, _jsonSchemaLoader, _monikerProvider, _customRuleProvider);
                var searchIndexBuilder = new SearchIndexBuilder(_config, _errors, _documentProvider, _metadataProvider);

                var resourceBuilder = new ResourceBuilder(_input, _documentProvider, _config, output, publishModelBuilder);
                var pageBuilder = new PageBuilder(_config, _buildOptions, _input, output, _documentProvider, _metadataProvider, _monikerProvider, _templateEngine, tocMap, linkResolver, _contributionProvider, bookmarkValidator, publishModelBuilder, contentValidator, metadataValidator, markdownEngine, searchIndexBuilder, _redirectionProvider, jsonSchemaTransformer);
                var tocBuilder = new TocBuilder(_config, tocLoader, contentValidator, _metadataProvider, metadataValidator, _documentProvider, _monikerProvider, publishModelBuilder, _templateEngine, output);
                var redirectionBuilder = new RedirectionBuilder(publishModelBuilder, _redirectionProvider, _documentProvider);

                var filesToBuild = files.Length > 0
                    ? files.Select(file => FilePath.Content(new PathString(file))).Where(file => _input.Exists(file) && _buildScope.Contains(file.Path)).ToHashSet()
                    : _publishUrlMap.GetFiles().Concat(tocMap.GetFiles()).ToHashSet();

                using (var scope = Progress.Start($"Building {filesToBuild.Count} files"))
                {
                    ParallelUtility.ForEach(scope, _errors, filesToBuild, file => BuildFile(file, contentValidator, resourceBuilder, pageBuilder, tocBuilder, redirectionBuilder));
                    ParallelUtility.ForEach(scope, _errors, linkResolver.GetAdditionalResources(), file => resourceBuilder.Build(file));
                }

                Parallel.Invoke(
                    () => bookmarkValidator.Validate(),
                    () => contentValidator.PostValidate(),
                    () => _errors.AddRange(metadataValidator.PostValidate()),
                    () => _contributionProvider.Save(),
                    () => _repositoryProvider.Save(),
                    () => _errors.AddRange(_githubAccessor.Save()),
                    () => _errors.AddRange(_microsoftGraphAccessor.Save()),
                    () => jsonSchemaTransformer.PostValidate());

                if (_config.DryRun)
                {
                    return;
                }

                // TODO: explicitly state that ToXrefMapModel produces errors
                var xrefMapModel = xrefResolver.ToXrefMapModel(_buildOptions.IsLocalizedBuild);
                var (publishModel, fileManifests) = publishModelBuilder.Build(filesToBuild);

                // TODO: decouple files and dependencies from legacy.
                var dependencyMap = dependencyMapBuilder.Build();
                var legacyContext = new LegacyContext(_config, _buildOptions, output, _sourceMap, _monikerProvider, _documentProvider);

                MemoryCache.Clear();

                Parallel.Invoke(
                    () => _templateEngine.CopyAssetsToOutput(output),
                    () => output.WriteJson(".xrefmap.json", xrefMapModel),
                    () => output.WriteJson(".publish.json", publishModel),
                    () => output.WriteJson(".dependencymap.json", dependencyMap.ToDependencyMapModel()),
                    () => output.WriteJson(".links.json", fileLinkMapBuilder.Build(publishModel)),
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

        private void BuildFile(
            FilePath file,
            ContentValidator contentValidator,
            ResourceBuilder resourceBuilder,
            PageBuilder pageBuilder,
            TocBuilder tocBuilder,
            RedirectionBuilder redirectionBuilder)
        {
            var contentType = _documentProvider.GetContentType(file);

            Telemetry.TrackBuildFileTypeCount(file, contentType, _documentProvider.GetMime(file));
            contentValidator.ValidateManifest(file);

            switch (contentType)
            {
                case ContentType.Toc:
                    tocBuilder.Build(_errors, file);
                    break;
                case ContentType.Resource:
                    resourceBuilder.Build(file);
                    break;
                case ContentType.Page:
                    pageBuilder.Build(_errors, file);
                    break;
                case ContentType.Redirection:
                    redirectionBuilder.Build(_errors, file);
                    break;
            }
        }

        private static T Ensure<T>(T? nullable) where T : class => nullable ?? throw new InvalidOperationException($"Cannot access {typeof(T).Name} in constructor.");
    }
}
