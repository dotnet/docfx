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
            MetadataProvider metadataProvider,
            MonikersProvider monikersProvider,
            DependencyResolver dependencyResolver,
            Dictionary<Document, List<string>> monikersMap)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);
            Debug.Assert(monikersMap != null);

            if (!tocMap.Contains(file))
            {
                return (Enumerable.Empty<Error>(), null, new List<string>());
            }

            var (errors, tocModel, tocMetadata, refArticles, refTocs) = Load(context, file, monikersProvider, dependencyResolver, monikersMap);

            var metadata = metadataProvider.GetMetadata(file, tocMetadata).ToObject<TableOfContentsMetadata>();

            Error monikerError;
            (monikerError, metadata.Monikers) = monikersProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            errors.AddIfNotNull(monikerError);

            var model = new TableOfContentsModel
            {
                Items = tocModel,
                Metadata = metadata,
            };

            return (errors, model, metadata.Monikers);
        }

        public static TableOfContentsMap BuildTocMap(Context context, Docset docset, MonikersProvider monikersProvider, DependencyResolver dependencyResolver)
        {
            using (Progress.Start("Loading TOC"))
            {
                var builder = new TableOfContentsMapBuilder();
                var tocFiles = docset.ScanScope.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return builder.Build();
                }

                ParallelUtility.ForEach(tocFiles, file => BuildTocMap(context, file, builder, monikersProvider, dependencyResolver), Progress.Update);

                return builder.Build();
            }
        }

        private static void BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder, MonikersProvider monikersProvider, DependencyResolver dependencyResolver)
        {
            try
            {
                Debug.Assert(tocMapBuilder != null);
                Debug.Assert(fileToBuild != null);

                var (errors, _, _, referencedDocuments, referencedTocs) = Load(context, fileToBuild, monikersProvider, dependencyResolver);
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
            DependencyResolver dependencyResolver,
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
                    var (error, referencedTocContent, referencedToc) = dependencyResolver.ResolveContent(href, file, DependencyType.TocInclusion);
                    errors.AddIfNotNull(error);
                    if (referencedToc != null && isInclude)
                    {
                        // add to referenced toc list
                        referencedTocs.Add(referencedToc);
                    }
                    return (referencedTocContent, referencedToc);
                },
                (file, href, resultRelativeTo) =>
                {
                    // add to referenced document list
                    var (error, link, buildItem) = dependencyResolver.ResolveLink(href, file, resultRelativeTo, null);
                    errors.AddIfNotNull(error);

                    var itemMonikers = new List<string>();
                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                        if (monikersMap == null || !monikersMap.TryGetValue(buildItem, out itemMonikers))
                        {
                            itemMonikers = new List<string>();
                        }
                    }
                    return (link, itemMonikers);
                },
                (file, uid) =>
                {
                    // add to referenced document list
                    var (error, link, _, buildItem) = dependencyResolver.ResolveXref(uid, file);
                    errors.AddIfNotNull(error);

                    var itemMonikers = new List<string>();
                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                        if (monikersMap == null || !monikersMap.TryGetValue(buildItem, out itemMonikers))
                        {
                            itemMonikers = new List<string>();
                        }
                    }

                    return (link, itemMonikers);
                },
                monikersProvider);

            errors.AddRange(loadErrors);
            return (errors, tocItems, tocMetadata, referencedDocuments, referencedTocs);
        }
    }
}
