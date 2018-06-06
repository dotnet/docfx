// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static Task Build(Context context, Document file, Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            var (errors, tocModel, refArticles, refTocs) = Load(file);

            foreach (var article in refArticles)
            {
                buildChild(article);
            }

            context.Report(file, errors);
            context.WriteJson(new TableOfContentsModel { Items = tocModel }, file.OutputPath);

            return Task.CompletedTask;
        }

        public static async Task<TableOfContentsMap> BuildTocMap(Context context, List<Document> files)
        {
            Debug.Assert(files != null);

            var builder = new TableOfContentsMapBuilder();
            var tocFiles = files.Where(f => f.ContentType == ContentType.TableOfContents);
            if (!tocFiles.Any())
            {
                return builder.Build();
            }

            await ParallelUtility.ForEach(tocFiles, file => BuildTocMap(context, file, builder));

            return builder.Build();
        }

        private static Task BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
        {
            Debug.Assert(tocMapBuilder != null);
            Debug.Assert(fileToBuild != null);

            var (errors, tocModel, referencedDocuments, referencedTocs) = Load(fileToBuild);

            tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);

            return Task.CompletedTask;
        }

        private static (
            List<DocfxException> errors,
            List<TableOfContentsItem> tocModel,
            List<Document> referencedDocuments,
            List<Document> referencedTocs)

            Load(Document fileToBuild)
        {
            var errors = new List<DocfxException>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();
            var tocViewModel = TableOfContentsParser.Load(
                fileToBuild.ReadText(),
                fileToBuild.FilePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase),
                fileToBuild,
                (file, href) =>
                {
                    var (error, referencedTocContent, referencedTocPath) = file.TryResolveContent(href);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                    if (referencedTocPath != null)
                    {
                        // add to referenced toc list
                        referencedTocs.Add(referencedTocPath);
                    }
                    return (referencedTocContent, referencedTocPath);
                },
                (file, href, resultRelativeTo) =>
                {
                    // add to referenced document list
                    // only resolve href, no need to build
                    var (error, link, buildItem) = file.TryResolveHref(href, resultRelativeTo);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                    }
                    return link;
                });

            return (errors, tocViewModel, referencedDocuments, referencedTocs);
        }
    }
}
