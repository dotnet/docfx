// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
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
                    return true;

                errorLog.Configure(config, buildOptions.OutputPath);
                using var context = new Context(errorLog, config, buildOptions, packageResolver, fileResolver);
                Run(context);
                return false;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                return errorLog.Write(dex);
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
            context.BuildQueue.Enqueue(context.BuildScope.Files.Concat(context.RedirectionProvider.Files));

            using (Progress.Start("Building files"))
            {
                context.BuildQueue.Drain(file => BuildFile(context, file), Progress.Update);
            }

            context.BookmarkValidator.Validate();
            context.ContentValidator.PostValidate();
            context.ErrorLog.Write(context.MetadataProvider.Validate());

            var (errors, publishModel, fileManifests) = context.PublishModelBuilder.Build();
            context.ErrorLog.Write(errors);

            // TODO: explicitly state that ToXrefMapModel produces errors
            var xrefMapModel = context.XrefResolver.ToXrefMapModel();

            if (!context.Config.DryRun)
            {
                var dependencyMap = context.DependencyMapBuilder.Build();
                var fileLinkMap = context.FileLinkMapBuilder.Build();

                context.Output.WriteJson(".xrefmap.json", xrefMapModel);
                context.Output.WriteJson(".publish.json", publishModel);
                context.Output.WriteJson(".dependencymap.json", dependencyMap.ToDependencyMapModel());
                context.Output.WriteJson(".links.json", fileLinkMap);

                if (context.Config.OutputJson)
                {
                    // TODO: decouple files and dependencies from legacy.
                    Legacy.ConvertToLegacyModel(context.BuildOptions.DocsetPath, context, fileManifests, dependencyMap);
                }
            }

            context.ContributionProvider.Save();
            context.RepositoryProvider.Save();

            context.ErrorLog.Write(context.GitHubAccessor.Save());
            context.ErrorLog.Write(context.MicrosoftGraphAccessor.Save());
        }

        private static void BuildFile(Context context, FilePath path)
        {
            var file = context.DocumentProvider.GetDocument(path);
            if (!ShouldBuildFile(context, file))
            {
                return;
            }

            try
            {
                var errors = Enumerable.Empty<Error>();
                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        errors = BuildResource.Build(context, file);
                        break;
                    case ContentType.Page:
                        errors = BuildPage.Build(context, file);
                        break;
                    case ContentType.TableOfContents:
                        // TODO: improve error message for toc monikers overlap
                        errors = BuildTableOfContents.Build(context, file);
                        break;
                    case ContentType.Redirection:
                        errors = BuildRedirection.Build(context, file);
                        break;
                }

                context.ErrorLog.Write(errors);
                Telemetry.TrackBuildFileTypeCount(file);
                context.ContentValidator.ValidateManifest(file);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.ErrorLog.Write(dex);
            }
            catch
            {
                Console.WriteLine($"Build {file.FilePath} failed");
                throw;
            }
        }

        private static bool ShouldBuildFile(Context context, Document file)
        {
            if (file.ContentType == ContentType.TableOfContents)
            {
                if (!context.TocMap.Contains(file))
                {
                    return false;
                }

                // if A toc includes B toc and only B toc is localized, then A need to be included and built
                return file.FilePath.Origin != FileOrigin.Fallback
                    || (context.TocMap.TryGetTocReferences(file, out var tocReferences)
                        && tocReferences.Any(toc => toc.FilePath.Origin != FileOrigin.Fallback));
            }

            return file.FilePath.Origin != FileOrigin.Fallback;
        }
    }
}
