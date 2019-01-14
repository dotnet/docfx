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
            XrefMap xrefMap = null;

            var errors = new List<Error>();

            // todo: abort the process if configuration loading has errors
            var (configErrors, config) = LocalizationUtility.GetBuildConfig(docsetPath, options);
            report.Configure(docsetPath, config);
            report.Write(config.ConfigFileName, configErrors);

            var localeToBuild = LocalizationUtility.GetBuildLocale(docsetPath, options);
            var docset = new Docset(report, docsetPath, localeToBuild, config, options).GetBuildDocset();
            var outputPath = Path.Combine(docsetPath, config.Output.Path);

            using (var context = await Context.Create(outputPath, report, docset, () => xrefMap))
            {
                xrefMap = XrefMap.Create(context, docset);

                var tocMap = BuildTableOfContents.BuildTocMap(context, docset);
                var (manifest, fileManifests, sourceDependencies) = await BuildFiles(context, docset, tocMap);

                context.Output.WriteJson(manifest, "build.manifest");
                var saveGitHubUserCache = context.GitHubUserCache.SaveChanges(config);
                xrefMap.OutputXrefMap(context);

                if (options.Legacy)
                {
                    if (config.Output.Json)
                    {
                        // TODO: decouple files and dependencies from legacy.
                        Legacy.ConvertToLegacyModel(docset, context, fileManifests, sourceDependencies, tocMap);
                    }
                    else
                    {
                        docset.LegacyTemplate.CopyTo(outputPath);
                    }
                }

                errors.AddIfNotNull(await saveGitHubUserCache);
                errors.ForEach(e => context.Report.Write(e));
            }
        }

        private static async Task<(Manifest, Dictionary<Document, FileManifest>, DependencyMap)> BuildFiles(
            Context context,
            Docset docset,
            TableOfContentsMap tocMap)
        {
            using (Progress.Start("Building files"))
            {
                var recurseDetector = new ConcurrentHashSet<Document>();
                var manifestBuilder = new ManifestBuilder();
                var monikerMap = new ConcurrentDictionary<Document, List<string>>();

                await ParallelUtility.ForEach(
                    docset.BuildScope,
                    async (file, buildChild) => { monikerMap.TryAdd(file, await BuildOneFile(file, buildChild, null)); },
                    (file) => { return ShouldBuildFile(file, new ContentType[] { ContentType.Page, ContentType.Redirection, ContentType.Resource }); },
                    Progress.Update);

                // Build TOC: since toc file depends on the build result of every node
                await ParallelUtility.ForEach(
                    docset.GetTableOfContentsScope(tocMap),
                    (file, buildChild) => { return BuildOneFile(file, buildChild, new MonikerMap(monikerMap)); },
                    ShouldBuildTocFile,
                    Progress.Update);

                var saveGitCommitCache = context.GitCommitProvider.SaveGitCommitCache();

                ValidateBookmarks();
                var manifest = manifestBuilder.Build(context);
                var dependencyMap = context.DependencyMapBuilder.Build();

                await saveGitCommitCache;

                return (CreateManifest(manifest, dependencyMap), manifest, dependencyMap);

                async Task<List<string>> BuildOneFile(
                    Document file,
                    Action<Document> buildChild,
                    MonikerMap fileMonikerMap)
                {
                    return await BuildFile(context, file, tocMap, fileMonikerMap, manifestBuilder, buildChild);
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
            MonikerMap monikerMap,
            ManifestBuilder manifestBuilder,
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
                        (errors, model, monikers) = BuildResource.Build(context, file);
                        break;
                    case ContentType.Page:
                        (errors, model, monikers) = await BuildPage.Build(context, file, tocMap, buildChild);
                        break;
                    case ContentType.TableOfContents:
                        // TODO: improve error message for toc monikers overlap
                        (errors, model, monikers) = BuildTableOfContents.Build(context, file, monikerMap);
                        break;
                    case ContentType.Redirection:
                        (errors, model, monikers) = BuildRedirection.Build(context, file);
                        break;
                }

                var hasErrors = context.Report.Write(file.ToString(), errors);
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
                            context.Output.Copy(file, manifest.OutputPath);
                        }
                    }
                    else if (model is string str)
                    {
                        context.Output.WriteText(str, manifest.OutputPath);
                    }
                    else
                    {
                        context.Output.WriteJson(model, manifest.OutputPath);
                    }
                }
                return monikers;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report.Write(file.ToString(), dex);
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
