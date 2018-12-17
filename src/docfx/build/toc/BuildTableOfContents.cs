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
        public static (IEnumerable<Error>, TableOfContentsModel, List<string> monikers) Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            GitCommitProvider gitCommitProvider,
            MetadataProvider metadataProvider,
            MonikersProvider monikersProvider,
            XrefMap xrefMap,
            DependencyMapBuilder dependencyMapBuilder,
            BookmarkValidator bookmarkValidator,
            Dictionary<Document, List<string>> monikersMap)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);
            Debug.Assert(monikersMap != null);

            if (!tocMap.Contains(file))
            {
                return (Enumerable.Empty<Error>(), null, new List<string>());
            }

            var (errors, tocModel, yamlHeader, refArticles, refTocs) = Load(context, file, monikersProvider, xrefMap, dependencyMapBuilder, gitCommitProvider, bookmarkValidator, monikersMap);

            var (metadataError, metadata) = metadataProvider.GetMetadata(file, yamlHeader);
            errors.AddIfNotNull(metadataError);

            Error monikerError;
            var tocMetadata = metadata.ToObject<TableOfContentsMetadata>();
            (monikerError, tocMetadata.Monikers) = monikersProvider.GetFileLevelMonikers(file, tocMetadata.MonikerRange);
            errors.AddIfNotNull(monikerError);

            var model = new TableOfContentsModel
            {
                Items = tocModel,
                Metadata = tocMetadata,
            };

            return (errors, model, tocMetadata.Monikers);
        }

        public static TableOfContentsMap BuildTocMap(Context context, Docset docset, MonikersProvider monikersProvider)
        {
            using (Progress.Start("Loading TOC"))
            {
                var builder = new TableOfContentsMapBuilder();
                var tocFiles = docset.ScanScope.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return builder.Build();
                }

                ParallelUtility.ForEach(tocFiles, file => BuildTocMap(context, file, builder, monikersProvider), Progress.Update);

                return builder.Build();
            }
        }

        private static void BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder, MonikersProvider monikersProvider)
        {
            try
            {
                Debug.Assert(tocMapBuilder != null);
                Debug.Assert(fileToBuild != null);

                var (errors, _, _, referencedDocuments, referencedTocs) = Load(context, fileToBuild, monikersProvider);
                context.Report(fileToBuild.ToString(), errors);

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
            List<Document> referencedTocs)

            Load(
            Context context,
            Document fileToBuild,
            MonikersProvider monikersProvider,
            XrefMap xrefMap = null,
            DependencyMapBuilder dependencyMapBuilder = null,
            GitCommitProvider gitCommitProvider = null,
            BookmarkValidator bookmarkValidator = null,
            Dictionary<Document, List<string>> monikersMap = null)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();

            var (loadErrors, tocItems, tocMetadata) = TableOfContentsParser.Load(
                context,
                fileToBuild,
                (file, href, isInclude) =>
                {
                    var (error, referencedTocContent, referencedToc) = file.TryResolveContent(href, gitCommitProvider);
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
                    var (error, link, fragment, buildItem) = file.TryResolveHref(href, resultRelativeTo, xrefMap, dependencyMapBuilder);
                    errors.AddIfNotNull(error);

                    var itemMonikers = new List<string>();
                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                        dependencyMapBuilder?.AddDependencyItem(file, buildItem, HrefUtility.FragmentToDependencyType(fragment));
                        if (monikersMap == null || !monikersMap.TryGetValue(buildItem, out itemMonikers))
                        {
                            itemMonikers = new List<string>();
                        }
                    }
                    bookmarkValidator?.AddBookmarkReference(file, buildItem ?? file, fragment);
                    return (link, itemMonikers);
                }, monikersProvider);

            errors.AddRange(loadErrors);
            return (errors, tocItems, tocMetadata, referencedDocuments, referencedTocs);
        }
    }
}
