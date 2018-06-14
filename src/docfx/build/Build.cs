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
        public static async Task Run(string docsetPath, CommandLineOptions options, Reporter reporter)
        {
            var config = Config.Load(docsetPath, options);

            reporter.Configure(docsetPath, config);

            var outputPath = Path.Combine(docsetPath, config.Output.Path);
            var context = new Context(reporter, outputPath);
            var docset = new Docset(docsetPath, options);

            var buildScope = GlobFiles(context, docset);

            var tocMap = await BuildTableOfContents.BuildTocMap(context, buildScope);

            var (files, sourceDependencies) = await BuildFiles(context, buildScope, tocMap);

            BuildManifest.Build(context, files, sourceDependencies);

            BuildRedirections(docset, context);

            if (options.Legacy)
            {
                Legacy.ConvertToLegacyModel(docset, context, files, sourceDependencies, tocMap);
            }
        }

        private static HashSet<Document> GlobFiles(Context context, Docset docset)
        {
            return FileGlob.GetFiles(docset.DocsetPath, docset.Config.Content.Include, docset.Config.Content.Exclude)
                           .Select(file => Document.TryCreate(docset, Path.GetRelativePath(docset.DocsetPath, file)))
                           .ToHashSet();
        }

        private static async Task<(List<Document> files, DependencyMap sourceDependencies)> BuildFiles(Context context, HashSet<Document> buildScope, TableOfContentsMap tocMap)
        {
            var sourceDependencies = new ConcurrentDictionary<Document, List<DependencyItem>>();
            var publishConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>();
            var filesByUrl = new ConcurrentDictionary<string, Document>();

            await ParallelUtility.ForEach(buildScope, BuildTheFile, ShouldBuildTheFile);

            HandlePublishConflicts();

            return (
                filesByUrl.Values.OrderBy(d => d.OutputPath).ToList(),
                new DependencyMap(sourceDependencies.OrderBy(d => d.Key.FilePath).ToDictionary(k => k.Key, v => v.Value)));

            async Task BuildTheFile(Document file, Action<Document> buildChild)
            {
                var dependencyMap = await BuildFile(context, file, tocMap, buildChild);

                foreach (var (souce, dependencies) in dependencyMap)
                {
                    sourceDependencies.TryAdd(souce, dependencies);
                }
            }

            bool ShouldBuildTheFile(Document file)
            {
                if (!ShouldBuildFile(context, file, tocMap, buildScope))
                {
                    return false;
                }

                // Find publish URL conflicts
                if (!filesByUrl.TryAdd(file.SiteUrl, file))
                {
                    if (filesByUrl.TryGetValue(file.SiteUrl, out var publishedFile) && publishedFile != file)
                    {
                        publishConflicts.GetOrAdd(file.SiteUrl, _ => new ConcurrentBag<Document>()).Add(file);
                    }
                    return false;
                }

                return true;
            }

            void HandlePublishConflicts()
            {
                foreach (var (siteUrl, conflict) in publishConflicts)
                {
                    var conflictingFiles = new HashSet<Document>();

                    foreach (var conflictingFile in conflict)
                    {
                        conflictingFiles.Add(conflictingFile);
                    }

                    if (filesByUrl.TryRemove(siteUrl, out var removed))
                    {
                        conflictingFiles.Add(removed);
                    }

                    context.Report(Errors.PublishUrlConflict(siteUrl, conflictingFiles));

                    foreach (var conflictingFile in conflictingFiles)
                    {
                        context.Delete(conflictingFile.OutputPath);
                    }
                }
            }
        }

        private static Task<DependencyMap> BuildFile(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            switch (file.ContentType)
            {
                case ContentType.Asset:
                    return BuildAsset(context, file);
                case ContentType.Markdown:
                    return BuildMarkdown.Build(context, file, tocMap, buildChild);
                case ContentType.SchemaDocument:
                    return BuildSchemaDocument.Build(context, file, tocMap, buildChild);
                case ContentType.TableOfContents:
                    return BuildTableOfContents.Build(context, file, buildChild);
                default:
                    return Task.FromResult(DependencyMap.Empty);
            }
        }

        private static Task<DependencyMap> BuildAsset(Context context, Document file)
        {
            context.Copy(file, file.FilePath);
            return Task.FromResult(DependencyMap.Empty);
        }

        private static void BuildRedirections(Docset docset, Context context)
        {
            Parallel.ForEach(docset.Redirections, redirection =>
            {
                var model = new PageModel
                {
                    Content = "<p></p>",
                    RedirectionUrl = redirection.Value,
                    Locale = docset.Config.Locale,
                };

                var contentType = Document.GetContentType(redirection.Key);
                context.WriteJson(model, Document.GetSitePath(Document.GetSiteUrl(redirection.Key, contentType, docset.Config), contentType));
            });
        }

        /// <summary>
        /// All children will be built through this gate
        /// We control all the inclusion logic here:
        /// https://github.com/dotnet/docfx/issues/2755
        /// </summary>
        private static bool ShouldBuildFile(Context context, Document childToBuild, TableOfContentsMap tocMap, HashSet<Document> buildScope)
        {
            if (childToBuild.OutputPath == null)
            {
                return false;
            }

            if (childToBuild.ContentType == ContentType.Unknown)
            {
                return false;
            }

            if (childToBuild.ContentType == ContentType.TableOfContents && !tocMap.Contains(childToBuild))
            {
                return false;
            }

            // the `content` scope is fix, all the `content` children out of our glob scope will be treated as warnings
            if (childToBuild.ContentType == ContentType.Markdown || childToBuild.ContentType == ContentType.SchemaDocument)
            {
                if (!buildScope.Contains(childToBuild))
                {
                    // todo: report warnings
                    return false;
                }
            }

            return true;
        }
    }
}
