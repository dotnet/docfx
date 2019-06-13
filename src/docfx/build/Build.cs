// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
            var (restoreMap, fallbackRepo) = LoadRestoreMap(docsetPath, locale, repository, options);

            try
            {
                await Run(docsetPath, repository, locale, options, errorLog, restoreMap, fallbackRepo);
            }
            finally
            {
                restoreMap.Release();
            }
        }

        private static async Task Run(
            string docsetPath,
            Repository repository,
            string locale,
            CommandLineOptions options,
            ErrorLog errorLog,
            RestoreMap restoreMap,
            Repository fallbackRepo = null)
        {
            XrefMap xrefMap = null;
            var (configErrors, config) = GetBuildConfig(docsetPath, options, locale, fallbackRepo);
            errorLog.Configure(config);

            // just return if config loading has errors
            if (errorLog.Write(configErrors))
                return;

            var docset = GetBuildDocset(new Docset(errorLog, docsetPath, locale, config, options, restoreMap, repository, fallbackRepo));
            var outputPath = Path.Combine(docsetPath, config.Output.Path);

            using (var context = new Context(outputPath, errorLog, docset, () => xrefMap))
            {
                xrefMap = XrefMapBuilder.Build(context, docset);
                var tocMap = TableOfContentsMap.Create(context, docset);

                var (publishManifest, fileManifests, sourceDependencies) = await BuildFiles(context, docset, tocMap);

                var saveGitHubUserCache = context.GitHubUserCache.SaveChanges(config);

                xrefMap.OutputXrefMap(context);
                context.Output.WriteJson(publishManifest, ".publish.json");
                context.Output.WriteJson(sourceDependencies.ToDependencyMapModel(), ".dependencymap.json");

                if (options.Legacy)
                {
                    if (config.Output.Json)
                    {
                        // TODO: decouple files and dependencies from legacy.
                        Legacy.ConvertToLegacyModel(docset, context, fileManifests, sourceDependencies, tocMap);
                    }
                    else
                    {
                        context.Template.CopyTo(outputPath);
                    }
                }

                context.ErrorLog.Write(await saveGitHubUserCache);

                context.ContributionProvider.UpdateCommitBuildTime();
            }
        }

        private static async Task<(PublishModel, Dictionary<Document, PublishItem>, DependencyMap)> BuildFiles(
            Context context,
            Docset docset,
            TableOfContentsMap tocMap)
        {
            using (Progress.Start("Building files"))
            {
                var recurseDetector = new ConcurrentHashSet<Document>();

                await ParallelUtility.ForEach(
                    GetBuildScope(docset, tocMap),
                    async (file, buildChild) => { await BuildFile(context, file, tocMap, buildChild); },
                    (file) => ShouldBuildFile(file),
                    Progress.Update);

                context.GitCommitProvider.SaveGitCommitCache();

                ValidateBookmarks();

                var (publishModel, fileManifests) = context.PublishModelBuilder.Build(context, docset.Legacy);
                var dependencyMap = context.DependencyMapBuilder.Build();

                return (publishModel, fileManifests, dependencyMap);

                bool ShouldBuildFile(Document file)
                {
                    // source content in a localization docset
                    if (file.ContentType != ContentType.TableOfContents && docset.IsLocalized() && !file.Docset.IsLocalized())
                    {
                        return false;
                    }

                    if (file.ContentType == ContentType.TableOfContents && !tocMap.Contains(file))
                    {
                        return false;
                    }

                    return recurseDetector.TryAdd(file);
                }

                void ValidateBookmarks()
                {
                    foreach (var (error, file) in context.BookmarkValidator.Validate())
                    {
                        // TODO: clean up ErrorLog.Write inputting file, should take file from Error
                        if (context.ErrorLog.Write(file.FilePath, new List<Error> { error }))
                        {
                            context.PublishModelBuilder.MarkError(file);
                        }
                    }
                }
            }
        }

        private static async Task BuildFile(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            Action<Document> buildChild)
        {
            try
            {
                var publishItem = default(PublishItem);
                var errors = Enumerable.Empty<Error>();

                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        (errors, publishItem) = BuildResource.Build(context, file);
                        break;
                    case ContentType.Page:
                        (errors, publishItem) = await BuildPage.Build(context, file, tocMap, buildChild);
                        break;
                    case ContentType.TableOfContents:
                        // TODO: improve error message for toc monikers overlap
                        (errors, publishItem) = BuildTableOfContents.Build(context, file);
                        break;
                    case ContentType.Redirection:
                        (errors, publishItem) = BuildRedirection.Build(context, file);
                        break;
                }

                var hasErrors = context.ErrorLog.Write(file.ToString(), errors);
                if (hasErrors)
                {
                    context.PublishModelBuilder.MarkError(file);
                }

                Telemetry.TrackBuildItemCount(file.ContentType);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.ErrorLog.Write(file.ToString(), dex.Error);
                context.PublishModelBuilder.MarkError(file);
            }
            catch
            {
                Console.WriteLine($"Build {file.FilePath} failed");
                throw;
            }
        }

        private static (RestoreMap restoreMap, Repository fallbackRepository) LoadRestoreMap(
            string docsetPath,
            string locale,
            Repository repository,
            CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            var (_, config) = ConfigLoader.TryLoad(docsetPath, commandLineOptions);

            var dependencyLock = DependencyLock.Load(docsetPath, string.IsNullOrEmpty(config.DependencyLock) ? new SourceInfo<string>(AppData.GetDependencyLockFile(docsetPath, locale)) : config.DependencyLock) ?? new DependencyLockModel();
            var restoreMap = RestoreMap.Create(dependencyLock);

            if (LocalizationUtility.TryGetSourceRepository(repository, out var remote, out string branch, out _))
            {
                if (dependencyLock.GetGitLock(remote, branch) == null && dependencyLock.GetGitLock(remote, "master") != null)
                {
                    // fallback to master branch
                    branch = "master";
                }

                var (fallbackRepoPath, fallbackRestoreMap) = restoreMap.GetGitRestorePath(remote, branch, docsetPath);
                var fallbackRepository = Repository.Create(fallbackRepoPath, branch, remote);

                if (!ConfigLoader.TryGetConfigPath(docsetPath, out _))
                {
                    // build from loc repo directly with overwrite config
                    // which means it's using source repo's dependency loc;
                    return (fallbackRestoreMap, fallbackRepository);
                }

                return (restoreMap, fallbackRepository);
            }

            return (restoreMap, null);
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

        private static Docset GetBuildDocset(Docset sourceDocset)
        {
            Debug.Assert(sourceDocset != null);

            return sourceDocset.LocalizationDocset ?? sourceDocset;
        }

        private static IReadOnlyList<Document> GetBuildScope(Docset docset, TableOfContentsMap tocMap)
        {
            Debug.Assert(tocMap != null);

            if (!docset.IsLocalized())
            {
                return docset.BuildScope.ToList();
            }

            // if A toc includes B toc and only B toc is localized, then A need to be included and built
            var tocBuildScope = docset.BuildScope.Where(d => d.ContentType == ContentType.TableOfContents).ToList();
            var fallbackTocs = new List<Document>();
            foreach (var toc in tocBuildScope)
            {
                if (tocMap.TryFindParents(toc, out var parents))
                {
                    fallbackTocs.AddRange(parents);
                }
            }

            var buildScope = docset.BuildScope.ToList();
            buildScope.AddRange(fallbackTocs);

            return buildScope;
        }
    }
}
