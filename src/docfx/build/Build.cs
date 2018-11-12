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
            var errors = new List<Error>();

            var (configErrors, config) = Config.Load(docsetPath, options);
            report.Configure(docsetPath, config);

            var outputPath = Path.Combine(docsetPath, config.Output.Path);
            var context = new Context(report, outputPath);
            context.Report(config.ConfigFileName, configErrors);

            var docset = GetBuildDocset();

            var githubUserCache = await GitHubUserCache.Create(docset, config.GitHub.AuthToken);
            var contribution = await ContributionProvider.Create(docset, githubUserCache);

            // TODO: toc map and xref map should always use source docset?
            var tocMap = BuildTableOfContents.BuildTocMap(context, docset);
            var xrefMap = XrefMap.Create(context, docset);

            var (manifest, files, sourceDependencies) = await BuildFiles(context, docset, tocMap, xrefMap, contribution);

            context.WriteJson(manifest, "build.manifest");

            // TODO: write back to global cache
            var saveGitHubUserCache = githubUserCache.SaveChanges();

            xrefMap.OutputXrefMap(context);

            if (options.Legacy)
            {
                if (config.Output.Json)
                {
                    // TODO: decouple files and dependencies from legacy.
                    Legacy.ConvertToLegacyModel(docset, context, files, sourceDependencies, tocMap, xrefMap);
                }
                else
                {
                    docset.LegacyTemplate.CopyTo(outputPath);
                }
            }

            await saveGitHubUserCache;
            errors.ForEach(e => context.Report(e));

            Docset GetBuildDocset()
            {
                var sourceDocset = new Docset(context, docsetPath, config, options);
                return sourceDocset.LocalizationDocset ?? sourceDocset;
            }
        }

        private static async Task<(Manifest, List<Document>, DependencyMap)> BuildFiles(
            Context context,
            Docset docset,
            TableOfContentsMap tocMap,
            XrefMap xrefMap,
            ContributionProvider contribution)
        {
            using (Progress.Start("Building files"))
            {
                var recurseDetector = new ConcurrentHashSet<Document>();
                var sourceDependencies = new ConcurrentDictionary<Document, List<DependencyItem>>();
                var bookmarkValidator = new BookmarkValidator();
                var manifestBuilder = new ManifestBuilder();

                await ParallelUtility.ForEach(docset.BuildScope, BuildOneFile, ShouldBuildFile, Progress.Update);

                ValidateBookmarks();

                var (manifest, files) = manifestBuilder.Build(context);
                var allDependencies = sourceDependencies.OrderBy(d => d.Key.FilePath).ToDictionary(k => k.Key, v => v.Value);
                var allDependencyMap = new DependencyMap(allDependencies);

                return (CreateManifest(manifest, allDependencyMap), files, allDependencyMap);

                async Task BuildOneFile(Document file, Action<Document> buildChild)
                {
                    var dependencyMapBuilder = new DependencyMapBuilder();
                    var callback = new PageCallback(xrefMap, dependencyMapBuilder, bookmarkValidator, buildChild);
                    var dependencyMap = await BuildFile(context, file, tocMap, contribution, callback, manifestBuilder);

                    foreach (var (source, dependencies) in dependencyMap)
                    {
                        sourceDependencies.TryAdd(source, dependencies);
                    }
                }

                bool ShouldBuildFile(Document file)
                {
                    // source content in a localization docset
                    if (docset.FallbackDocset != null && file.Docset.LocalizationDocset != null)
                    {
                        return false;
                    }

                    // loc redirection files
                    if (docset.FallbackDocset != null && file.Docset.FallbackDocset != null && file.ContentType == ContentType.Redirection)
                    {
                        return false;
                    }

                    return file.ContentType != ContentType.Unknown && recurseDetector.TryAdd(file);
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

        private static async Task<DependencyMap> BuildFile(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            ContributionProvider contribution,
            PageCallback callback,
            ManifestBuilder manifestBuilder)
        {
            try
            {
                var model = (object)null;
                var dependencies = DependencyMap.Empty;
                var errors = Enumerable.Empty<Error>();
                var monikers = new List<string>();

                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        model = BuildResource(file);
                        break;
                    case ContentType.Page:
                        (errors, model, dependencies, monikers) = await BuildPage.Build(context, file, tocMap, contribution, callback);
                        break;
                    case ContentType.TableOfContents:
                        (errors, model, dependencies) = BuildTableOfContents.Build(context, file, tocMap);
                        break;
                    case ContentType.Redirection:
                        model = BuildRedirection(file);
                        break;
                }

                var hasErrors = context.Report(file.ToString(), errors);
                if (hasErrors || model == null)
                {
                    manifestBuilder.MarkError(file);
                    return DependencyMap.Empty;
                }

                var manifest = new FileManifest
                {
                    SourcePath = file.FilePath,
                    SiteUrl = file.SiteUrl,
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
                return dependencies;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report(file.ToString(), dex.Error);
                manifestBuilder.MarkError(file);
                return DependencyMap.Empty;
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

            var outputPath = file.SitePath;
            if (monikers.Count != 0)
            {
                var monikerSeg = HashUtility.GetMd5HashShort(string.Join(',', monikers));
                outputPath = PathUtility.NormalizeFile(Path.Combine(monikerSeg, file.SitePath));
            }
            return outputPath;
        }

        private static ResourceModel BuildResource(Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            return new ResourceModel { Locale = file.Docset.Locale };
        }

        private static RedirectionModel BuildRedirection(Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            return new RedirectionModel
            {
                RedirectUrl = file.RedirectionUrl,
                Locale = file.Docset.Locale,
            };
        }

        private static Manifest CreateManifest(FileManifest[] files, DependencyMap dependencies)
        {
            return new Manifest
            {
                Files = files,

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
