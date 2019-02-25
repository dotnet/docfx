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
            var gits = new List<DependencyGit>();

            try
            {
                await RunWithGit();
            }
            finally
            {
                if (gits != null)
                {
                    foreach (var git in gits)
                    {
                        await DependencySlotPool.ReleaseSlot(git, LockType.Shared);
                    }
                }
            }

            async Task RunWithGit()
            {
                XrefMap xrefMap = null;
                var repository = Repository.Create(docsetPath);
                Telemetry.SetRepository(repository?.Remote, repository?.Branch);

                var locale = LocalizationUtility.GetLocale(repository, options);
                var dependencyLock = await LoadBuildDependencyLock(docsetPath, locale, repository, options);
                var (configErrors, config, sourceGit) = await GetBuildConfig(docsetPath, repository, options, dependencyLock);
                gits.AddIfNotNull(sourceGit);
                report.Configure(docsetPath, config);

                // just return if config loading has errors
                if (report.Write(config.ConfigFileName, configErrors))
                    return;

                var errors = new List<Error>();

                var (entryDocset, dependencyGits) = await Docset.Create(report, docsetPath, locale, config, options, dependencyLock, repository);
                gits.AddRange(dependencyGits);

                var docset = GetBuildDocset(entryDocset);
                var outputPath = Path.Combine(docsetPath, config.Output.Path);

                using (var context = await Context.Create(outputPath, report, docset, () => xrefMap))
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
                            docset.Template.CopyTo(outputPath);
                        }
                    }

                    errors.AddIfNotNull(await saveGitHubUserCache);
                    errors.ForEach(e => context.Report.Write(e));
                }
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

                var saveGitCommitCache = context.GitCommitProvider.SaveGitCommitCache();

                ValidateBookmarks();

                var (publishModel, fileManifests) = context.PublishModelBuilder.Build(context);
                var dependencyMap = context.DependencyMapBuilder.Build();

                await saveGitCommitCache;

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
                        if (context.Report.Write(error))
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
                var model = default(object);
                var publishItem = default(PublishItem);
                var errors = Enumerable.Empty<Error>();

                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        (errors, publishItem) = BuildResource.Build(context, file);
                        break;
                    case ContentType.Page:
                        (errors, model, publishItem) = await BuildPage.Build(context, file, tocMap, buildChild);
                        break;
                    case ContentType.TableOfContents:
                        // TODO: improve error message for toc monikers overlap
                        (errors, model, publishItem) = BuildTableOfContents.Build(context, file, monikerMap);
                        break;
                    case ContentType.Redirection:
                        (errors, publishItem) = BuildRedirection.Build(context, file);
                        break;
                }

                var hasErrors = context.Report.Write(file.ToString(), errors);
                if (hasErrors)
                {
                    context.PublishModelBuilder.MarkError(file);
                    return publishItem.Monikers;
                }

                if (context.PublishModelBuilder.TryAdd(file, publishItem))
                {
                    if (model is string str)
                    {
                        publishItem.Hash = context.Output.WriteTextWithHash(str, publishItem.Path);
                    }
                    else if (model != null)
                    {
                        publishItem.Hash = context.Output.WriteJsonWithHash(model, publishItem.Path);
                    }
                }

                return publishItem.Monikers;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report.Write(file.ToString(), dex.Error);
                context.PublishModelBuilder.MarkError(file);
                return new List<string>();
            }
        }

        private static async Task<DependencyLockModel> LoadBuildDependencyLock(string docset, string locale, Repository repository, CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var (errors, config) = ConfigLoader.TryLoad(docset, commandLineOptions);

            var dependencyLock = await DependencyLock.Load(docset, string.IsNullOrEmpty(config.DependencyLock) ? AppData.GetDependencyLockFile(docset, locale) : config.DependencyLock);

            if (LocalizationUtility.TryGetSourceRepositoryInfo(repository, out var sourceRemote, out var sourceBranch, out _) && !ConfigLoader.TryGetConfigPath(docset, out _))
            {
                // build from loc repo directly with overwrite config
                // which means it's using source repo's dependency lock
                var sourceDependencyLock = dependencyLock.GetGitLock(sourceRemote, sourceBranch);
                dependencyLock = sourceDependencyLock == null
                    ? null
                    : new DependencyLockModel
                    {
                        Downloads = sourceDependencyLock.Downloads,
                        Hash = sourceDependencyLock.Hash,
                        Commit = sourceDependencyLock.Commit,
                        Git = new Dictionary<string, DependencyLockModel>(sourceDependencyLock.Git.Concat(new[] { KeyValuePair.Create($"{sourceRemote}#{sourceBranch}", sourceDependencyLock) })),
                    };
            }

            return dependencyLock ?? new DependencyLockModel();
        }

        private static async Task<(List<Error> errors, Config config, DependencyGit sourceGit)> GetBuildConfig(string docset, Repository repository, CommandLineOptions options, DependencyLockModel dependencyLock)
        {
            if (ConfigLoader.TryGetConfigPath(docset, out _) || !LocalizationUtility.TryGetSourceRepositoryInfo(repository, out var sourceRemote, out var sourceBranch, out var locale))
            {
                var (errors, config) = ConfigLoader.Load(docset, options);
                return (errors, config, null);
            }

            Debug.Assert(dependencyLock != null);
            var (sourceDocsetPath, _, sourceGit) = await DependencyGitPool.AcquireSharedGit(sourceRemote, sourceBranch, dependencyLock);
            var (sourceErrors, sourceConfig) = ConfigLoader.Load(sourceDocsetPath, options, locale);
            return (sourceErrors, sourceConfig, sourceGit);
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
