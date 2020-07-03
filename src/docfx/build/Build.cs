// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Build
    {
        public static int Run(string workingDirectory, CommandLineOptions options)
        {
            var (errors, docsets) = ConfigLoader.FindDocsets(workingDirectory, options);
            ErrorLog.PrintErrors(errors);
            if (docsets.Length == 0)
            {
                ErrorLog.PrintError(Errors.Config.ConfigNotFound(workingDirectory));
                return 1;
            }

            var hasError = false;
            var restoreFetchOptions = options.NoCache ? FetchOptions.Latest : FetchOptions.UseCache;
            var buildFetchOptions = options.NoRestore ? FetchOptions.NoFetch : FetchOptions.UseCache;
            Parallel.ForEach(docsets, docset =>
            {
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

            using var errorLog = new ErrorLog(outputPath);
            using var disposables = new DisposableCollector();

            try
            {
                var configLoader = new ConfigLoader(errorLog);
                var (errors, config, buildOptions, packageResolver, fileResolver) =
                    configLoader.Load(disposables, docsetPath, outputPath, options, fetchOptions);
                if (errorLog.Write(errors))
                {
                    return true;
                }

                new OpsPreProcessor(config, errorLog, buildOptions).Run();
                var sourceMap = new SourceMap(new PathString(buildOptions.DocsetPath), config, fileResolver);
                errorLog.Configure(config, buildOptions.OutputPath, sourceMap);
                using var context = new Context(errorLog, config, buildOptions, packageResolver, fileResolver, sourceMap);
                Run(context);
                return errorLog.ErrorCount > 0;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errorLog.Write(dex);
                return errorLog.ErrorCount > 0;
            }
            finally
            {
                Telemetry.TrackOperationTime("build", stopwatch.Elapsed);
                Log.Important($"Build done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                errorLog.PrintSummary();
            }
        }

        private static void Run(Context context)
        {
            using (Progress.Start("Building files"))
            {
                ParallelUtility.ForEach(
                    context.ErrorLog,
                    context.PublishUrlMap.GetAllFiles(),
                    file => BuildFile(context, file));
            }

            Parallel.Invoke(
                () => context.BookmarkValidator.Validate(),
                () => context.ContentValidator.PostValidate(),
                () => context.ErrorLog.Write(context.MetadataValidator.PostValidate()),
                () => context.ContributionProvider.Save(),
                () => context.RepositoryProvider.Save(),
                () => context.ErrorLog.Write(context.GitHubAccessor.Save()),
                () => context.ErrorLog.Write(context.MicrosoftGraphAccessor.Save()));

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
    }
}
