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
                var (errors, docset, fallbackDocset) = GetDocsetWithFallback(
                    docsetPath, locale, options, repository, restoreGitMap);

                errorLog.Configure(docset.Config);

                // just return if config loading has errors
                if (errorLog.Write(errors))
                    return;

                var outputPath = Path.Combine(docsetPath, docset.Config.Output.Path);
                var dependencyDocsets = LoadDependencies(docset, restoreGitMap);

                await Run(docset, fallbackDocset, dependencyDocsets, options, errorLog, outputPath, restoreGitMap);
            }
        }

        private static (List<Error> errors, Docset docset, Docset fallbackDocset) GetDocsetWithFallback(
            string docsetPath,
            string locale,
            CommandLineOptions options,
            Repository repository,
            RestoreGitMap restoreGitMap)
        {
            var fallbackRepo = GetFallbackRepository(docsetPath, repository, restoreGitMap);
            var (errors, config) = GetBuildConfig(docsetPath, options, locale, fallbackRepo);

            var currentDocset = new Docset(docsetPath, locale, config, repository);
            if (!string.IsNullOrEmpty(currentDocset.Locale) && !string.Equals(currentDocset.Locale, config.Localization.DefaultLocale))
            {
                if (fallbackRepo != null)
                {
                    return (errors, currentDocset, new Docset(fallbackRepo.Path, locale, config, fallbackRepo));
                }

                if (LocalizationUtility.TryGetLocalizationDocset(
                    restoreGitMap,
                    currentDocset,
                    config,
                    currentDocset.Locale,
                    out var localizationDocset,
                    out var localizationRepository))
                {
                    return (errors,
                        new Docset(
                        localizationDocset,
                        currentDocset.Locale,
                        config,
                        localizationRepository),
                        currentDocset);
                }
            }

            return (errors, currentDocset, default);
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

        private static Repository GetFallbackRepository(
            string docsetPath,
            Repository repository,
            RestoreGitMap restoreGitMap)
        {
            Debug.Assert(restoreGitMap != null);
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out string fallbackBranch, out _))
            {
                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (restoreGitMap.IsBranchRestored(fallbackRemote, branch))
                    {
                        var (fallbackRepoPath, fallbackRepoCommit) = restoreGitMap.GetRestoreGitPath(new PackageUrl(fallbackRemote, branch), bare: false);
                        return Repository.Create(fallbackRepoPath, branch, fallbackRemote, fallbackRepoCommit, true);
                    }
                }
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

        private static Dictionary<string, (Docset docset, bool inScope)> LoadDependencies(Docset docset, RestoreGitMap restoreGitMap)
        {
            var config = docset.Config;
            var result = new Dictionary<string, (Docset docset, bool inScope)>(config.Dependencies.Count, PathUtility.PathComparer);

            foreach (var (name, dependency) in config.Dependencies)
            {
                var (dir, commit) = restoreGitMap.GetRestoreGitPath(dependency, true);

                var repository = Repository.Create(dir, dependency.Branch, dependency.Url, commit, true);
                result.TryAdd(name, (new Docset(dir, docset.Locale, config, repository), dependency.BuildFiles));
            }

            return result;
        }
    }
}
