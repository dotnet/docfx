// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static (IEnumerable<Error>, object, PublishItem publishItem) Build(
            Context context,
            Document file,
            MonikerMap monikerMap)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);
            Debug.Assert(monikerMap != null);

            var (errors, model, refArticles, refTocs) = Load(context, file, monikerMap);
            var outputPath = file.GetOutputPath(model.Metadata.Monikers);

            if (file.Docset.Config.Output.Pdf)
            {
                var siteBasePath = file.Docset.Config.DocumentId.SiteBasePath;
                var relativePath = PathUtility.NormalizeFile(Path.GetRelativePath(siteBasePath, Path.ChangeExtension(outputPath, ".pdf")));
                model.Metadata.PdfAbsolutePath = $"/{siteBasePath}/opbuildpdf/{relativePath}";
            }

            var output = (object)model;
            if (file.Docset.Legacy)
            {
                output = file.Docset.Template.TransformMetadata("toc.json.js", JsonUtility.ToJObject(model));

                context.Output.WriteJson(model.Metadata, Path.ChangeExtension(outputPath, ".mta.json"));
            }

            // TODO: Add experimental and experiment_id to publish item
            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                Locale = file.Docset.Locale,
                Monikers = model.Metadata.Monikers,
            };

            return (errors, output, publishItem);
        }

        public static (
            List<Error> errors,
            TableOfContentsModel model,
            List<Document> referencedDocuments,
            List<Document> referencedTocs)

            Load(Context context, Document fileToBuild, MonikerMap monikerMap = null)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();
            var hrefMap = new Dictionary<string, List<string>>();

            // load toc model
            var (loadErrors, model) = TableOfContentsParser.Load(
                context,
                fileToBuild,
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
                    var (error, link, buildItem) = context.DependencyResolver.ResolveLink(href, file, resultRelativeTo, null);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                        if (!hrefMap.ContainsKey(link) && monikerMap != null && monikerMap.TryGetValue(buildItem, out var moniker))
                        {
                            hrefMap.Add(link, moniker);
                        }
                    }
                    return (link, buildItem);
                },
                (file, uid) =>
                {
                    // add to referenced document list
                    var (error, link, display, buildItem) = context.DependencyResolver.ResolveXref(uid, file, file);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                        if (!hrefMap.ContainsKey(link) && monikerMap != null && monikerMap.TryGetValue(buildItem, out var moniker))
                        {
                            hrefMap.Add(link, moniker);
                        }
                    }

                    return (link, display, buildItem);
                });

            errors.AddRange(loadErrors);

            // resolve monikers
            var (monikerError, fileMonikers) = context.MonikerProvider.GetFileLevelMonikers(fileToBuild, model.Metadata.MonikerRange);
            errors.AddIfNotNull(monikerError);

            model.Metadata.Monikers = fileMonikers;
            ResolveItemMonikers(model.Items);

            return (errors, model, referencedDocuments, referencedTocs);

            void ResolveItemMonikers(List<TableOfContentsItem> items)
            {
                foreach (var item in items)
                {
                    if (item.Items != null)
                    {
                        ResolveItemMonikers(item.Items);
                    }

                    List<string> monikers = null;
                    if (item.Href == null || !hrefMap.TryGetValue(item.Href, out monikers))
                    {
                        if (item.TopicHref == null || !hrefMap.TryGetValue(item.TopicHref, out monikers))
                        {
                            monikers = fileMonikers;
                        }
                    }

                    var childrenMonikers = item.Items?.SelectMany(child => child?.Monikers ?? new List<string>()) ?? new List<string>();
                    monikers = childrenMonikers.Union(monikers ?? new List<string>()).Distinct(context.MonikerProvider.Comparer).ToList();
                    monikers.Sort(context.MonikerProvider.Comparer);

                    item.Monikers = monikers;
                }
            }
        }
    }
}
