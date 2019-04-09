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
        public static (IEnumerable<Error>, PublishItem publishItem) Build(
            Context context,
            Document file,
            MonikerMap monikerMap)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);
            Debug.Assert(monikerMap != null);

            // load toc model
            var hrefMap = new Dictionary<string, List<string>>();
            var (errors, model, refArticles, refTocs) = context.Cache.LoadTocModel(context, file);
            foreach (var (doc, href) in refArticles)
            {
                if (!hrefMap.ContainsKey(href) && monikerMap.TryGetValue(doc, out var monikers))
                {
                    hrefMap[href] = monikers;
                }
            }

            // resolve monikers
            var (monikerError, fileMonikers) = context.MonikerProvider.GetFileLevelMonikers(file, model.Metadata.MonikerRange);
            errors.AddIfNotNull(monikerError);

            model.Metadata.Monikers = fileMonikers;
            ResolveItemMonikers(model.Items);

            // enable pdf
            var outputPath = file.GetOutputPath(model.Metadata.Monikers);

            if (file.Docset.Config.Output.Pdf)
            {
                var siteBasePath = file.Docset.Config.DocumentId.SiteBasePath;
                var relativePath = PathUtility.NormalizeFile(Path.GetRelativePath(siteBasePath, Path.ChangeExtension(outputPath, ".pdf")));
                model.Metadata.PdfAbsolutePath = $"/{siteBasePath}/opbuildpdf/{relativePath}";
            }

            // TODO: Add experimental and experiment_id to publish item
            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                Locale = file.Docset.Locale,
                Monikers = model.Metadata.Monikers,
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem))
            {
                if (file.Docset.Legacy)
                {
                    var output = context.Template.TransformMetadata("toc.json.js", JsonUtility.ToJObject(model));
                    publishItem.Hash = context.Output.WriteJsonWithHash(output, outputPath);
                    context.Output.WriteJson(model.Metadata, Path.ChangeExtension(outputPath, ".mta.json"));
                }
                else
                {
                    publishItem.Hash = context.Output.WriteJsonWithHash(model, outputPath);
                }
            }

            return (errors, publishItem);

            void ResolveItemMonikers(List<TableOfContentsItem> items)
            {
                foreach (var item in items)
                {
                    if (item.Items != null)
                    {
                        ResolveItemMonikers(item.Items);
                    }

                    List<string> monikers = null;
                    if (item.Href?.Value is null || !hrefMap.TryGetValue(item.Href, out monikers))
                    {
                        if (item.TopicHref is null || !hrefMap.TryGetValue(item.TopicHref, out monikers))
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

        public static (
            List<Error> errors,
            TableOfContentsModel model,
            List<(Document doc, string href)> referencedDocuments,
            List<Document> referencedTocs)

            Load(Context context, Document fileToBuild)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<(Document doc, string href)>();
            var referencedTocs = new List<Document>();
            var hrefMap = new Dictionary<string, List<string>>();

            // load toc model
            var (loadErrors, model) = TableOfContentsParser.Load(
                context,
                fileToBuild,
                (file, href, isInclude) =>
                {
                    var (error, referencedTocContent, referencedToc) = context.DependencyResolver.ResolveContent(href, file, href?.Range ?? default, DependencyType.TocInclusion);
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
                    var (error, link, buildItem) = context.DependencyResolver.ResolveLink(href, file, resultRelativeTo, null, href?.Range ?? default);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        // add to referenced document list
                        referencedDocuments.Add((buildItem, link));
                    }
                    return (link, buildItem);
                },
                (file, uid) =>
                {
                    // add to referenced document list
                    // TODO: pass line info into ResolveXref
                    var (error, link, display, buildItem) = context.DependencyResolver.ResolveXref(uid, file, file);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        referencedDocuments.Add((buildItem, link));
                    }

                    return (link, display, buildItem);
                });

            errors.AddRange(loadErrors);

            return (errors, model, referencedDocuments, referencedTocs);
        }
    }
}
