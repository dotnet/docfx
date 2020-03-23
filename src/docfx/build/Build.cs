// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                if (BuildDocset(docset.docsetPath, docset.outputPath, options))
                {
                    hasError = true;
                }
            });
            return hasError ? 1 : 0;
        }

        private static bool BuildDocset(string docsetPath, string? outputPath, CommandLineOptions options)
        {
            List<Error> errors;
            Config? config = null;

            using var errorLog = new ErrorLog(docsetPath, outputPath, () => config, options.Legacy);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // load and trace entry repository
                var repository = Repository.Create(docsetPath);
                Telemetry.SetRepository(repository?.Remote, repository?.Branch);
                var locale = LocalizationUtility.GetLocale(repository);

                var configLoader = new ConfigLoader(repository, errorLog);
                (errors, config) = configLoader.Load(docsetPath, locale, options);
                if (errorLog.Write(errors))
                    return false;

                using var packageResolver = new PackageResolver(docsetPath, config, options.FetchOptions);
                var localizationProvider = new LocalizationProvider(packageResolver, config, locale, docsetPath, repository);
                var repositoryProvider = new RepositoryProvider(docsetPath, repository, config, packageResolver, localizationProvider);
                var input = new Input(docsetPath, repositoryProvider);

                // get docsets(build docset, fallback docset and dependency docsets)
                var docset = new Docset(docsetPath);
                var fallbackDocset = localizationProvider.GetFallbackDocset();

                // run build based on docsets
                outputPath ??= Path.Combine(docsetPath, config.OutputPath);
                Run(config, docset, fallbackDocset, options, errorLog, outputPath, input, repositoryProvider, localizationProvider, packageResolver);
                return true;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errorLog.Write(dex);
                return false;
            }
            finally
            {
                Telemetry.TrackOperationTime("build", stopwatch.Elapsed);
                Log.Important($"Build '{config?.Name}' done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                errorLog.PrintSummary();
            }
        }

        private static void Run(
            Config config,
            Docset docset,
            Docset? fallbackDocset,
            CommandLineOptions options,
            ErrorLog errorLog,
            string outputPath,
            Input input,
            RepositoryProvider repositoryProvider,
            LocalizationProvider localizationProvider,
            PackageResolver packageResolver)
        {
            using var context = new Context(outputPath, errorLog, options, config, docset, fallbackDocset, input, repositoryProvider, localizationProvider, packageResolver);
            context.BuildQueue.Enqueue(context.BuildScope.Files.Concat(context.RedirectionProvider.Files));

            using (Progress.Start("Building files"))
            {
                context.BuildQueue.Drain(file => BuildFile(context, file), Progress.Update);
            }

            context.BookmarkValidator.Validate();
            context.ErrorLog.Write(context.MetadataProvider.Validate());
            context.ErrorLog.Write(context.DocsValidator.PostValidate().GetAwaiter().GetResult());

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

                if (options.Legacy)
                {
                    if (context.Config.OutputJson)
                    {
                        // TODO: decouple files and dependencies from legacy.
                        Legacy.ConvertToLegacyModel(docset, context, fileManifests, dependencyMap);
                    }
                    else
                    {
                        context.TemplateEngine.CopyTo(outputPath);
                    }
                }
            }

            context.ContributionProvider.Save();
            context.GitCommitProvider.Save();

            errorLog.Write(context.GitHubAccessor.Save());
            errorLog.Write(context.MicrosoftGraphAccessor.Save());
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
                Telemetry.TrackBuildItemCount(file.ContentType);
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
