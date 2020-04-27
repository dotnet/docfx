// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Build
    {
        public static int Run(string workingDirectory, CommandLineOptions options)
        {
            options.UseCache = true;
            var docsets = ConfigLoader.FindDocsets(workingDirectory, options);
            if (docsets.Length == 0)
            {
                ErrorLog.PrintError(Errors.Config.ConfigNotFound(workingDirectory));
                return 1;
            }

            var hasError = false;
            Parallel.ForEach(docsets, docset =>
            {
                if (!options.NoRestore && Restore.RestoreDocset(docset.docsetPath, docset.outputPath, options))
                {
                    hasError = true;
                    return;
                }

                if (BuildDocset(docset.docsetPath, docset.outputPath, options))
                {
                    hasError = true;
                }
            });
            return hasError ? 1 : 0;
        }

        private static bool BuildDocset(string docsetPath, string? outputPath, CommandLineOptions options)
        {
            using var errorLog = new ErrorLog(outputPath, options.Legacy);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var configLoader = new ConfigLoader(errorLog);
                var (errors, config, buildOptions, packageResolver, fileResolver) = configLoader.Load(docsetPath, outputPath, options);
                if (errorLog.Write(errors))
                {
                    return true;
                }

                new OpsPreProcessor(config, buildOptions).Run();
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
                context.BuildQueue.Start(file => BuildFile(context, file));

                Parallel.Invoke(
                    () => context.BuildQueue.Enqueue(context.RedirectionProvider.Files),
                    () => context.BuildQueue.Enqueue(context.BuildScope.GetFiles(ContentType.Resource)),
                    () => context.BuildQueue.Enqueue(context.BuildScope.GetFiles(ContentType.Page)),
                    () => context.BuildQueue.Enqueue(context.TocMap.GetFiles()));

                context.BuildQueue.WaitForCompletion();
            }

            Parallel.Invoke(
                () => context.BookmarkValidator.Validate(),
                () => context.ContentValidator.PostValidate(),
                () => context.ErrorLog.Write(context.MetadataProvider.PostValidate()),
                () => context.ContributionProvider.Save(),
                () => context.RepositoryProvider.Save(),
                () => context.ErrorLog.Write(context.GitHubAccessor.Save()),
                () => context.ErrorLog.Write(context.MicrosoftGraphAccessor.Save()));

            // TODO: explicitly state that ToXrefMapModel produces errors
            var xrefMapModel = context.XrefResolver.ToXrefMapModel();
            var (errors, publishModel, fileManifests) = context.PublishModelBuilder.Build();
            context.ErrorLog.Write(errors);

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
                () => context.Output.WriteJson(".links.json", context.FileLinkMapBuilder.Build()),
                () => Legacy.ConvertToLegacyModel(context.BuildOptions.DocsetPath, context, fileManifests, dependencyMap));
        }

        private static void BuildFile(Context context, FilePath path)
        {
            var file = context.DocumentProvider.GetDocument(path);
            var errors = file.ContentType switch
            {
                ContentType.TableOfContents => BuildTableOfContents.Build(context, file),
                ContentType.Resource when path.Origin != FileOrigin.Fallback => BuildResource.Build(context, file),
                ContentType.Page when path.Origin != FileOrigin.Fallback => BuildPage.Build(context, file),
                ContentType.Redirection when path.Origin != FileOrigin.Fallback => BuildRedirection.Build(context, file),
                _ => new List<Error>(),
            };

            context.ErrorLog.Write(errors);
        }
    }
}
