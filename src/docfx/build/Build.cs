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

            // todo: abort the process if configuration loading has errors
            var outputPath = Path.Combine(docsetPath, config.Output.Path);
            var context = new Context(report, outputPath);
            context.Report(config.ConfigFileName, configErrors);

            var metadataProvider = new MetadataProvider(config);
            var docset = new Docset(context, docsetPath, config, options).GetBuildDocset();
            var monikerProvider = new MonikerProvider(docset);

            using (var gitCommitProvider = new GitCommitProvider())
            {
                XrefMap xrefMap = null;

                var dependencyResolver = new DependencyResolver(gitCommitProvider, new Lazy<XrefMap>(() => xrefMap));

                // Xrefmap and dependency resolver has a circular dependency.
                xrefMap = XrefMap.Create(context, docset, metadataProvider, monikerProvider, dependencyResolver);
                var tocMap = BuildTableOfContents.BuildTocMap(context, docset, dependencyResolver);

                var githubUserCache = await GitHubUserCache.Create(docset, config.GitHub.AuthToken);
                var (manifest, fileManifests, sourceDependencies) = await BuildFiles(context, docset, tocMap, githubUserCache, metadataProvider, monikerProvider, dependencyResolver, gitCommitProvider);

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
        }

        private static async Task<(Manifest, Dictionary<Document, FileManifest>, DependencyMap)> BuildFiles(
            Context context,
            Docset docset,
            TableOfContentsMap tocMap,
            GitHubUserCache githubUserCache,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            DependencyResolver dependencyResolver,
            GitCommitProvider gitCommitProvider)
        {
            using (Progress.Start("Building files"))
            {
                var recurseDetector = new ConcurrentHashSet<Document>();
                var manifestBuilder = new ManifestBuilder();
                var monikerMap = new ConcurrentDictionary<Document, List<string>>();

                var contribution = await ContributionProvider.Create(docset, githubUserCache, gitCommitProvider);
                await ParallelUtility.ForEach(
                    docset.BuildScope,
                    async (file, buildChild) => { monikerMap.TryAdd(file, await BuildOneFile(file, buildChild, null)); },
                    (file) => { return ShouldBuildFile(file, new ContentType[] { ContentType.Page, ContentType.Redirection, ContentType.Resource }); },
                    Progress.Update);

                // Build TOC: since toc file depends on the build result of every node
                await ParallelUtility.ForEach(
                    docset.GetTableOfContents(tocMap),
                    (file, buildChild) => { return BuildOneFile(file, buildChild, new MonikerMap(monikerMap)); },
                    ShouldBuildTocFile,
                    Progress.Update);

                var saveGitCommitCache = gitCommitProvider.SaveGitCommitCache();

                ValidateBookmarks();
                var manifest = manifestBuilder.Build(context);
                var dependencyMap = dependencyResolver.DependencyMapBuilder.Build();

                await saveGitCommitCache;

                return (CreateManifest(manifest, dependencyMap), manifest, dependencyMap);

                async Task<List<string>> BuildOneFile(
                    Document file,
                    Action<Document> buildChild,
                    MonikerMap fileMonikerMap)
                {
                    return await BuildFile(context, file, tocMap, contribution, fileMonikerMap, manifestBuilder, metadataProvider, monikerProvider, dependencyResolver, buildChild);
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
                    foreach (var (error, file) in dependencyResolver.BookmarkValidator.Validate())
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
            MonikerMap monikerMap,
            ManifestBuilder manifestBuilder,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            DependencyResolver dependencyResolver,
            Action<Document> buildChild)
        {
            try
            {
                var model = (object)null;
                var errors = Enumerable.Empty<Error>();
                var monikers = new List<string>();

                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        (errors, model, monikers) = BuildResource.Build(file, metadataProvider, monikerProvider);
                        break;
                    case ContentType.Page:
                        (errors, model, monikers) = await BuildPage.Build(context, file, tocMap, contribution, metadataProvider, monikerProvider, dependencyResolver, buildChild);
                        break;
                    case ContentType.TableOfContents:
                        // TODO: improve error message for toc monikers overlap
                        (errors, model, monikers) = BuildTableOfContents.Build(context, file, metadataProvider, monikerProvider, dependencyResolver, monikerMap);
                        break;
                    case ContentType.Redirection:
                        (errors, model, monikers) = BuildRedirection.Build(file, metadataProvider, monikerProvider);
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
                    OutputPath = GetOutputPath(file, monikers),
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

        private static string GetOutputPath(Document file, List<string> monikers)
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
                $"{HashUtility.GetMd5HashShort(monikers)}",
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
