﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Immutable;
    using System.IO;
    using System.Web;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Common.Git;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// Base document processor for table of contents.
    /// </summary>
    public abstract class TocDocumentProcessorBase : DisposableDocumentProcessor
    {
        private static readonly char[] QueryStringOrAnchor = new[] { '#', '?' };

        #region IDocumentProcessor

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var filePath = file.FullPath;
            var toc = TocHelper.LoadSingleToc(filePath);

            var repoDetail = GitUtility.TryGetFileDetail(filePath);
            var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

            // Apply metadata to TOC
            foreach (var pair in metadata)
            {
                if (!toc.Metadata.TryGetValue(pair.Key, out var val))
                {
                    toc.Metadata[pair.Key] = pair.Value;
                }
            }

            return new FileModel(file, toc)
            {
                Uids = new[] { new UidDefinition(file.File, displayLocalPath) }.ToImmutableArray(),
                LocalPathFromRoot = displayLocalPath
            };
        }

        public override SaveResult Save(FileModel model)
        {
            return new SaveResult
            {
                DocumentType = "Toc",
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = model.LinkToFiles.ToImmutableArray(),
                LinkToUids = model.LinkToUids,
            };
        }

        public override void UpdateHref(FileModel model, IDocumentBuildContext context)
        {
            var toc = (TocItemViewModel)model.Content;
            UpdateTocItemHref(toc, model, context);

            RegisterTocToContext(toc, model, context);
        }

        #endregion

        #region Abstract methods

        protected abstract void RegisterTocToContext(TocItemViewModel item, FileModel model, IDocumentBuildContext context);

        protected abstract void RegisterTocMapToContext(TocItemViewModel item, FileModel model, IDocumentBuildContext context);

        #endregion

        #region Private methods

        private void UpdateTocItemHref(TocItemViewModel toc, FileModel model, IDocumentBuildContext context)
        {
            if (toc.IsHrefUpdated) return;

            ResolveUid(toc, model, context);

            // Have to register TocMap after uid is resolved
            RegisterTocMapToContext(toc, model, context);

            toc.Homepage = ResolveHref(toc.Homepage, toc.OriginalHomepage, model, context, nameof(toc.Homepage));
            toc.Href = ResolveHref(toc.Href, toc.OriginalHref, model, context, nameof(toc.Href));
            toc.TocHref = ResolveHref(toc.TocHref, toc.OriginalTocHref, model, context, nameof(toc.TocHref));
            toc.TopicHref = ResolveHref(toc.TopicHref, toc.OriginalTopicHref, model, context, nameof(toc.TopicHref));

            if (toc.Items != null && toc.Items.Count > 0)
            {
                foreach (var item in toc.Items)
                {
                    UpdateTocItemHref(item, model, context);
                }
            }

            toc.IsHrefUpdated = true;
        }

        private void ResolveUid(TocItemViewModel item, FileModel model, IDocumentBuildContext context)
        {
            if (item.TopicUid != null)
            {
                var xref = GetXrefFromUid(item.TopicUid, model, context);
                if (xref != null)
                {
                    item.Href = item.TopicHref = xref.Href;
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        item.Name = xref.Name;
                    }

                    if (string.IsNullOrEmpty(item.NameForCSharp) && xref.TryGetValue("name.csharp", out string nameForCSharp))
                    {
                        item.NameForCSharp = nameForCSharp;
                    }
                    if (string.IsNullOrEmpty(item.NameForVB) && xref.TryGetValue("name.vb", out string nameForVB))
                    {
                        item.NameForVB = nameForVB;
                    }
                }
            }
        }

        private XRefSpec GetXrefFromUid(string uid, FileModel model, IDocumentBuildContext context)
        {
            var xref = context.GetXrefSpec(uid);
            if (xref == null)
            {
                Logger.LogWarning($"Unable to find file with uid \"{uid}\" referenced by TOC file \"{model.LocalPathFromRoot}\"");
            }
            return xref;
        }

        private string ResolveHref(string pathToFile, string originalPathToFile, FileModel model, IDocumentBuildContext context, string propertyName)
        {
            if (!Utility.IsSupportedRelativeHref(pathToFile))
            {
                return pathToFile;
            }

            var index = pathToFile.IndexOfAny(QueryStringOrAnchor);
            if (index == 0)
            {
                throw new DocumentException($"Invalid toc link for {propertyName}: {originalPathToFile}.");
            }

            var path = UriUtility.GetPath(pathToFile);
            var segments = UriUtility.GetQueryStringAndFragment(pathToFile);

            var href = context.GetFilePath(HttpUtility.UrlDecode(path));

            // original path to file can be null for files generated by docfx in PreBuild
            var displayFilePath = string.IsNullOrEmpty(originalPathToFile) ? pathToFile : originalPathToFile;

            if (href == null)
            {
                Logger.LogInfo($"Unable to find file \"{displayFilePath}\" for {propertyName} referenced by TOC file \"{model.LocalPathFromRoot}\"");
                return originalPathToFile;
            }

            var relativePath = GetRelativePath(href, model.File);
            var resolvedHref = ((RelativePath)relativePath).UrlEncode().ToString() + segments;
            return resolvedHref;
        }

        private string GetRelativePath(string pathFromWorkingFolder, string relativeToPath)
        {
            return ((RelativePath)pathFromWorkingFolder).MakeRelativeTo((((RelativePath)relativeToPath).UrlDecode()).GetPathFromWorkingFolder());
        }

        #endregion
    }
}
