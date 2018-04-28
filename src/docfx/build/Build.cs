// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Build
    {
        public static async Task Run(string docsetPath, CommandLineOptions options, ILog log)
        {
            var config = Config.Load(docsetPath, options);
            var context = new Context(log, Path.Combine(docsetPath, config.Output.Path), config.Output.Stable);
            var docset = new Docset(docsetPath, options);

            var globbedFiles = GlobFiles(context, docset);

            var tocMap = await BuildTableOfContents.BuildTocMap(context, globbedFiles);

            await BuildFiles(context, globbedFiles, tocMap);
        }

        private static List<Document> GlobFiles(Context context, Docset docset)
        {
            return FileGlob.GetFiles(docset.DocsetPath, docset.Config.Files.Include, docset.Config.Files.Exclude)
                           .Select(file => new Document(docset, Path.GetRelativePath(docset.DocsetPath, file)))
                           .ToList();
        }

        private static Task BuildFiles(Context context, List<Document> files, TableOfContentsMap tocMap)
        {
            return ParallelUtility.ForEach(files, file => BuildOneFile(context, file, tocMap));
        }

        private static Task BuildOneFile(Context context, Document file, TableOfContentsMap tocMap)
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
                    return BuildTableOfContents.Build(context, file);
                default:
                    return Task.CompletedTask;
            }
        }

        private static Task BuildAsset(Context context, Document file)
        {
            context.Copy(file, file.FilePath);
            return Task.CompletedTask;
        }
    }
}
