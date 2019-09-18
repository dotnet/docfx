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
        public static async Task Run(string docsetPath, CommandLineOptions options, ErrorLog errorLog)
        {
            var repositoryProvider = new RepositoryProvider(docsetPath);
            var repository = repositoryProvider.GetRepository(FileOrigin.Default);
            Telemetry.SetRepository(repository?.Remote, repository?.Branch);

            var locale = LocalizationUtility.GetLocale(repository?.Remote, repository?.Branch, options);

            using (var restoreGitMap = GetRestoreGitMap(docsetPath, locale, options))
            {
                repositoryProvider = repositoryProvider.WithRestoreMap(restoreGitMap);
                var (errors, config) = GetBuildConfig(docsetPath, options, locale, repositoryProvider);

                errorLog.Configure(config);

                // just return if config loading has errors
                if (errorLog.Write(errors))
                    return;

                repositoryProvider = repositoryProvider.WithConfig(config);
                var (docset, fallbackDocset) = GetDocsetWithFallback(docsetPath, locale, config, repositoryProvider, restoreGitMap);
                var outputPath = Path.Combine(docsetPath, docset.Config.Output.Path);
                var dependencyDocsets = LoadDependencies(docset, repositoryProvider);

                await Run(docset, fallbackDocset, dependencyDocsets, options, errorLog, outputPath, restoreGitMap);
            }
        }

        private static (Docset docset, Docset fallbackDocset) GetDocsetWithFallback(
            string docsetPath,
            string locale,
            Config config,
            RepositoryProvider repositoryProvider,
            RestoreGitMap restoreGitMap)
        {
            var currentDocset = new Docset(docsetPath, locale, config, repositoryProvider.GetRepository(FileOrigin.Default));
            if (!string.IsNullOrEmpty(currentDocset.Locale) && !string.Equals(currentDocset.Locale, config.Localization.DefaultLocale))
            {
                var fallbackRepo = repositoryProvider.GetRepository(FileOrigin.Fallback);
                if (fallbackRepo != null)
                {
                    return (currentDocset, new Docset(fallbackRepo.Path, locale, config, fallbackRepo));
                }

                // todo: get localization repository from repository provider
                if (LocalizationUtility.TryGetLocalizationDocset(
                    restoreGitMap,
                    currentDocset,
                    config,
                    currentDocset.Locale,
                    out var localizationDocset,
                    out var localizationRepository))
                {
                    return (new Docset(
                        localizationDocset,
                        currentDocset.Locale,
                        config,
                        localizationRepository),
                        currentDocset);
                }
            }

            return (currentDocset, default);
        }

        private static async Task Run(
            Docset docset,
            Docset fallbackDocset,
            Dictionary<string, (Docset, bool)> dependencyDocsets,
            CommandLineOptions options,
            ErrorLog errorLog,
            string outputPath,
            RestoreGitMap restoreGitMap)
        {
            using (var context = new Context(outputPath, errorLog, docset, fallbackDocset, dependencyDocsets, restoreGitMap))
            {
                context.BuildQueue.Enqueue(context.BuildScope.Files);

                using (Progress.Start("Building files"))
                {
                    await context.BuildQueue.Drain(file => BuildFile(context, file), Progress.Update);
                }

                context.BookmarkValidator.Validate();

                var (publishModel, fileManifests) = context.PublishModelBuilder.Build(context, docset.Legacy);
                var dependencyMap = context.DependencyMapBuilder.Build();
                var xrefMapModel = context.XrefResolver.ToXrefMapModel();

                context.Output.WriteJson(xrefMapModel, ".xrefmap.json");
                context.Output.WriteJson(publishModel, ".publish.json");
                context.Output.WriteJson(dependencyMap.ToDependencyMapModel(), ".dependencymap.json");

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

                context.GitHubUserCache.Save();
                context.MicrosoftGraphCache.Save();
                context.ContributionProvider.Save();
                context.GitCommitProvider.Save();
            }
        }

        private static async Task BuildFile(Context context, Document file)
        {
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

                var hasErrors = context.ErrorLog.Write(file, errors);
                if (hasErrors)
                {
                    context.PublishModelBuilder.MarkError(file);
                }

                Telemetry.TrackBuildItemCount(file.ContentType);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.ErrorLog.Write(file, dex.Error, isException: true);
                context.PublishModelBuilder.MarkError(file);
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

        private static RestoreGitMap GetRestoreGitMap(
            string docsetPath,
            string locale,
            CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            var (_, config) = ConfigLoader.TryLoad(docsetPath, commandLineOptions);

            return RestoreGitMap.Create(docsetPath, config, locale);
        }

        private static (List<Error> errors, Config config) GetBuildConfig(
            string docset,
            CommandLineOptions options,
            string locale,
            RepositoryProvider repositoryProvider)
        {
            var fallbackRepo = repositoryProvider.GetRepository(FileOrigin.Fallback);
            if (ConfigLoader.TryGetConfigPath(docset, out _) || fallbackRepo is null)
            {
                return ConfigLoader.Load(docset, options, locale);
            }

            return ConfigLoader.Load(fallbackRepo.Path, options, locale);
        }

        private static Dictionary<string, (Docset docset, bool inScope)> LoadDependencies(Docset docset, RepositoryProvider repositoryProvider)
        {
            var config = docset.Config;
            var result = new Dictionary<string, (Docset docset, bool inScope)>(config.Dependencies.Count, PathUtility.PathComparer);

            foreach (var (name, dependency) in config.Dependencies)
            {
                var repository = repositoryProvider.GetRepository(FileOrigin.Dependency, name);
                result.TryAdd(name, (new Docset(repository.Path, docset.Locale, config, repository), dependency.BuildFiles));
            }

            return result;
        }
    }
}
