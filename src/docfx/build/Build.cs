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
            context.Report("docfx.yml", configErrors);

            var docset = new Docset(context, docsetPath, config, options);

            var githubUserCache = await GitHubUserCache.Create(docset, options.GitHubToken);

            var contribution = new ContributionInfo(docset, githubUserCache);
            var tocMap = BuildTableOfContents.BuildTocMap(context, docset.BuildScope);

            var xrefMap = XrefMap.Create(context, docset);

            var (files, sourceDependencies) = await BuildFiles(context, docset.BuildScope, tocMap, xrefMap, contribution);

            // TODO: write back to global cache
            // var saveGitHubUserCache = githubUserCache.SaveChanges();
            BuildManifest.Build(context, files, sourceDependencies);

            if (options.Legacy)
            {
                if (config.Output.Json)
                {
                    Legacy.ConvertToLegacyModel(docset, context, files, sourceDependencies, tocMap, xrefMap);
                }
                else
                {
                    docset.LegacyTemplate.CopyTo(outputPath);
                }
            }

            // await saveGitHubUserCache;
            errors.ForEach(e => context.Report(e));
        }

        private static async Task<(List<Document> files, DependencyMap sourceDependencies)> BuildFiles(
            Context context,
            HashSet<Document> buildScope,
            TableOfContentsMap tocMap,
            XrefMap xrefMap,
            ContributionInfo contribution)
        {
            using (Progress.Start("Building files"))
            {
                var sourceDependencies = new ConcurrentDictionary<Document, List<DependencyItem>>();
                var bookmarkValidator = new BookmarkValidator();
                var filesBuilder = new DocumentListBuilder();
                var filesWithErrors = new ConcurrentBag<Document>();

                await ParallelUtility.ForEach(buildScope, BuildOneFile, ShouldBuildFile, Progress.Update);

                ValidateBookmarks();

                var files = filesBuilder.Build(context, filesWithErrors).OrderBy(file => file.FilePath).ToList();
                var allDependencies = sourceDependencies.OrderBy(d => d.Key.FilePath).ToDictionary(k => k.Key, v => v.Value);

                return (files, new DependencyMap(allDependencies));

                async Task BuildOneFile(Document file, Action<Document> buildChild)
                {
                    var (hasError, dependencyMap) = await BuildFile(context, file, xrefMap, tocMap, contribution, bookmarkValidator, buildChild);
                    if (hasError)
                    {
                        filesWithErrors.Add(file);
                    }
                    foreach (var (source, dependencies) in dependencyMap)
                    {
                        sourceDependencies.TryAdd(source, dependencies);
                    }
                }

                bool ShouldBuildFile(Document file)
                {
                    return file.ContentType != ContentType.Unknown && filesBuilder.TryAdd(file);
                }

                void ValidateBookmarks()
                {
                    foreach (var (error, file) in bookmarkValidator.Validate())
                    {
                        if (context.Report(error))
                        {
                            filesWithErrors.Add(file);
                        }
                    }
                }
            }
        }

        private static async Task<(bool hasError, DependencyMap)> BuildFile(
            Context context,
            Document file,
            XrefMap xrefMap,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild)
        {
            try
            {
                var model = (object)null;
                var dependencies = DependencyMap.Empty;
                var errors = Enumerable.Empty<Error>();

                switch (file.ContentType)
                {
                    case ContentType.Resource:
                        BuildResource(context, file);
                        return (false, DependencyMap.Empty);
                    case ContentType.Page:
                        (errors, model, dependencies) = await BuildPage.Build(context, file, tocMap, contribution, bookmarkValidator, buildChild, xrefMap);
                        break;
                    case ContentType.TableOfContents:
                        (errors, model, dependencies) = BuildTableOfContents.Build(context, file, tocMap, buildChild);
                        break;
                    case ContentType.Redirection:
                        model = BuildRedirection(file);
                        break;
                }

                var hasErrors = context.Report(file.ToString(), errors);
                if (model != null && !hasErrors)
                {
                    if (model is string str)
                        context.WriteText(str, file.OutputPath);
                    else
                        context.WriteJson(model, file.OutputPath);

                    return (false, dependencies);
                }

                return (true, dependencies);
            }
            catch (DocfxException ex)
            {
                context.Report(file.ToString(), ex.Error);
                return (true, DependencyMap.Empty);
            }
        }

        private static void BuildResource(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            if (file.Docset.Config.Output.CopyResources)
            {
                context.Copy(file, file.OutputPath);
            }
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
    }
}
