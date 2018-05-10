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

            reporter.OutputToFile(Path.Combine(docsetPath, config.Output.Path, config.Output.LogPath), config.Output.Stable);

            var context = new Context(reporter, Path.Combine(docsetPath, config.Output.Path), config.Output.Stable);
            var docset = new Docset(docsetPath, options);

            var globbedFiles = GlobFiles(context, docset);

            var tocMap = await BuildTableOfContents.BuildTocMap(context, globbedFiles);

            await BuildFiles(context, globbedFiles, tocMap);
        }

        private static List<Document> GlobFiles(Context context, Docset docset)
        {
            return FileGlob.GetFiles(docset.DocsetPath, docset.Config.Content.Include, docset.Config.Content.Exclude)
                           .Select(file => new Document(docset, Path.GetRelativePath(docset.DocsetPath, file)))
                           .ToList();
        }

        private static Task BuildFiles(Context context, List<Document> files, TableOfContentsMap tocMap)
        {
            var manifest = new ConcurrentDictionary<Document, byte>();
            var references = new ConcurrentDictionary<Document, byte>();

            return ParallelUtility.ForEach(
                files,
                (file, buildChild) =>
                {
                    if (!ShouldBuildFile(file, manifest, tocMap))
                    {
                        return Task.CompletedTask;
                    }

                    return BuildOneFile(context, file, tocMap, item =>
                    {
                        if (ShouldBuildFile(item, references, tocMap))
                        {
                            buildChild(item);
                        }
                    });
                });
        }

        private static Task BuildOneFile(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            switch (file.ContentType)
            {
                case ContentType.Asset:
                    return BuildAsset(context, file);
                case ContentType.Markdown:
                    return BuildMarkdown.Build(context, file, tocMap);
                case ContentType.SchemaDocument:
                    return BuildSchemaDocument.Build(context, file, tocMap);
                case ContentType.TableOfContents:
                    return BuildTableOfContents.Build(context, file, buildChild);
                default:
                    return Task.CompletedTask;
            }
        }

        private static Task BuildAsset(Context context, Document file)
        {
            context.Copy(file, file.FilePath);
            return Task.CompletedTask;
        }

        private static bool ShouldBuildFile(Document itemToBuild, ConcurrentDictionary<Document, byte> set, TableOfContentsMap tocMap)
        {
            if (itemToBuild.OutputPath == null)
            {
                return false;
            }

            if (itemToBuild.ContentType == ContentType.Unknown)
            {
                return false;
            }

            if (!set.TryAdd(itemToBuild, 0))
            {
                return false;
            }

            if (itemToBuild.ContentType == ContentType.TableOfContents &&
                !tocMap.Contains(itemToBuild))
            {
                return false;
            }

            return true;
        }
    }
}
