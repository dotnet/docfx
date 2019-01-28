// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static (IEnumerable<Error>, TableOfContentsModel, PublishItem publishItem) Build(
            Context context,
            Document file,
            MonikerMap monikerMap)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);
            Debug.Assert(monikerMap != null);

            var (errors, tocModel, tocMetadata, refArticles, refTocs) = Load(context, file, monikerMap);

            var model = new TableOfContentsModel
            {
                Items = tocModel,
                Metadata = tocMetadata,
            };

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = file.GetOutputPath(tocMetadata.Monikers),
                Locale = file.Docset.Locale,
                Monikers = tocMetadata.Monikers,
            };

            return (errors, model, publishItem);
        }

        public static (List<Error> errors, TableOfContentsMap map) BuildTocMap(Context context, Docset docset)
        {
            using (Progress.Start("Loading TOC"))
            {
                var errors = new ConcurrentBag<Error>();
                var builder = new TableOfContentsMapBuilder();
                var tocFiles = docset.ScanScope.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return (errors.ToList(), builder.Build());
                }

                ParallelUtility.ForEach(
                    tocFiles,
                    file =>
                    {
                        var errorsPerFile = BuildTocMap(context, file, builder);
                        foreach (var errorPerFile in errorsPerFile)
                        {
                            errors.Add(errorPerFile.WithFile(file.ToString()));
                        }
                    },
                    Progress.Update);

                return (errors.ToList(), builder.Build());
            }
        }

        private static List<Error> BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
        {
            var errors = new List<Error>();
            try
            {
                Debug.Assert(tocMapBuilder != null);
                Debug.Assert(fileToBuild != null);

                var (loadErrors, _, _, referencedDocuments, referencedTocs) = Load(context, fileToBuild);
                errors.AddRange(loadErrors);

                tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.Add(dex.Error);
            }

            return errors;
        }

        private static (
            List<Error> errors,
            List<TableOfContentsItem> tocItems,
            TableOfContentsMetadata metadata,
            List<Document> referencedDocuments,
            List<Document> referencedTocs)

            Load(Context context, Document fileToBuild, MonikerMap monikerMap = null)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();

            var (loadErrors, tocItems, tocMetadata) = TableOfContentsParser.Load(
                context,
                fileToBuild,
                monikerMap,
                (file, href, isInclude) =>
                {
                    var (error, referencedTocContent, referencedToc) = context.DependencyResolver.ResolveContent(href, file, DependencyType.TocInclusion);
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
                    var (linkErrors, link, buildItem) = context.DependencyResolver.ResolveLink(href, file, resultRelativeTo, null);
                    errors.AddRange(linkErrors);

                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                    }
                    return (link, buildItem);
                },
                (file, uid) =>
                {
                    // add to referenced document list
                    var (xrefErrors, link, display, buildItem) = context.DependencyResolver.ResolveXref(uid, file, file);
                    errors.AddRange(xrefErrors);

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
