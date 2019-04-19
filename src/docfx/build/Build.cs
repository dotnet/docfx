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

            var sourceRepoInfo = LocalizationUtility.TryGetSourceRepository(repository, out var remote, out string branch, out var locale) ? (remote, branch) : default;

            locale = locale ?? options.Locale;
            var dependencyLock = LoadBuildDependencyLock(docsetPath, locale, options, sourceRepoInfo);
            var restoreMap = RestoreMap.Create(dependencyLock);

            var sourceRepo = sourceRepoInfo != default
                ? Repository.Create(restoreMap.GetGitRestorePath(sourceRepoInfo.remote, sourceRepoInfo.branch, dependencyLock).path, sourceRepoInfo.branch, sourceRepoInfo.remote)
                : default;

            try
            {
                await Run(docsetPath, repository, locale, options, report, dependencyLock, restoreMap, sourceRepo);
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
            DependencyLockModel dependencyLock,
            RestoreMap restoreMap,
            Repository sourceRepo = null)
        {
            XrefMap xrefMap = null;
            var (configErrors, config) = GetBuildConfig(docsetPath, options, dependencyLock, locale, sourceRepo);
            report.Configure(docsetPath, config);

            // just return if config loading has errors
            if (report.Write(config.ConfigFileName, configErrors))
                return;

            var errors = new List<Error>();
            var docset = GetBuildDocset(new Docset(report, docsetPath, locale, config, options, dependencyLock, restoreMap, repository, sourceRepo));
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

                errors.AddIfNotNull(await saveGitHubUserCache);
                errors.ForEach(e => context.Report.Write(e));
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

        private static DependencyLockModel LoadBuildDependencyLock(
            string docset,
            string locale,
            CommandLineOptions commandLineOptions,
            (string remote, string branch) sourceRepo = default)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var (_, config) = ConfigLoader.TryLoad(docset, commandLineOptions);

            var dependencyLock = DependencyLock.Load(docset, string.IsNullOrEmpty(config.DependencyLock) ? new SourceInfo<string>(AppData.GetDependencyLockFile(docset, locale)) : config.DependencyLock);

            if (sourceRepo != default && !ConfigLoader.TryGetConfigPath(docset, out _))
            {
                // build from loc repo directly with overwrite config
                // which means it's using source repo's dependency lock
                var sourceDependencyLock = dependencyLock.GetGitLock(sourceRepo.remote, sourceRepo.branch);
                dependencyLock = sourceDependencyLock is null
                    ? null
                    : new DependencyLockModel
                    {
                        Commit = sourceDependencyLock.Commit,
                        Git = new Dictionary<string, DependencyLockModel>(sourceDependencyLock.Git.Concat(new[] { KeyValuePair.Create($"{sourceRepo.remote}#{sourceRepo.branch}", sourceDependencyLock) })),
                    };
            }

            return dependencyLock ?? new DependencyLockModel();
        }

        private static (List<Error> errors, Config config) GetBuildConfig(
            string docset,
            CommandLineOptions options,
            DependencyLockModel dependencyLock,
            string locale,
            Repository sourceRepo = null)
        {
            if (ConfigLoader.TryGetConfigPath(docset, out _) || sourceRepo is null)
            {
                return ConfigLoader.Load(docset, options, locale);
            }

            Debug.Assert(dependencyLock != null);
            return ConfigLoader.Load(sourceRepo.Path, options, locale);
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
