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
        private readonly string _workingDirectory;
        private readonly string _docsetPath;
        private readonly string? _outputPath;
        private readonly CommandLineOptions _options;

        public DocsetBuilder(string workingDirectory, string docsetPath, string? outputPath, CommandLineOptions options)
        {
            _workingDirectory = workingDirectory;
            _docsetPath = docsetPath;
            _outputPath = outputPath;
            _options = options;
        }

        [SuppressMessage("Layout", "MEN002:Line is too long", Justification = "Long constructor parameter list")]
        [SuppressMessage("Layout", "MEN003:Method is too long", Justification = "Long constructor list")]
        public void Build(ErrorBuilder errors)
        {
            errors = errors.WithDocsetPath(_workingDirectory, _docsetPath);

            try
            {
                var fetchOptions = _options.NoRestore ? FetchOptions.NoFetch : (_options.NoCache ? FetchOptions.Latest : FetchOptions.UseCache);
                var (config, buildOptions, packageResolver, fileResolver, opsAccessor) = ConfigLoader.Load(errors, _docsetPath, _outputPath, _options, fetchOptions);
                if (errors.HasError)
                {
                    return;
                }

                if (!_options.NoRestore)
                {
                    Restore.RestoreDocset(errors, config, buildOptions, packageResolver, fileResolver);
                    if (errors.HasError)
                    {
                        return;
                    }
                }

                var repositoryProvider = new RepositoryProvider(errors, buildOptions, config);
                new OpsPreProcessor(config, errors, buildOptions, repositoryProvider).Run();

                var sourceMap = new SourceMap(errors, new PathString(buildOptions.DocsetPath), config, fileResolver);
                errors = new ErrorLog(errors, config, sourceMap);

                PublishUrlMap? publishUrlMap = null;
                JsonSchemaTransformer? jsonSchemaTransformer = null;
                ContentValidator? contentValidator = null;

                var dependencyMapBuilder = new DependencyMapBuilder(sourceMap);
                var input = new Input(buildOptions, config, packageResolver, repositoryProvider, sourceMap);
                var output = new Output(buildOptions.OutputPath, input, config.DryRun);
                var microsoftGraphAccessor = new MicrosoftGraphAccessor(config);
                var jsonSchemaLoader = new JsonSchemaLoader(fileResolver);
                var templateEngine = new TemplateEngine(errors, config, output, packageResolver, new Lazy<JsonSchemaTransformer>(() => jsonSchemaTransformer!), buildOptions, jsonSchemaLoader);
                var buildScope = new BuildScope(config, input, buildOptions);
                var metadataProvider = new MetadataProvider(config, input, buildScope, jsonSchemaLoader);
                var monikerProvider = new MonikerProvider(config, buildScope, metadataProvider, fileResolver);
                var documentProvider = new DocumentProvider(input, errors, config, buildOptions, buildScope, templateEngine, monikerProvider, metadataProvider);
                var zonePivotProvider = new ZonePivotProvider(config, errors, documentProvider, metadataProvider, input, new Lazy<PublishUrlMap>(() => publishUrlMap!), new Lazy<ContentValidator>(() => contentValidator!));
                var redirectionProvider = new RedirectionProvider(buildOptions.DocsetPath, config.HostName, errors, buildScope, buildOptions.Repository, documentProvider, monikerProvider, new Lazy<PublishUrlMap>(() => publishUrlMap!));
                contentValidator = new ContentValidator(config, fileResolver, errors, documentProvider, monikerProvider, zonePivotProvider, new Lazy<PublishUrlMap>(() => publishUrlMap!));

                var gitHubAccessor = new GitHubAccessor(config);
                var bookmarkValidator = new BookmarkValidator(errors);
                var contributionProvider = new ContributionProvider(config, buildOptions, input, gitHubAccessor, repositoryProvider);
                var fileLinkMapBuilder = new FileLinkMapBuilder(errors, documentProvider, monikerProvider, contributionProvider);
                var xrefResolver = new XrefResolver(config, fileResolver, buildOptions.Repository, dependencyMapBuilder, fileLinkMapBuilder, errors, documentProvider, metadataProvider, monikerProvider, buildScope, new Lazy<JsonSchemaTransformer>(() => jsonSchemaTransformer!));
                var linkResolver = new LinkResolver(config, buildOptions, buildScope, redirectionProvider, documentProvider, bookmarkValidator, dependencyMapBuilder, xrefResolver, templateEngine, fileLinkMapBuilder, metadataProvider);
                var markdownEngine = new MarkdownEngine(config, input, fileResolver, linkResolver, xrefResolver, documentProvider, metadataProvider, monikerProvider, templateEngine, contentValidator, new Lazy<PublishUrlMap>(() => publishUrlMap!));
                jsonSchemaTransformer = new JsonSchemaTransformer(documentProvider, markdownEngine, linkResolver, xrefResolver, errors, monikerProvider, templateEngine, input);

                var tocParser = new TableOfContentsParser(input, markdownEngine, documentProvider);
                var tableOfContentsLoader = new TableOfContentsLoader(buildOptions.DocsetPath, input, linkResolver, xrefResolver, tocParser, monikerProvider, dependencyMapBuilder, contentValidator, config, errors, buildScope);
                var customRuleProvider = new CustomRuleProvider(config, fileResolver, documentProvider, new Lazy<PublishUrlMap>(() => publishUrlMap!), monikerProvider, errors);
                errors.CustomRuleProvider = customRuleProvider; // TODO use better way to inject

                var tocMap = new TableOfContentsMap(config, errors, input, buildScope, dependencyMapBuilder, tocParser, tableOfContentsLoader, documentProvider, contentValidator);
                publishUrlMap = new PublishUrlMap(config, errors, buildScope, redirectionProvider, documentProvider, monikerProvider, tocMap);
                var publishModelBuilder = new PublishModelBuilder(config, errors, monikerProvider, buildOptions, publishUrlMap, sourceMap, documentProvider, linkResolver);
                var metadataValidator = new MetadataValidator(config, microsoftGraphAccessor, jsonSchemaLoader, monikerProvider, customRuleProvider);
                var searchIndexBuilder = new SearchIndexBuilder(config, errors, documentProvider, metadataProvider);

                var resourceBuilder = new ResourceBuilder(input, documentProvider, config, output, publishModelBuilder);
                var pageBuilder = new PageBuilder(config, buildOptions, input, output, documentProvider, metadataProvider, monikerProvider, templateEngine, tocMap, linkResolver, contributionProvider, bookmarkValidator, publishModelBuilder, contentValidator, metadataValidator, markdownEngine, searchIndexBuilder, redirectionProvider, jsonSchemaTransformer);
                var tocBuilder = new TableOfContentsBuilder(config, tableOfContentsLoader, contentValidator, metadataProvider, metadataValidator, documentProvider, monikerProvider, publishModelBuilder, templateEngine, output);
                var redirectionBuilder = new RedirectionBuilder(publishModelBuilder, redirectionProvider, documentProvider);

                using (Progress.Start("Building files"))
                {
                    ParallelUtility.ForEach(errors, publishUrlMap.GetAllFiles(), file => BuildFile(file, errors, contentValidator, documentProvider, resourceBuilder, pageBuilder, tocBuilder, redirectionBuilder));
                    ParallelUtility.ForEach(errors, linkResolver.GetAdditionalResources(), file => resourceBuilder.Build(file));
                }

                Parallel.Invoke(
                    () => bookmarkValidator.Validate(),
                    () => contentValidator.PostValidate(),
                    () => errors.AddRange(metadataValidator.PostValidate()),
                    () => contributionProvider.Save(),
                    () => repositoryProvider.Save(),
                    () => errors.AddRange(gitHubAccessor.Save()),
                    () => errors.AddRange(microsoftGraphAccessor.Save()),
                    () => jsonSchemaTransformer.PostValidate());

                // TODO: explicitly state that ToXrefMapModel produces errors
                var xrefMapModel = xrefResolver.ToXrefMapModel(buildOptions.IsLocalizedBuild);
                var (publishModel, fileManifests) = publishModelBuilder.Build();

                if (config.DryRun)
                {
                    return;
                }

                // TODO: decouple files and dependencies from legacy.
                var dependencyMap = dependencyMapBuilder.Build();
                var legacyContext = new LegacyContext(config, buildOptions, output, sourceMap, monikerProvider, documentProvider);

                MemoryCache.Clear();

                Parallel.Invoke(
                    () => templateEngine.CopyAssetsToOutput(),
                    () => output.WriteJson(".xrefmap.json", xrefMapModel),
                    () => output.WriteJson(".publish.json", publishModel),
                    () => output.WriteJson(".dependencymap.json", dependencyMap.ToDependencyMapModel()),
                    () => output.WriteJson(".links.json", fileLinkMapBuilder.Build(publishUrlMap.GetAllFiles())),
                    () => output.WriteText(".lunr.json", searchIndexBuilder.Build()),
                    () => Legacy.ConvertToLegacyModel(buildOptions.DocsetPath, legacyContext, fileManifests, dependencyMap));

                using (Progress.Start("Waiting for pending outputs"))
                {
                    output.WaitForCompletion();
                }

                new OpsPostProcessor(config, errors, buildOptions, opsAccessor, jsonSchemaTransformer.GetValidateExternalXrefs()).Run();
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex);
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
