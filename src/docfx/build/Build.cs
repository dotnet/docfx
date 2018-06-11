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

            var globbedFiles = GlobFiles(context, docset);

            var tocMap = await BuildTableOfContents.BuildTocMap(context, globbedFiles);

            var (builtDocs, sourceDependencies) = await BuildFiles(context, globbedFiles, tocMap);

            BuildManifest.Build(context, builtDocs, sourceDependencies);

            if (options.Legacy)
            {
                var documents = builtDocs.ToList();
                Legacy.ConvertToLegacyModel(docset, context, documents);
            }
        }

        private static List<Document> GlobFiles(Context context, Docset docset)
        {
            return FileGlob.GetFiles(docset.DocsetPath, docset.Config.Content.Include, docset.Config.Content.Exclude)
                           .Select(file => Document.TryCreate(docset, Path.GetRelativePath(docset.DocsetPath, file)))
                           .ToList();
        }

        private static async Task<(List<Document> builtDocs, DependencyMap sourceDependencies)> BuildFiles(Context context, List<Document> files, TableOfContentsMap tocMap)
        {
            var builtDocs = new ConcurrentDictionary<Document, byte>();
            var references = new ConcurrentDictionary<Document, byte>();
            var sourceDependencies = new ConcurrentDictionary<Document, List<DependencyItem>>();
            var buildScope = new HashSet<Document>(files);

            await ParallelUtility.ForEach(
                files,
                async (file, buildChild) =>
                {
                    if (!ShouldBuildFile(context, file, tocMap, buildScope) || !builtDocs.TryAdd(file, 0))
                    {
                        return;
                    }

                    var dependencyMap = await BuildOneFile(context, file, tocMap, item =>
                    {
                        if (references.TryAdd(item, 0))
                        {
                            buildChild(item);
                        }
                    });

                    foreach (var (souce, dependencies) in dependencyMap)
                    {
                        sourceDependencies.TryAdd(souce, dependencies);
                    }
                });

            return (builtDocs.Keys.OrderBy(d => d.OutputPath).ToList(), new DependencyMap(sourceDependencies.OrderBy(d => d.Key.FilePath).ToDictionary(k => k.Key, v => v.Value)));
        }

        private static Task<DependencyMap> BuildOneFile(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
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
