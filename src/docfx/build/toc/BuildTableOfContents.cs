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
        public static IEnumerable<Error> Build(Context context, Document file, TableOfContentsMap tocMap)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            if (!tocMap.Contains(file))
            {
                return Array.Empty<Error>();
            }

            // if A toc includes B toc and only B toc is localized, then A need to be included and built
            if (file.Docset.IsFallback() && !ReferencesLocalizedToc(file, tocMap))
            {
                return Array.Empty<Error>();
            }

            // load toc model
            var (errors, model, _, _) = context.Cache.LoadTocModel(context, file);

            // enable pdf
            var outputPath = file.GetOutputPath(model.Metadata.Monikers, file.Docset.SiteBasePath);

            if (file.Docset.Config.Output.Pdf)
            {
                var siteBasePath = file.Docset.SiteBasePath;
                var relativePath = PathUtility.NormalizeFile(Path.GetRelativePath(siteBasePath, LegacyUtility.ChangeExtension(outputPath, ".pdf")));
                model.Metadata.PdfAbsolutePath = $"/{siteBasePath}/opbuildpdf/{relativePath}";
            }

            // TODO: Add experimental and experiment_id to publish item
            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath,
                Locale = file.Docset.Locale,
                Monikers = model.Metadata.Monikers,
                MonikerGroup = MonikerUtility.GetGroup(model.Metadata.Monikers),
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem))
            {
                if (file.Docset.Legacy)
                {
                    var output = context.Template.TransformTocMetadata(JsonUtility.ToJObject(model));
                    context.Output.WriteJson(output, outputPath);
                    context.Output.WriteJson(model.Metadata, LegacyUtility.ChangeExtension(outputPath, ".mta.json"));
                }
                else
                {
                    context.Output.WriteJson(model, outputPath);
                }
            }

            return errors;
        }

        public static (
            List<Error> errors,
            TableOfContentsModel model,
            List<Document> referencedDocuments,
            List<Document> referencedTocs)

            Load(Context context, Document fileToBuild)
        {
            var errors = new List<Error>();
            var referencedDocuments = new List<Document>();
            var referencedTocs = new List<Document>();

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
                (file, href, relativeToFile) =>
                {
                    var (error, link, buildItem) = context.DependencyResolver.ResolveRelativeLink(relativeToFile, href, file);
                    errors.AddIfNotNull(error);

                    if (buildItem != null)
                    {
                        // add to referenced document list
                        referencedDocuments.Add(buildItem);
                    }
                    return (link, buildItem);
                },
                (file, uid) =>
                {
                    // add to referenced document list
                    // TODO: pass line info into ResolveXref
                    var (error, link, display, xrefSpec) = context.DependencyResolver.ResolveRelativeXref(file, uid, file);
                    errors.AddIfNotNull(error);

                    if (xrefSpec?.DeclairingFile != null)
                    {
                        referencedDocuments.Add(xrefSpec?.DeclairingFile);
                    }

                    return (link, display, xrefSpec?.DeclairingFile);
                },
                (document) =>
                {
                    if (document != null)
                    {
                        var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(document);
                        errors.AddIfNotNull(error);
                        return monikers;
                    }
                    return new List<string>();
                });

            errors.AddRange(loadErrors);

            return (errors, model, referencedDocuments, referencedTocs);
        }

        private static bool ReferencesLocalizedToc(Document file, TableOfContentsMap tocMap)
        {
            return tocMap.TryGetTocReferences(file, out var tocReferences) && tocReferences.Any(toc => !toc.Docset.IsFallback());
        }
    }
}
