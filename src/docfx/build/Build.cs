// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Build
    {
        public static async Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            var errors = new List<Error>();

            var (configErrors, config) = Config.Load(docsetPath, options);
            report.Configure(docsetPath, config);

            var outputPath = Path.Combine(docsetPath, config.Output.Path);
            var context = new Context(report, outputPath);
            context.Report(config.ConfigFileName, configErrors);

            var metadataProvider = new MetadataProvider(config);
            var docset = new Docset(context, docsetPath, config, options).GetBuildDocset();
            var monikersProvider = new MonikersProvider(docset);

            // TODO: toc map and xref map should always use source docset?
            var tocMap = BuildTableOfContents.BuildTocMap(context, docset, monikersProvider);
            var xrefMap = XrefMap.Create(context, docset, metadataProvider, monikersProvider);

            var githubUserCache = await GitHubUserCache.Create(docset, config.GitHub.AuthToken);
            var (manifest, fileManifests, sourceDependencies) = await BuildFiles(context, docset, tocMap, xrefMap, githubUserCache, metadataProvider, monikersProvider);

            context.WriteJson(manifest, "build.manifest");
            var saveGitHubUserCache = githubUserCache.SaveChanges(config);
            xrefMap.OutputXrefMap(context);

            if (options.Legacy)
            {
                if (config.Output.Json)
                {
                    // TODO: decouple files and dependencies from legacy.
                    Legacy.ConvertToLegacyModel(docset, context, fileManifests, sourceDependencies, tocMap, metadataProvider);
                }
                else
                {
                    docset.LegacyTemplate.CopyTo(outputPath);
                }
            }

            errors.AddIfNotNull(await saveGitHubUserCache);
            errors.ForEach(e => context.Report(e));
        }

        private static async Task<(Manifest, Dictionary<Document, FileManifest>, DependencyMap)> BuildFiles(
            Context context,
            Docset docset,
            TableOfContentsMap tocMap,
            XrefMap xrefMap,
            GitHubUserCache githubUserCache,
            MetadataProvider metadataProvider,
            MonikersProvider monikersProvider)
        {
            using (Progress.Start("Building files"))
            using (var gitCommitProvider = new GitCommitProvider())
            {
                var recurseDetector = new ConcurrentHashSet<Document>();
                var dependencyMapBuilder = new DependencyMapBuilder();
                var bookmarkValidator = new BookmarkValidator();
                var manifestBuilder = new ManifestBuilder();
                var monikersMap = new ConcurrentDictionary<Document, List<string>>();

                var contribution = await ContributionProvider.Create(docset, githubUserCache, gitCommitProvider);
                await ParallelUtility.ForEach(
                    docset.BuildScope,
                    async (file, buildChild) => { monikersMap.TryAdd(file, await BuildOneFile(file, buildChild, null)); },
                    (file) => { return ShouldBuildFile(file, new ContentType[] { ContentType.Page, ContentType.Redirection, ContentType.Resource }); },
                    Progress.Update);

                // Build TOC: since toc file depends on the build result of every node
                await ParallelUtility.ForEach(
                    docset.BuildScope,
                    (file, buildChild) => { return BuildOneFile(file, buildChild, monikersMap.ToDictionary(item => item.Key, item => item.Value)); },
                    file => { return ShouldBuildFile(file, new ContentType[] { ContentType.TableOfContents }); },
                    Progress.Update);

                var saveGitCommitCache = gitCommitProvider.SaveGitCommitCache();

                ValidateBookmarks();
                var manifest = manifestBuilder.Build(context);
                var dependencyMap = dependencyMapBuilder.Build();

                await saveGitCommitCache;

                return (CreateManifest(manifest, dependencyMap), manifest, dependencyMap);

                async Task<List<string>> BuildOneFile(
                    Document file,
                    Action<Document> buildChild,
                    Dictionary<Document, List<string>> fileMonikersMap)
                {
                    var callback = new PageCallback(xrefMap, dependencyMapBuilder, bookmarkValidator, buildChild);
                    return await BuildFile(context, file, tocMap, contribution, fileMonikersMap, callback, manifestBuilder, gitCommitProvider, metadataProvider, monikersProvider);
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

                void ValidateBookmarks()
                {
                    foreach (var (error, file) in bookmarkValidator.Validate())
                    {
                        if (context.Report(error))
                        {
                            manifestBuilder.MarkError(file);
                        }
                    }
                }
            }
        }

        private static async Task<List<string>> BuildFile(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            ContributionProvider contribution,
            Dictionary<Document, List<string>> monikersMap,
            PageCallback callback,
            ManifestBuilder manifestBuilder,
            GitCommitProvider gitCommitProvider,
            MetadataProvider metadataProvider,
            MonikersProvider monikersProvider)
        {
            try
            {
                var model = (object)null;
                var errors = Enumerable.Empty<Error>();
                var monikers = new List<string>();

                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        (errors, model, monikers) = BuildResource.Build(file, metadataProvider, monikersProvider);
                        break;
                    case ContentType.Page:
                        (errors, model, monikers) = await BuildPage.Build(context, file, tocMap, contribution, callback, gitCommitProvider, metadataProvider, monikersProvider);
                        break;
                    case ContentType.TableOfContents:
                        // TODO: improve error message for toc monikers overlap
                        (errors, model, monikers) = BuildTableOfContents.Build(context, file, tocMap, gitCommitProvider, metadataProvider, monikersProvider, callback.DependencyMapBuilder, callback.BookmarkValidator, monikersMap);
                        break;
                    case ContentType.Redirection:
                        (errors, model, monikers) = BuildRedirection.Build(file, metadataProvider, monikersProvider);
                        break;
                }

                var hasErrors = context.Report(file.ToString(), errors);
                if (hasErrors || model == null)
                {
                    manifestBuilder.MarkError(file);
                    return monikers;
                }

                var manifest = new FileManifest
                {
                    SourcePath = file.FilePath,
                    SiteUrl = file.SiteUrl,
                    Monikers = monikers,
                    OutputPath = GetOutputPath(file, monikers, monikersProvider),
                };

                if (manifestBuilder.TryAdd(file, manifest, monikers))
                {
                    if (model is ResourceModel copy)
                    {
                        if (file.Docset.Config.Output.CopyResources)
                        {
                            context.Copy(file, manifest.OutputPath);
                        }
                    }
                    else if (model is string str)
                    {
                        context.WriteText(str, manifest.OutputPath);
                    }
                    else
                    {
                        context.WriteJson(model, manifest.OutputPath);
                    }
                }
                return monikers;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report(file.ToString(), dex.Error);
                manifestBuilder.MarkError(file);
                return new List<string>();
            }
        }

        private static string GetOutputPath(Document file, List<string> monikers, MonikersProvider monikersProvider)
        {
            if (file.ContentType == ContentType.Resource && !file.Docset.Config.Output.CopyResources)
            {
                var docset = file.Docset;
                return PathUtility.NormalizeFile(
                    Path.GetRelativePath(
                        Path.GetFullPath(Path.Combine(docset.DocsetPath, docset.Config.Output.Path)),
                        Path.GetFullPath(Path.Combine(docset.DocsetPath, file.FilePath))));
            }

            return PathUtility.NormalizeFile(Path.Combine(
                $"{monikersProvider.GetGroupIdFromMonikerList(monikers)}",
                file.SitePath));
        }

        private static Manifest CreateManifest(Dictionary<Document, FileManifest> files, DependencyMap dependencies)
        {
            return new Manifest
            {
                Files = files.Values.OrderBy(item => item.SourcePath).ToArray(),

                Dependencies = dependencies.ToDictionary(
                           d => d.Key.FilePath,
                           d => d.Value.Select(v =>
                           new DependencyManifestItem
                           {
                               Source = v.Dest.FilePath,
                               Type = v.Type,
                           }).ToArray()),
            };
        }
    }
}
