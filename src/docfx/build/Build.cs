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
        public static async Task<int> Run(string workingDirectory, CommandLineOptions options)
        {
            var docsets = ConfigLoader.FindDocsets(workingDirectory, options);
            if (docsets.Length == 0)
            {
                ErrorLog.PrintError(Errors.ConfigNotFound(workingDirectory));
                return 1;
            }

            var result = await Task.WhenAll(docsets.Select(docset => BuildDocset(docset.docsetPath, docset.outputPath, options)));
            return result.All(x => x) ? 0 : 1;
        }

        private static async Task<bool> BuildDocset(string docsetPath, string outputPath, CommandLineOptions options)
        {
            List<Error> errors;
            Config config = null;
            RestoreGitMap restoreGitMap = null;

            using (var errorLog = new ErrorLog(docsetPath, outputPath, () => config, options.Legacy))
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    // load and trace entry repository
                    var repository = Repository.Create(docsetPath);
                    Telemetry.SetRepository(repository?.Remote, repository?.Branch);
                    var locale = LocalizationUtility.GetLocale(repository, options);

                    using (restoreGitMap = RestoreGitMap.Create(docsetPath, locale))
                    {
                        var configLoader = new ConfigLoader(repository);
                        (errors, config) = await configLoader.Load(docsetPath, errorLog, options, noFetch: true);
                        if (errorLog.Write(errors))
                            return false;

                        var localizationProvider = new LocalizationProvider(restoreGitMap, options, config, locale, docsetPath, repository);
                        var repositoryProvider = new RepositoryProvider(docsetPath, repository, options, config, restoreGitMap, localizationProvider);
                        var input = new Input(docsetPath, repositoryProvider, localizationProvider);

                        // get docsets(build docset, fallback docset and dependency docsets)
                        var (docset, fallbackDocset) = GetDocsetWithFallback(locale, config, localizationProvider);

                        // run build based on docsets
                        outputPath = outputPath ?? Path.Combine(docsetPath, docset.Config.Output.Path);
                        await Run(docset, fallbackDocset, options, errorLog, outputPath, input, repositoryProvider, localizationProvider);
                    }
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    Log.Write(dex);
                    errorLog.Write(dex.Error, dex.OverwriteLevel);
                    return false;
                }
                finally
                {
                    Telemetry.TrackOperationTime("build", stopwatch.Elapsed);
                    Log.Important($"Build '{config?.Name}' done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                    errorLog.PrintSummary();
                }
                return true;
            }
        }

        private static (Docset docset, Docset fallbackDocset) GetDocsetWithFallback(
            string locale,
            Config config,
            LocalizationProvider localizationProvider)
        {
            var (currentDocsetPath, currentRepo) = localizationProvider.GetBuildRepositoryWithDocsetEntry();
            var currentDocset = new Docset(currentDocsetPath, locale, config, currentRepo);
            if (localizationProvider.IsLocalizationBuild)
            {
                var (fallbackDocsetPath, fallbackRepo) = localizationProvider.GetFallbackRepositoryWithDocsetEntry();
                if (fallbackRepo != null)
                {
                    return (currentDocset, new Docset(fallbackDocsetPath, locale, config, fallbackRepo));
                }
            }

            return (currentDocset, default);
        }

        private static async Task Run(
            Docset docset,
            Docset fallbackDocset,
            CommandLineOptions options,
            ErrorLog errorLog,
            string outputPath,
            Input input,
            RepositoryProvider repositoryProvider,
            LocalizationProvider localizationProvider)
        {
            using (var context = new Context(outputPath, errorLog, docset, fallbackDocset, input, repositoryProvider, localizationProvider))
            {
                context.BuildQueue.Enqueue(context.BuildScope.Files.Concat(context.RedirectionProvider.Files));

                using (Progress.Start("Building files"))
                {
                    await context.BuildQueue.Drain(file => BuildFile(context, file), Progress.Update);
                }

                context.BookmarkValidator.Validate();

                var (publishModel, fileManifests) = context.PublishModelBuilder.Build(context, docset.Legacy);
                var dependencyMap = context.DependencyMapBuilder.Build();
                var xrefMapModel = context.XrefResolver.ToXrefMapModel();
                var fileLinkMap = context.FileLinkMapBuilder.Build();

                context.Output.WriteJson(xrefMapModel, ".xrefmap.json");
                context.Output.WriteJson(publishModel, ".publish.json");
                context.Output.WriteJson(dependencyMap.ToDependencyMapModel(), ".dependencymap.json");
                context.Output.WriteJson(fileLinkMap, ".links.json");

                if (options.Legacy)
                {
                    if (docset.Config.Output.Json)
                    {
                        // TODO: decouple files and dependencies from legacy.
                        Legacy.ConvertToLegacyModel(docset, context, fileManifests, dependencyMap);
                    }
                    else
                    {
                        context.TemplateEngine.CopyTo(outputPath);
                    }
                }

                context.ContributionProvider.Save();
                context.GitCommitProvider.Save();

                errorLog.Write(await context.GitHubAccessor.Save());
                errorLog.Write(await context.MicrosoftGraphAccessor.Save());
            }
        }

        private static async Task BuildFile(Context context, FilePath path)
        {
            var file = context.DocumentProvider.GetDocument(path);
            if (!ShouldBuildFile(context, file))
            {
                context.PublishModelBuilder.ExcludeFromOutput(file);
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
                        errors = await BuildPage.Build(context, file);
                        break;
                    case ContentType.TableOfContents:
                        // TODO: improve error message for toc monikers overlap
                        errors = BuildTableOfContents.Build(context, file);
                        break;
                    case ContentType.Redirection:
                        errors = BuildRedirection.Build(context, file);
                        break;
                }

                if (context.ErrorLog.Write(path, errors))
                {
                    context.PublishModelBuilder.ExcludeFromOutput(file);
                }

                Telemetry.TrackBuildItemCount(file.ContentType);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.ErrorLog.Write(path, dex.Error, dex.OverwriteLevel);
                context.PublishModelBuilder.ExcludeFromOutput(file);
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
