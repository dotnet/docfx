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
        public static Task<DependencyMap> Build(Context context, Document file, Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            var dependencyMapBuilder = new DependencyMapBuilder();
            var (errors, tocModel, refArticles, refTocs) = Load(file);

            foreach (var (article, parent) in refArticles)
            {
                buildChild(article);

                dependencyMapBuilder.AddDependencyItem(parent, article, DependencyType.TocLink);
            }

            foreach (var (toc, parent) in refTocs)
            {
                // todo: handle folder referencing
                dependencyMapBuilder.AddDependencyItem(parent, toc, DependencyType.TocInclusion);
            }

            context.Report(file, errors);
            context.WriteJson(new TableOfContentsModel { Items = tocModel }, file.OutputPath);

            return Task.FromResult(dependencyMapBuilder.Build());
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

            await ParallelUtility.ForEach(tocFiles, file => BuildTocMap(file, builder));

            return builder.Build();
        }

        private static Task BuildTocMap(Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
        {
            Debug.Assert(tocMapBuilder != null);
            Debug.Assert(fileToBuild != null);

            var (errors, tocModel, referencedDocuments, referencedTocs) = Load(fileToBuild);

            tocMapBuilder.Add(fileToBuild, referencedDocuments.Select(r => r.doc), referencedTocs.Select(r => r.toc));

            return Task.CompletedTask;
        }

        private static (
            List<DocfxException> errors,
            List<TableOfContentsItem> tocModel,
            List<(Document doc, Document parent)> referencedDocuments,
            List<(Document toc, Document parent)> referencedTocs)

            Load(Document fileToBuild)
        {
            var errors = new List<DocfxException>();
            var referencedDocuments = new List<(Document doc, Document parent)>();
            var referencedTocs = new List<(Document toc, Document parent)>();
            var tocViewModel = TableOfContentsParser.Load(
                fileToBuild.ReadText(),
                fileToBuild.FilePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase),
                fileToBuild,
                (file, href, isInclude) =>
                {
                    var (error, referencedTocContent, referencedToc) = file.TryResolveContent(href);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                    if (referencedToc != null && isInclude)
                    {
                        // add to referenced toc list
                        referencedTocs.Add((referencedToc, file));
                    }
                    return (referencedTocContent, referencedToc);
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
                        referencedDocuments.Add((buildItem, file));
                    }
                    return link;
                });

            return (errors, tocViewModel, referencedDocuments, referencedTocs);
        }
    }
}
