// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal static class Build
    {
        public static int Run(string workingDirectory, CommandLineOptions options)
        {
            var (errors, docsets) = ConfigLoader.FindDocsets(workingDirectory, options);
            ErrorWriter.PrintErrors(errors);
            if (docsets.Length == 0)
            {
                ErrorWriter.PrintError(Errors.Config.ConfigNotFound(workingDirectory));
                return 1;
            }

            var hasError = false;
            var restoreFetchOptions = options.NoCache ? FetchOptions.Latest : FetchOptions.UseCache;
            var buildFetchOptions = options.NoRestore ? FetchOptions.NoFetch : FetchOptions.UseCache;

            Parallel.ForEach(docsets, docset =>
            {
                using var errors = new ErrorWriter(docset.outputPath);

                if (!options.NoRestore && Restore.RestoreDocset(docset.docsetPath, docset.outputPath, options, restoreFetchOptions))
                {
                    hasError = true;
                    return;
                }

                if (BuildDocset(docset.docsetPath, docset.outputPath, options, buildFetchOptions))
                {
                    hasError = true;
                }
            });

            return hasError ? 1 : 0;
        }

        private static bool BuildDocset(string docsetPath, string? outputPath, CommandLineOptions options, FetchOptions fetchOptions)
        {
            var stopwatch = Stopwatch.StartNew();

            using var errorWriter = new ErrorWriter(outputPath);
            using var disposables = new DisposableCollector();

            ErrorBuilder errors = errorWriter;

            try
            {
                var (config, buildOptions, packageResolver, fileResolver) = ConfigLoader.Load(
                    errors, disposables, docsetPath, outputPath, options, fetchOptions);
                if (errors.HasError)
                {
                    return true;
                }

                new OpsPreProcessor(config, errors, buildOptions).Run();

                var sourceMap = new SourceMap(new PathString(buildOptions.DocsetPath), config, fileResolver);
                var validationRules = GetContentValidationRules(config, fileResolver);

                errors = new ErrorLog(errors, config, sourceMap, validationRules);

                using var context = new Context(errors, config, buildOptions, packageResolver, fileResolver, sourceMap);
                Run(context);

                new OpsPostProcessor(config, errors, buildOptions).Run();

                return errors.HasError;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex);
                return errors.HasError;
            }
            finally
            {
                Telemetry.TrackOperationTime("build", stopwatch.Elapsed);
                Log.Important($"Build done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                errorWriter.PrintSummary();
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
            }

            Parallel.Invoke(
                () => context.BookmarkValidator.Validate(),
                () => context.ContentValidator.PostValidate(),
                () => context.ErrorBuilder.AddRange(context.MetadataValidator.PostValidate()),
                () => context.ContributionProvider.Save(),
                () => context.RepositoryProvider.Save(),
                () => context.ErrorBuilder.AddRange(context.GitHubAccessor.Save()),
                () => context.ErrorBuilder.AddRange(context.MicrosoftGraphAccessor.Save()));

            // TODO: explicitly state that ToXrefMapModel produces errors
            var xrefMapModel = context.XrefResolver.ToXrefMapModel(context.BuildOptions.IsLocalizedBuild);
            var (publishModel, fileManifests) = context.PublishModelBuilder.Build();

            if (context.Config.DryRun)
            {
                return;
            }

            // TODO: decouple files and dependencies from legacy.
            var dependencyMap = context.DependencyMapBuilder.Build();

            Parallel.Invoke(
                () => context.Output.WriteJson(".xrefmap.json", xrefMapModel),
                () => context.Output.WriteJson(".publish.json", publishModel),
                () => context.Output.WriteJson(".dependencymap.json", dependencyMap.ToDependencyMapModel()),
                () => context.Output.WriteJson(".links.json", context.FileLinkMapBuilder.Build(context.PublishUrlMap.GetAllFiles())),
                () => Legacy.ConvertToLegacyModel(context.BuildOptions.DocsetPath, context, fileManifests, dependencyMap));

            using (Progress.Start("Waiting for pending outputs"))
            {
                context.Output.WaitForCompletion();
            }
        }

        private static void BuildFile(Context context, FilePath path)
        {
            var file = context.DocumentProvider.GetDocument(path);
            switch (file.ContentType)
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
