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
            var repository = Repository.Create(docsetPath);
            Telemetry.SetRepository(repository?.Remote, repository?.Branch);

            var locale = LocalizationUtility.GetLocale(repository?.Remote, repository?.Branch, options);
            using (var restoreGitMap = GetRestoreGitMap(docsetPath, locale, options))
            {
                var fallbackRepo = GetFallbackRepository(docsetPath, repository, restoreGitMap);

                var (configErrors, config) = GetBuildConfig(docsetPath, options, locale, fallbackRepo);
                errorLog.Configure(config);

                // just return if config loading has errors
                if (errorLog.Write(configErrors))
                    return;

                var (docset, fallbackDocset) = GetDocsetWithFallback(
                    docsetPath, repository, locale, fallbackRepo, config, restoreGitMap);
                var outputPath = Path.Combine(docsetPath, config.Output.Path);

                await Run(docset, fallbackDocset, restoreGitMap, options, errorLog, outputPath);
            }
        }

        private static (Docset docset, Docset fallbackDocset) GetDocsetWithFallback(
            string docsetPath,
            Repository repository,
            string locale,
            Repository fallbackRepo,
            Config config,
            RestoreGitMap gitMap)
        {
            var currentDocset = new Docset(docsetPath, locale, config, repository);
            if (!string.IsNullOrEmpty(currentDocset.Locale) && !string.Equals(currentDocset.Locale, config.Localization.DefaultLocale))
            {
                if (fallbackRepo != null)
                {
                    return (currentDocset, new Docset(fallbackRepo.Path, locale, config, fallbackRepo));
                }

                if (LocalizationUtility.TryGetLocalizedDocsetPath(
                    currentDocset,
                    gitMap,
                    config,
                    currentDocset.Locale,
                    out var localizationDocsetPath,
                    out var localizationBranch))
                {
                    var repo = Repository.Create(localizationDocsetPath, localizationBranch);
                    return (new Docset(localizationDocsetPath, currentDocset.Locale, config, repo), currentDocset);
                }
            }

            return (currentDocset, default);
        }

        private static async Task Run(
            Docset docset,
            Docset fallbackDocset,
            RestoreGitMap restoreGitMap,
            CommandLineOptions options,
            ErrorLog errorLog,
            string outputPath)
        {
            using (var context = new Context(outputPath, errorLog, docset, fallbackDocset, restoreGitMap))
            {
                context.BuildQueue.Enqueue(context.BuildScope.Files);

                using (Progress.Start("Building files"))
                {
                    await context.BuildQueue.Drain(file => BuildFile(context, file), Progress.Update);
                }

                context.BookmarkValidator.Validate();

                var (publishModel, fileManifests) = context.PublishModelBuilder.Build(context);
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
            var dependencyLockPath = string.IsNullOrEmpty(config.DependencyLock)
                ? new SourceInfo<string>(AppData.GetDependencyLockFile(docsetPath, locale)) : config.DependencyLock;

            var dependenyGitLock = DependencyLockProvider.LoadGitLock(docsetPath, dependencyLockPath) ?? new Dictionary<PackageUrl, DependencyGitLock>();
            return RestoreGitMap.Create(dependenyGitLock);
        }

        private static Repository GetFallbackRepository(
            string docsetPath,
            Repository repository,
            RestoreGitMap restoreGitMap)
        {
            Debug.Assert(restoreGitMap != null);
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out string fallbackBranch, out _))
            {
                var fallbackPackageUrl = new PackageUrl(fallbackRemote, fallbackBranch);
                if (restoreGitMap.DependencyGitLock.GetGitLock(fallbackPackageUrl) == null
                    && restoreGitMap.DependencyGitLock.GetGitLock(new PackageUrl(fallbackRemote, "master")) != null)
                {
                    // fallback to master branch
                    fallbackPackageUrl = new PackageUrl(fallbackRemote, "master");
                }

                var fallbackRepoPath = restoreGitMap.GetGitRestorePath(fallbackPackageUrl);
                return Repository.Create(fallbackRepoPath, fallbackBranch, fallbackRemote);
            }

            return default;
        }

        private static (List<Error> errors, Config config) GetBuildConfig(
            string docset,
            CommandLineOptions options,
            string locale,
            Repository fallbackRepo = null)
        {
            if (ConfigLoader.TryGetConfigPath(docset, out _) || fallbackRepo is null)
            {
                return ConfigLoader.Load(docset, options, locale);
            }

            return ConfigLoader.Load(fallbackRepo.Path, options, locale);
        }
    }
}
