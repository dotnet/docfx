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
        private readonly ErrorLog _errors;
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
        private readonly BookmarkValidator _bookmarkValidator;
        private readonly IProgress<string> _progressReporter;
        private readonly FileLinkMapBuilder _fileLinkMapBuilder;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly SearchIndexBuilder _searchIndexBuilder;
        private readonly ZonePivotProvider _zonePivotProvider;
        private readonly ContentValidator _contentValidator;
        private readonly XrefResolver _xrefResolver;
        private readonly LinkResolver _linkResolver;
        private readonly MarkdownEngine _markdownEngine;
        private readonly JsonSchemaTransformer _jsonSchemaTransformer;
        private readonly MetadataValidator _metadataValidator;
        private readonly TocParser _tocParser;
        private readonly TocLoader _tocLoader;
        private readonly TocMap _tocMap;

        public BuildOptions BuildOptions => _buildOptions;

        private DocsetBuilder(
            ErrorLog errors,
            Config config,
            BuildOptions buildOptions,
            PackageResolver packageResolver,
            FileResolver fileResolver,
            OpsAccessor opsAccessor,
            RepositoryProvider repositoryProvider,
            Package package,
            IProgress<string> progressReporter)
        {
            _errors = errors;
            _config = config;
            _buildOptions = buildOptions;
            _packageResolver = packageResolver;
            _fileResolver = fileResolver;
            _opsAccessor = opsAccessor;
            _repositoryProvider = repositoryProvider;
            _progressReporter = progressReporter;
            _sourceMap = _errors.SourceMap = new(_errors, new(_buildOptions.DocsetPath), _config, _fileResolver);
            _input = new(_buildOptions, _config, _packageResolver, _repositoryProvider, _sourceMap, package);
            _buildScope = new(_config, _input, _buildOptions);
            _githubAccessor = new(_config);
            _microsoftGraphAccessor = new(_config);
            _jsonSchemaLoader = new(_fileResolver);
            _metadataProvider = _errors.MetadataProvider = new(_config, _input, _buildScope, _jsonSchemaLoader);
            _monikerProvider = new(_config, _buildScope, _metadataProvider, _fileResolver);
            _templateEngine = new(_errors, _config, _packageResolver, _buildOptions, _jsonSchemaLoader);
            _documentProvider = new(_input, _errors, _config, _buildOptions, _buildScope, _templateEngine, _monikerProvider, _metadataProvider);
            _contributionProvider = new(_config, _buildOptions, _input, _githubAccessor, _repositoryProvider);
            _redirectionProvider = new(_config, _buildOptions, _errors, _buildScope, package, _documentProvider, _monikerProvider, () => Ensure(_publishUrlMap));
            _publishUrlMap = new(_config, _errors, _buildScope, _redirectionProvider, _documentProvider, _monikerProvider);
            _customRuleProvider = _errors.CustomRuleProvider = new(_config, _errors, _fileResolver, _documentProvider, _publishUrlMap, _monikerProvider);
            _bookmarkValidator = new(_errors);
            _fileLinkMapBuilder = new(_errors, _documentProvider, _monikerProvider, _contributionProvider);
            _dependencyMapBuilder = new(_sourceMap);
            _searchIndexBuilder = new(_config, _errors, _documentProvider, _metadataProvider);
            _zonePivotProvider = new(_errors, _documentProvider, _metadataProvider, _input, _publishUrlMap, () => Ensure(_contentValidator));
            _contentValidator = new(_config, _fileResolver, _errors, _documentProvider, _monikerProvider, _zonePivotProvider, _publishUrlMap);
            _xrefResolver = new(_config, _fileResolver, _buildOptions.Repository, _dependencyMapBuilder, _fileLinkMapBuilder, _errors, _documentProvider, _metadataProvider, _monikerProvider, _buildScope, () => Ensure(_jsonSchemaTransformer));
            _linkResolver = new(_config, _buildOptions, _buildScope, _redirectionProvider, _documentProvider, _bookmarkValidator, _dependencyMapBuilder, _xrefResolver, _templateEngine, _fileLinkMapBuilder, _metadataProvider);
            _markdownEngine = new(_config, _input, _fileResolver, _linkResolver, _xrefResolver, _documentProvider, _metadataProvider, _monikerProvider, _templateEngine, _contentValidator, _publishUrlMap);
            _jsonSchemaTransformer = new(_documentProvider, _markdownEngine, _linkResolver, _xrefResolver, _errors, _monikerProvider, _templateEngine, _input);
            _metadataValidator = new MetadataValidator(_config, _microsoftGraphAccessor, _jsonSchemaLoader, _monikerProvider, _customRuleProvider);
            _tocParser = new(_input, _markdownEngine);
            _tocLoader = new(_buildOptions, _input, _linkResolver, _xrefResolver, _tocParser, _monikerProvider, _dependencyMapBuilder, _contentValidator, _config, _errors, _buildScope);
            _tocMap = new(_config, _errors, _input, _buildScope, _dependencyMapBuilder, _tocParser, _tocLoader, _documentProvider, _contentValidator, _publishUrlMap);
        }

        public static DocsetBuilder? Create(
            ErrorBuilder errors,
            Repository? repository,
            string docsetPath,
            string? outputPath,
            Package package,
            CommandLineOptions options,
            IProgress<string> progressReporter,
            CredentialProvider? getCredential = null)
        {
            var errorLog = new ErrorLog(errors, options.WorkingDirectory, docsetPath);

            try
            {
                progressReporter.Report("Loading config...");
                var fetchOptions = options.NoRestore ? FetchOptions.NoFetch : (options.NoCache ? FetchOptions.Latest : FetchOptions.UseCache);
                var (config, buildOptions, packageResolver, fileResolver, opsAccessor) = ConfigLoader.Load(
                   errorLog, repository, docsetPath, outputPath, options, fetchOptions, package, getCredential);

                if (errorLog.HasError)
                {
                    return null;
                }

                errorLog.Config = config;

                if (!options.NoRestore)
                {
                    progressReporter.Report("Restoring dependencies...");
                    Restore.RestoreDocset(errorLog, config, buildOptions, packageResolver, fileResolver);
                    if (errorLog.HasError)
                    {
                        return null;
                    }
                }

                var repositoryProvider = new RepositoryProvider(errorLog, buildOptions, config);

                new OpsPreProcessor(config, errorLog, buildOptions, repositoryProvider).Run();

                return new DocsetBuilder(errorLog, config, buildOptions, packageResolver, fileResolver, opsAccessor, repositoryProvider, package, progressReporter);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errorLog.AddRange(dex);
                return null;
            }
        }

        public void Build(string[]? files)
        {
            try
            {
                _progressReporter.Report("Building...");

                var output = new Output(_buildOptions.OutputPath, _input, _config.DryRun);
                var publishModelBuilder = new PublishModelBuilder(_config, _errors, _monikerProvider, _buildOptions, _sourceMap, _documentProvider);
                var resourceBuilder = new ResourceBuilder(_input, _documentProvider, _config, output, publishModelBuilder);
                var learnHierarchyBuilder = new LearnHierarchyBuilder(_contentValidator);
                var pageBuilder = new PageBuilder(_config, _buildOptions, _input, output, _documentProvider, _metadataProvider, _monikerProvider, _templateEngine, _tocMap, _linkResolver, _contributionProvider, _bookmarkValidator, publishModelBuilder, _contentValidator, _metadataValidator, _markdownEngine, _searchIndexBuilder, _redirectionProvider, _jsonSchemaTransformer, learnHierarchyBuilder);
                var tocBuilder = new TocBuilder(_config, _tocLoader, _contentValidator, _metadataProvider, _metadataValidator, _documentProvider, _monikerProvider, publishModelBuilder, _templateEngine, output);
                var redirectionBuilder = new RedirectionBuilder(publishModelBuilder, _redirectionProvider, _documentProvider);

                var filesToBuild = files != null
                    ? files.Select(file => FilePath.Content(new PathString(file))).Where(file => _input.Exists(file) && _buildScope.Contains(file.Path)).ToHashSet()
                    : _publishUrlMap.GetFiles().Concat(_tocMap.GetFiles()).ToHashSet();

                using (var scope = Progress.Start($"Building {filesToBuild.Count} files"))
                {
                    ParallelUtility.ForEach(scope, _errors, filesToBuild, file => BuildFile(file, _contentValidator, resourceBuilder, pageBuilder, tocBuilder, redirectionBuilder));
                    ParallelUtility.ForEach(scope, _errors, _linkResolver.GetAdditionalResources(), file => resourceBuilder.Build(file));
                }

                Parallel.Invoke(
                    () => _bookmarkValidator.Validate(),
                    () => _contentValidator.PostValidate(),
                    () => _errors.AddRange(_metadataValidator.PostValidate()),
                    () => _contributionProvider.Save(),
                    () => _repositoryProvider.Save(),
                    () => _errors.AddRange(_githubAccessor.Save()),
                    () => _errors.AddRange(_microsoftGraphAccessor.Save()),
                    () => _jsonSchemaTransformer.PostValidate(files != null),
                    () => learnHierarchyBuilder.ValidateHierarchy());

                if (_config.DryRun)
                {
                    return;
                }

                // TODO: explicitly state that ToXrefMapModel produces errors
                var xrefMapModel = _xrefResolver.ToXrefMapModel(_buildOptions.IsLocalizedBuild);
                var (publishModel, fileManifests) = publishModelBuilder.Build(filesToBuild);

                // TODO: decouple files and dependencies from legacy.
                var dependencyMap = _dependencyMapBuilder.Build();
                var legacyContext = new LegacyContext(_config, _buildOptions, output, _sourceMap, _monikerProvider, _documentProvider);

                MemoryCache.Clear();

                Parallel.Invoke(
                    () => _templateEngine.CopyAssetsToOutput(output),
                    () => output.WriteJson(".xrefmap.json", xrefMapModel),
                    () => output.WriteJson(".publish.json", publishModel),
                    () => output.WriteJson(".dependencymap.json", dependencyMap.ToDependencyMapModel()),
                    () => output.WriteJson(".links.json", _fileLinkMapBuilder.Build(publishModel)),
                    () => output.WriteText(".lunr.json", _searchIndexBuilder.Build()),
                    () => Legacy.ConvertToLegacyModel(_buildOptions.DocsetPath, legacyContext, fileManifests, dependencyMap));

                using (Progress.Start("Waiting for pending outputs"))
                {
                    output.WaitForCompletion();
                }

                new OpsPostProcessor(_config, _errors, _buildOptions, _opsAccessor, _jsonSchemaTransformer.GetValidateExternalXrefs()).Run();
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
