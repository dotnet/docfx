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
            var repo = new GitRepoInfoProvider();

            var tocMap = await BuildTableOfContents.BuildTocMap(docset.BuildScope);

            var (files, sourceDependencies) = await BuildFiles(context, docset.BuildScope, tocMap);

            BuildManifest.Build(context, files, sourceDependencies);

            if (options.Legacy)
            {
                Legacy.ConvertToLegacyModel(docset, context, files, sourceDependencies, tocMap, repo);
            }
        }

        private static async Task<(List<Document> files, DependencyMap sourceDependencies)> BuildFiles(
            Context context,
            HashSet<Document> buildScope,
            TableOfContentsMap tocMap)
        {
            var sourceDependencies = new ConcurrentDictionary<Document, List<DependencyItem>>();
            var fileListBuilder = new DocumentListBuilder();

            await ParallelUtility.ForEach(buildScope, BuildOneFile, ShouldBuildFile);

            return (fileListBuilder.Build(context), new DependencyMap(sourceDependencies.OrderBy(d => d.Key.FilePath).ToDictionary(k => k.Key, v => v.Value)));

            async Task BuildOneFile(Document file, Action<Document> buildChild)
            {
                var dependencyMap = await BuildFile(context, file, tocMap, buildChild);

                foreach (var (souce, dependencies) in dependencyMap)
                {
                    sourceDependencies.TryAdd(souce, dependencies);
                }
            }

            bool ShouldBuildFile(Document file, bool dynamicAdd)
            {
                return file.ContentType != ContentType.Unknown && fileListBuilder.TryAdd(file);
            }
        }

        private static Task<DependencyMap> BuildFile(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            Action<Document> buildChild)
        {
            switch (file.ContentType)
            {
                case ContentType.Asset:
                    return BuildAsset(context, file);
                case ContentType.Markdown:
                    return BuildMarkdown.Build(context, file, tocMap, buildChild);
                case ContentType.SchemaDocument:
                    return BuildSchemaDocument.Build();
                case ContentType.TableOfContents:
                    return BuildTableOfContents.Build(context, file, tocMap, buildChild);
                case ContentType.Redirection:
                    return BuildRedirectionItem(context, file);
                default:
                    return Task.FromResult(DependencyMap.Empty);
            }
        }

        private static Task<DependencyMap> BuildAsset(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Asset);

            context.Copy(file, file.FilePath);
            return Task.FromResult(DependencyMap.Empty);
        }

        private static Task<DependencyMap> BuildRedirectionItem(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            var model = new PageModel
            {
                RedirectionUrl = file.Docset.Redirections[file],
                Locale = file.Docset.Config.Locale,
                Id = file.Id.docId,
                VersionIndependentId = file.Id.versionIndependentId,
            };

            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(DependencyMap.Empty);
        }
    }
}
