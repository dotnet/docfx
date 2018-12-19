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
            MonikerProvider monikerProvider,
            DependencyResolver dependencyResolver,
            MonikerMap monikerMap,
            List<Document> callStack)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);
            Debug.Assert(monikerMap != null);

            if (!tocMap.Contains(file))
            {
                return (Enumerable.Empty<Error>(), null, new List<string>());
            }

            var (errors, tocModel, tocMetadata, refArticles, refTocs) = Load(context, file, dependencyResolver, callStack, monikerMap, monikerProvider.Comparer);

            var metadata = metadataProvider.GetMetadata(file, tocMetadata).ToObject<TableOfContentsMetadata>();

            Error monikerError;
            (monikerError, metadata.Monikers) = monikerProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            errors.AddIfNotNull(monikerError);

            var model = new TableOfContentsModel
            {
                Items = tocModel,
                Metadata = metadata,
            };

            return (errors, model, metadata.Monikers);
        }

        public static TableOfContentsMap BuildTocMap(Context context, Docset docset, DependencyResolver dependencyResolver, List<Document> callStack)
        {
            using (Progress.Start("Loading TOC"))
            {
                var builder = new TableOfContentsMapBuilder();
                var tocFiles = docset.ScanScope.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return builder.Build();
                }

                ParallelUtility.ForEach(tocFiles, file => BuildTocMap(context, file, builder, dependencyResolver, callStack), Progress.Update);

                return builder.Build();
            }
        }

        private static void BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder, DependencyResolver dependencyResolver, List<Document> callStack)
        {
            try
            {
                Debug.Assert(tocMapBuilder != null);
                Debug.Assert(fileToBuild != null);

                var (errors, _, _, referencedDocuments, referencedTocs) = Load(context, fileToBuild, dependencyResolver, callStack);
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
            DependencyResolver dependencyResolver,
            List<Document> callStack,
            MonikerMap monikerMap = null,
            MonikerComparer monikerComparer = null)
        {
            Debug.Assert(!(monikerMap == null ^ monikerComparer == null));

            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();

            var (loadErrors, tocItems, tocMetadata) = TableOfContentsParser.Load(
                context,
                fileToBuild,
                monikerComparer,
                monikerMap,
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
                    var (error, link, buildItem) = dependencyResolver.ResolveLink(href, file, resultRelativeTo, null, callStack);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                    }
                    return (link, buildItem);
                },
                (file, uid) =>
                {
                    // add to referenced document list
                    var (error, link, display, buildItem) = dependencyResolver.ResolveXref(uid, file, callStack);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                    }

                    return (link, display, buildItem);
                });

            errors.AddRange(loadErrors);
            return (errors, tocItems, tocMetadata, referencedDocuments, referencedTocs);
        }
    }
}
