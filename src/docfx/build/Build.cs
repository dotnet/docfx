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
        public static async Task Run(string docsetPath, CommandLineOptions options, Reporter reporter)
        {
            var config = Config.Load(docsetPath, options);

            reporter.Configure(docsetPath, config);

            var outputPath = Path.Combine(docsetPath, config.Output.Path);
            var context = new Context(reporter, outputPath);
            var docset = new Docset(docsetPath, options);

            var glob = GlobFiles(docset);

            var tocMap = await BuildTableOfContents.BuildTocMap(glob);
            var repo = new GitRepoInfoProvider();

            var buildScope = new HashSet<Document>(glob.Concat(docset.Redirections.Keys));
            var (files, sourceDependencies) = await BuildFiles(context, buildScope, tocMap);

            BuildManifest.Build(context, files, sourceDependencies);

            if (options.Legacy)
            {
                Legacy.ConvertToLegacyModel(docset, context, files, sourceDependencies, tocMap, repo);
            }
        }

        private static List<Document> GlobFiles(Docset docset)
        {
            return FileGlob.GetFiles(docset.DocsetPath, docset.Config.Content.Include, docset.Config.Content.Exclude)
                           .Select(file => Document.TryCreateFromFile(docset, Path.GetRelativePath(docset.DocsetPath, file)))
                           .ToList();
        }

        private static async Task<(List<Document> files, DependencyMap sourceDependencies)> BuildFiles(
            Context context,
            HashSet<Document> buildScope,
            TableOfContentsMap tocMap)
        {
            var sourceDependencies = new ConcurrentDictionary<Document, List<DependencyItem>>();
            var fileListBuilder = new DocumentListBuilder();

            await ParallelUtility.ForEach(buildScope, BuildTheFile, ShouldBuildTheFile);

            return (fileListBuilder.Build(context), new DependencyMap(sourceDependencies.OrderBy(d => d.Key.FilePath).ToDictionary(k => k.Key, v => v.Value)));

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
                if (!ShouldBuildFile(file, tocMap, buildScope))
                {
                    return false;
                }

                return fileListBuilder.TryAdd(file);
            }
        }

        private static Task<DependencyMap> BuildFile(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            Action<Document> buildChild)
        {
            if (file.IsRedirection)
            {
                return BuildRedirectionItem(context, file);
            }

            switch (file.ContentType)
            {
                case ContentType.Asset:
                    return BuildAsset(context, file);
                case ContentType.Markdown:
                    return BuildMarkdown.Build(context, file, tocMap, buildChild);
                case ContentType.SchemaDocument:
                    return BuildSchemaDocument.Build();
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

        private static Task<DependencyMap> BuildRedirectionItem(Context context, Document file)
        {
            Debug.Assert(file.IsRedirection);

            var model = new PageModel
            {
                Content = "<p></p>",
                RedirectionUrl = file.Docset.Redirections[file],
                Locale = file.Docset.Config.Locale,
                DocumentId = file.Id.docId,
                VersionIndependentId = file.Id.versionIndependentId,
            };

            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(DependencyMap.Empty);
        }

        /// <summary>
        /// All children will be built through this gate
        /// We control all the inclusion logic here:
        /// https://github.com/dotnet/docfx/issues/2755
        /// </summary>
        private static bool ShouldBuildFile(Document childToBuild, TableOfContentsMap tocMap, HashSet<Document> buildScope)
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
