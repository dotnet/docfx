// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static (IEnumerable<Error>, TableOfContentsModel, DependencyMap) Build(
            Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            if (!tocMap.Contains(file))
            {
                return (Enumerable.Empty<Error>(), null, DependencyMap.Empty);
            }

            var dependencyMapBuilder = new DependencyMapBuilder();
            var (errors, tocModel, tocMetadata, refArticles, refTocs) = Load(file, dependencyMapBuilder);

            foreach (var article in refArticles)
            {
                buildChild(article);
            }

            var model = new TableOfContentsModel { Items = tocModel, Metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), tocMetadata) };

            return (errors, model, dependencyMapBuilder.Build());
        }

        public static async Task<TableOfContentsMap> BuildTocMap(Context context, IEnumerable<Document> files)
        {
            using (Progress.Start("Loading TOC"))
            {
                Debug.Assert(files != null);

                var builder = new TableOfContentsMapBuilder();
                var tocFiles = files.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return builder.Build();
                }

                await ParallelUtility.ForEach(tocFiles, file => BuildTocMap(context, file, builder), Progress.Update);

                return builder.Build();
            }
        }

        private static Task BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
        {
            try
            {
                Debug.Assert(tocMapBuilder != null);
                Debug.Assert(fileToBuild != null);

                var (errors, tocModel, _, referencedDocuments, referencedTocs) = Load(fileToBuild);

                tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);

                return Task.CompletedTask;
            }
            catch (DocfxException ex)
            {
                context.Report(fileToBuild.ToString(), ex.Error);
                return Task.CompletedTask;
            }
        }

        private static (
            List<Error> errors,
            List<TableOfContentsItem> tocItems,
            JObject metadata,
            List<Document> referencedDocuments,
            List<Document> referencedTocs)

            Load(Document fileToBuild, DependencyMapBuilder dependencyMapBuilder = null)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();
            var (loadErrors, tocItems, tocMetadata) = TableOfContentsParser.Load(
                fileToBuild.ReadText(),
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
                        referencedTocs.Add(referencedToc);
                        dependencyMapBuilder?.AddDependencyItem(file, referencedToc, DependencyType.TocInclusion);
                    }
                    return (referencedTocContent, referencedToc);
                },
                (file, href, resultRelativeTo) =>
                {
                    // add to referenced document list
                    // only resolve href, no need to build
                    var (error, link, fragment, buildItem) = file.TryResolveHref(href, resultRelativeTo);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                        dependencyMapBuilder?.AddDependencyItem(file, buildItem, HrefUtility.FragmentToDependencyType(fragment));
                    }
                    return link;
                });

            errors.AddRange(loadErrors);
            return (errors, tocItems, tocMetadata, referencedDocuments, referencedTocs);
        }
    }
}
