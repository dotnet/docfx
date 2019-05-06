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
        public static async Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            var repository = Repository.Create(docsetPath);
            Telemetry.SetRepository(repository?.Remote, repository?.Branch);

            var locale = LocalizationUtility.GetLocale(repository?.Remote, repository?.Branch, options);
            var (restoreMap, fallbackRepo) = LoadRestoreMap(docsetPath, locale, repository, options);

            try
            {
                await Run(docsetPath, repository, locale, options, report, restoreMap, fallbackRepo);
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
            Report report,
            RestoreMap restoreMap,
            Repository fallbackRepo = null)
        {
            XrefMap xrefMap = null;
            var (configErrors, config) = GetBuildConfig(docsetPath, options, locale, fallbackRepo);
            report.Configure(config);

            // just return if config loading has errors
            if (report.Write(config.ConfigFileName, configErrors))
                return;

            var docset = GetBuildDocset(new Docset(report, docsetPath, locale, config, options, restoreMap, repository, fallbackRepo));
            var outputPath = Path.Combine(docsetPath, config.Output.Path);

            using (var context = Context.Create(outputPath, report, docset, () => xrefMap))
            {
                xrefMap = XrefMap.Create(context, docset);
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

                context.Report.Write(await saveGitHubUserCache);

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
                var monikerMapBuilder = new MonikerMapBuilder();

                await ParallelUtility.ForEach(
                    docset.BuildScope,
                    async (file, buildChild) => { monikerMapBuilder.Add(file, await BuildOneFile(file, buildChild, null)); },
                    (file) => ShouldBuildFile(file, new ContentType[] { ContentType.Page, ContentType.Redirection, ContentType.Resource }),
                    Progress.Update);

                var monikerMap = monikerMapBuilder.Build();

                // Build TOC: since toc file depends on the build result of every node
                await ParallelUtility.ForEach(
                    GetTableOfContentsScope(docset, tocMap),
                    (file, buildChild) => BuildOneFile(file, buildChild, monikerMap),
                    ShouldBuildTocFile,
                    Progress.Update);

                context.GitCommitProvider.SaveGitCommitCache();

                ValidateBookmarks();

                var (publishModel, fileManifests) = context.PublishModelBuilder.Build(context, docset.Legacy);
                var dependencyMap = context.DependencyMapBuilder.Build();

                return (publishModel, fileManifests, dependencyMap);

                async Task<List<string>> BuildOneFile(
                    Document file,
                    Action<Document> buildChild,
                    MonikerMap fileMonikerMap)
                {
                    return await BuildFile(context, file, tocMap, fileMonikerMap, buildChild);
                }

                bool ShouldBuildFile(Document file, ContentType[] shouldBuildContentTypes)
                {
                    // source content in a localization docset
                    if (docset.IsLocalized() && !file.Docset.IsLocalized())
                    {
                        return false;
                    }

                    return shouldBuildContentTypes.Contains(file.ContentType) && recurseDetector.TryAdd(file);
                }

                bool ShouldBuildTocFile(Document file) => file.ContentType == ContentType.TableOfContents && tocMap.Contains(file);

                void ValidateBookmarks()
                {
                    foreach (var (error, file) in context.BookmarkValidator.Validate())
                    {
                        // TODO: clean up Report.Write inputting file, should take file from Error
                        if (context.Report.Write(file.FilePath, new List<Error> { error }))
                        {
                            context.PublishModelBuilder.MarkError(file);
                        }
                    }
                }
            }
        }

        private static async Task<List<string>> BuildFile(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            MonikerMap monikerMap,
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
                        (errors, publishItem) = BuildTableOfContents.Build(context, file, monikerMap);
                        break;
                    case ContentType.Redirection:
                        (errors, publishItem) = BuildRedirection.Build(context, file);
                        break;
                }

                var hasErrors = context.Report.Write(file.ToString(), errors);
                if (hasErrors)
                {
                    context.PublishModelBuilder.MarkError(file);
                }

                Telemetry.TrackBuildItemCount(file.ContentType);
                return publishItem.Monikers;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report.Write(file.ToString(), dex.Error);
                context.PublishModelBuilder.MarkError(file);
                return new List<string>();
            }
            catch
            {
                Console.WriteLine($"Build {file.FilePath} failed");
                throw;
            }
        }

        private static (RestoreMap restoreMap, Repository fallbackRepository) LoadRestoreMap(
            string docset,
            string locale,
            Repository repository,
            CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var (_, config) = ConfigLoader.TryLoad(docset, commandLineOptions);

            var dependencyLock = DependencyLock.Load(docset, string.IsNullOrEmpty(config.DependencyLock) ? new SourceInfo<string>(AppData.GetDependencyLockFile(docset, locale)) : config.DependencyLock) ?? new DependencyLockModel();
            var restoreMap = RestoreMap.Create(dependencyLock);

            if (LocalizationUtility.TryGetSourceRepository(repository, out var remote, out string branch, out _))
            {
                if (dependencyLock.GetGitLock(remote, branch) == null && dependencyLock.GetGitLock(remote, "master") != null)
                {
                    // fallback to master branch
                    branch = "master";
                }

                var (fallbackRepoPath, fallbackRestoreMap) = restoreMap.GetGitRestorePath(remote, branch);
                var fallbackRepository = Repository.Create(fallbackRepoPath, branch, remote);

                if (!ConfigLoader.TryGetConfigPath(docset, out _))
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

        private static IReadOnlyList<Document> GetTableOfContentsScope(Docset docset, TableOfContentsMap tocMap)
        {
            Debug.Assert(tocMap != null);

            var result = docset.BuildScope.Where(d => d.ContentType == ContentType.TableOfContents).ToList();

            if (!docset.IsLocalized())
            {
                return result;
            }

            // if A toc includes B toc and only B toc is localized, then A need to be included and built
            var fallbackTocs = new List<Document>();
            foreach (var toc in result)
            {
                if (tocMap.TryFindParents(toc, out var parents))
                {
                    fallbackTocs.AddRange(parents);
                }
            }

            result.AddRange(fallbackTocs);

            return result;
        }
    }
}
