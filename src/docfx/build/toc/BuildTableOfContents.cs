// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                Locale = file.Docset.Locale,
                Monikers = model.Metadata.Monikers,
            };

            return (errors, output, publishItem);
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

                var (errors, _, referencedDocuments, referencedTocs) = Load(context, fileToBuild);
                context.Report.Write(fileToBuild.ToString(), errors);

                tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.Report.Write(fileToBuild.ToString(), dex.Error);
            }
        }

        private static (
            List<Error> errors,
            TableOfContentsModel model,
            List<Document> referencedDocuments,
            List<Document> referencedTocs)

            Load(Context context, Document fileToBuild, MonikerMap monikerMap = null)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();

            var (loadErrors, model) = TableOfContentsParser.Load(
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
                    var (error, link, buildItem) = context.DependencyResolver.ResolveLink(href, file, resultRelativeTo, null);
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
                    var (error, link, display, buildItem) = context.DependencyResolver.ResolveXref(uid, file, file);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        referencedDocuments.Add(buildItem);
                    }

                    return (link, display, buildItem);
                });

            errors.AddRange(loadErrors);

            return (errors, model, referencedDocuments, referencedTocs);
        }
    }
}
