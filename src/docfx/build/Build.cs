// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal static class Build
    {
        public static bool Run(string workingDirectory, CommandLineOptions options)
        {
            var stopwatch = Stopwatch.StartNew();
            using var errors = new ErrorWriter(options.Log);
            var docsets = ConfigLoader.FindDocsets(errors, workingDirectory, options);
            if (docsets.Length == 0)
            {
                errors.Add(Errors.Config.ConfigNotFound(workingDirectory));
                return errors.HasError;
            }

            Parallel.ForEach(docsets, docset =>
            {
                BuildDocset(errors, workingDirectory, docset.docsetPath, docset.outputPath, options);
            });

            Telemetry.TrackOperationTime("build", stopwatch.Elapsed);
            Log.Important($"Build done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
            errors.PrintSummary();
            return errors.HasError;
        }

        private static void BuildDocset(
            ErrorBuilder errors, string workingDirectory, string docsetPath, string? outputPath, CommandLineOptions options)
        {
            using var disposables = new DisposableCollector();
            errors = errors.WithDocsetPath(workingDirectory, docsetPath);

            try
            {
                var fetchOptions = options.NoRestore ? FetchOptions.NoFetch : (options.NoCache ? FetchOptions.Latest : FetchOptions.UseCache);
                var (config, buildOptions, packageResolver, fileResolver, opsAccessor) = ConfigLoader.Load(
                    errors, disposables, docsetPath, outputPath, options, fetchOptions);
                if (errors.HasError)
                {
                    return;
                }

                if (!options.NoRestore)
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
                var validationRules = GetContentValidationRules(config, fileResolver);

                errors = new ErrorLog(errors, config, sourceMap, validationRules);

                using var context = new Context(errors, config, buildOptions, packageResolver, fileResolver, sourceMap, repositoryProvider);
                Run(context);

                new OpsPostProcessor(config, errors, buildOptions, opsAccessor, context.JsonSchemaTransformer.GetValidateExternalXrefs()).Run();
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex);
            }
        }

        private static void Run(Context context)
        {
            using (Progress.Start("Building files"))
            {
                ParallelUtility.ForEach(
                    context.ErrorBuilder,
                    context.PublishUrlMap.GetAllFiles(),
                    file => BuildFile(context, file));

                ParallelUtility.ForEach(
                    context.ErrorBuilder,
                    context.LinkResolver.GetAdditionalResources(),
                    file => BuildResource.Build(context, file));
            }

            Parallel.Invoke(
                () => context.BookmarkValidator.Validate(),
                () => context.ContentValidator.PostValidate(),
                () => context.ErrorBuilder.AddRange(context.MetadataValidator.PostValidate()),
                () => context.ContributionProvider.Save(),
                () => context.RepositoryProvider.Save(),
                () => context.ErrorBuilder.AddRange(context.GitHubAccessor.Save()),
                () => context.ErrorBuilder.AddRange(context.MicrosoftGraphAccessor.Save()),
                () => context.JsonSchemaTransformer.PostValidate());

            // TODO: explicitly state that ToXrefMapModel produces errors
            var xrefMapModel = context.XrefResolver.ToXrefMapModel(context.BuildOptions.IsLocalizedBuild);
            var (publishModel, fileManifests) = context.PublishModelBuilder.Build();

            if (context.Config.DryRun)
            {
                return;
            }

            // TODO: decouple files and dependencies from legacy.
            var dependencyMap = context.DependencyMapBuilder.Build();

            MemoryCache.Clear();

            Parallel.Invoke(
                () => context.TemplateEngine.CopyAssetsToOutput(),
                () => context.Output.WriteJson(".xrefmap.json", xrefMapModel),
                () => context.Output.WriteJson(".publish.json", publishModel),
                () => context.Output.WriteJson(".dependencymap.json", dependencyMap.ToDependencyMapModel()),
                () => context.Output.WriteJson(".links.json", context.FileLinkMapBuilder.Build(context.PublishUrlMap.GetAllFiles())),
                () => context.Output.WriteText(".lunr.json", context.SearchIndexBuilder.Build()),
                () => Legacy.ConvertToLegacyModel(context.BuildOptions.DocsetPath, context, fileManifests, dependencyMap));

            using (Progress.Start("Waiting for pending outputs"))
            {
                context.Output.WaitForCompletion();
            }
        }

        private static void BuildFile(Context context, FilePath file)
        {
            var contentType = context.DocumentProvider.GetContentType(file);

            Telemetry.TrackBuildFileTypeCount(file, contentType, context.DocumentProvider.GetMime(file));
            context.ContentValidator.ValidateManifest(file);

            switch (contentType)
            {
                case ContentType.TableOfContents:
                    BuildTableOfContents.Build(context, file);
                    break;
                case ContentType.Resource:
                    BuildResource.Build(context, file);
                    break;
                case ContentType.Page:
                    BuildPage.Build(context, file);
                    break;
                case ContentType.Redirection:
                    BuildRedirection.Build(context, file);
                    break;
            }
        }

        private static Dictionary<string, ValidationRules>? GetContentValidationRules(Config? config, FileResolver fileResolver)
            => !string.IsNullOrEmpty(config?.MarkdownValidationRules.Value)
            ? JsonUtility.DeserializeData<Dictionary<string, ValidationRules>>(
                fileResolver.ReadString(config.MarkdownValidationRules),
                config.MarkdownValidationRules.Source?.File)
            : null;
    }
}
