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

            var locale = LocalizationUtility.GetLocale(repository, options);
            var dependencyLock = LoadBuildDependencyLock(docsetPath, locale, repository, options);

            var restoreMap = RestoreMap.Create(dependencyLock);
            try
            {
                await Run(docsetPath, repository, locale, options, report, dependencyLock, restoreMap);
            }
            finally
            {
                restoreMap.Release();
            }
        }

        private static async Task Run(string docsetPath, Repository repository, string locale, CommandLineOptions options, Report report, DependencyLockModel dependencyLock, RestoreMap restoreMap)
        {
            XrefMap xrefMap = null;
            var (configErrors, config) = GetBuildConfig(docsetPath, repository, options, dependencyLock, restoreMap);
            report.Configure(docsetPath, config);

            // just return if config loading has errors
            if (report.Write(config.ConfigFileName, configErrors))
                return;

            var errors = new List<Error>();
            var docset = GetBuildDocset(new Docset(report, docsetPath, locale, config, options, dependencyLock, restoreMap, repository));
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

        private static DependencyLockModel LoadBuildDependencyLock(string docset, string locale, Repository repository, CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var (errors, config) = ConfigLoader.TryLoad(docset, commandLineOptions);

            var dependencyLock = DependencyLock.Load(docset, string.IsNullOrEmpty(config.DependencyLock) ? AppData.GetDependencyLockFile(docset, locale) : config.DependencyLock);

            if (LocalizationUtility.TryGetSourceRepository(repository, out var sourceRemote, out var sourceBranch, out _) && !ConfigLoader.TryGetConfigPath(docset, out _))
            {
                // build from loc repo directly with overwrite config
                // which means it's using source repo's dependency lock
                var sourceDependencyLock = dependencyLock.GetGitLock(sourceRemote, sourceBranch);
                dependencyLock = sourceDependencyLock is null
                    ? null
                    : new DependencyLockModel
                    {
                        Commit = sourceDependencyLock.Commit,
                        Git = new Dictionary<string, DependencyLockModel>(sourceDependencyLock.Git.Concat(new[] { KeyValuePair.Create($"{sourceRemote}#{sourceBranch}", sourceDependencyLock) })),
                    };
            }

            return dependencyLock ?? new DependencyLockModel();
        }

        private static (List<Error> errors, Config config) GetBuildConfig(string docset, Repository repository, CommandLineOptions options, DependencyLockModel dependencyLock, RestoreMap restoreMap)
        {
            if (ConfigLoader.TryGetConfigPath(docset, out _) || !LocalizationUtility.TryGetSourceRepository(repository, out var sourceRemote, out var sourceBranch, out var locale))
            {
                return ConfigLoader.Load(docset, options);
            }

            Debug.Assert(dependencyLock != null);
            var (sourceDocsetPath, _) = restoreMap.GetGitRestorePath(sourceRemote, sourceBranch, dependencyLock);
            return ConfigLoader.Load(sourceDocsetPath, options, locale);
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
