// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static (IEnumerable<Error>, TableOfContentsModel, DependencyMap, List<string>) Build(
            Context context, Document file, TableOfContentsMap tocMap)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            if (!tocMap.Contains(file))
            {
                return (Enumerable.Empty<Error>(), null, DependencyMap.Empty, new List<string>());
            }

            // todo: build resource files linked by toc
            var dependencyMapBuilder = new DependencyMapBuilder();
            var (errors, tocModel, tocMetadata, refArticles, refTocs, monikers) = Load(context, file, dependencyMapBuilder, true);

            var metadata = file.Docset.Metadata.GetMetadata(file, tocMetadata);
            metadata["monikers"] = new JArray(monikers);

            var model = new TableOfContentsModel
            {
                Items = tocModel,
                Metadata = metadata,
            };

            return (errors, model, dependencyMapBuilder.Build(), monikers);
        }

        public static TableOfContentsMap BuildTocMap(Context context, Docset docset)
        {
            using (Progress.Start("Loading TOC"))
            {
                var builder = new TableOfContentsMapBuilder();
                var tocFiles = docset.ScanScope.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return builder.Build();
                }

                ParallelUtility.ForEach(tocFiles, file => BuildTocMap(context, file, builder), Progress.Update);

                return builder.Build();
            }
        }

        private static void BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
        {
            try
            {
                Debug.Assert(tocMapBuilder != null);
                Debug.Assert(fileToBuild != null);

                var (errors, tocModel, _, referencedDocuments, referencedTocs, _) = Load(context, fileToBuild);

                tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report(fileToBuild.ToString(), dex.Error);
            }
        }

        private static (
            List<Error> errors,
            List<TableOfContentsItem> tocItems,
            JObject metadata,
            List<Document> referencedDocuments,
            List<Document> referencedTocs,
            List<string> monikers)

            Load(Context context, Document fileToBuild, DependencyMapBuilder dependencyMapBuilder = null, bool outputMonikers = false)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();

            var (loadErrors, tocItems, tocMetadata, monikers) = TableOfContentsParser.Load(
                context,
                fileToBuild,
                (file, href, isInclude) =>
                {
                    var (error, referencedTocContent, referencedToc) = file.TryResolveContent(href);
                    errors.AddIfNotNull(error);
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
                    errors.AddIfNotNull(error);

                    List<string> itemMonikers = new List<string>();
                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                        dependencyMapBuilder?.AddDependencyItem(file, buildItem, HrefUtility.FragmentToDependencyType(fragment));
                        if (outputMonikers)
                        {
                            itemMonikers = buildItem.Docset.MonikersProvider.GetFileMonikers(buildItem);
                        }
                    }
                    return (link, itemMonikers);
                });

            errors.AddRange(loadErrors);
            return (errors, tocItems, tocMetadata, referencedDocuments, referencedTocs, monikers);
        }
    }
}
