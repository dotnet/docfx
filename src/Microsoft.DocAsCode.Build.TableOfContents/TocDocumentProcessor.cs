// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common.Git;

    using TypeForwardedToRelativePath = Microsoft.DocAsCode.Common.RelativePath;
    using TypeForwardedToPathUtility = Microsoft.DocAsCode.Common.PathUtility;

    [Export(typeof(IDocumentProcessor))]
    public class TocDocumentProcessor : DisposableDocumentProcessor
    {
        private static readonly char[] QueryStringOrAnchor = new[] { '#', '?' };

        public override string Name => nameof(TocDocumentProcessor);

        [ImportMany(nameof(TocDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Article && Utility.IsSupportedFile(file.File))
            {
                return ProcessingPriority.High;
            }

            return ProcessingPriority.NotSupported;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var filePath = file.FullPath;
            var tocViewModel = Utility.LoadSingleToc(filePath);
            var toc = new TocItemViewModel
            {
                Items = tocViewModel
            };

            var repoDetail = GitUtility.TryGetFileDetail(filePath, EnvironmentContext.RepoRootDirectory);
            var displayLocalPath = TypeForwardedToPathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

            // todo : metadata.
            return new FileModel(file, toc)
            {
                Uids = new[] { new UidDefinition(file.File, displayLocalPath) }.ToImmutableArray(),
                LocalPathFromRepoRoot = repoDetail?.RelativePath ?? filePath,
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
            var key = model.Key;

            // Add current folder to the toc mapping, e.g. `a/` maps to `a/toc`
            var directory = ((TypeForwardedToRelativePath)key).GetPathFromWorkingFolder().GetDirectoryPath();
            context.RegisterToc(key, directory);
            UpdateTocItemHref(toc, model, context);
            var tocInfo = new TocInfo(key);
            if (toc.Homepage != null)
            {
                if (TypeForwardedToPathUtility.IsRelativePath(toc.Homepage))
                {
                    var pathToRoot = ((TypeForwardedToRelativePath)model.File + (TypeForwardedToRelativePath)toc.Homepage).GetPathFromWorkingFolder();
                    tocInfo.Homepage = pathToRoot;
                }
            }

            context.RegisterTocInfo(tocInfo);
        }

        private void UpdateTocItemHref(TocItemViewModel toc, FileModel model, IDocumentBuildContext context)
        {
            if (toc.IsHrefUpdated) return;

            ResolveUid(toc, model, context);

            // Have to register TocMap after uid is resolved
            RegisterTocMap(toc, model.Key, context);

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

                    string nameForCSharp;
                    if (string.IsNullOrEmpty(item.NameForCSharp) && xref.TryGetValue("name.csharp", out nameForCSharp))
                    {
                        item.NameForCSharp = nameForCSharp;
                    }
                    string nameForVB;
                    if (string.IsNullOrEmpty(item.NameForVB) && xref.TryGetValue("name.vb", out nameForVB))
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

        private void RegisterTocMap(TocItemViewModel item, string key, IDocumentBuildContext context)
        {
            // If tocHref is set, href is originally RelativeFolder type, and href is set to the homepage of TocHref,
            // So in this case, TocHref should be used to in TocMap
            // TODO: what if user wants to set TocHref?
            var tocHref = item.TocHref;
            var tocHrefType = Utility.GetHrefType(tocHref);
            if (tocHrefType == HrefType.MarkdownTocFile || tocHrefType == HrefType.YamlTocFile)
            {
                context.RegisterToc(key, tocHref);
            }
            else
            {
                var href = item.Href; // Should be original href from working folder starting with ~
                if (Utility.IsSupportedRelativeHref(href))
                {
                    context.RegisterToc(key, href);
                }
            }
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

            string href = index == -1
                ? context.GetFilePath(pathToFile)
                : context.GetFilePath(pathToFile.Remove(index));


            if (href == null)
            {
                Logger.LogInfo($"Unable to find file \"{originalPathToFile}\" for {propertyName} referenced by TOC file \"{model.LocalPathFromRoot}\"");
                return originalPathToFile;
            }

            var relativePath = GetRelativePath(href, model.File);
            var path = ((TypeForwardedToRelativePath)relativePath).UrlEncode().ToString();
            if (index >= 0)
            {
                path += pathToFile.Substring(index);
            }
            return path;
        }

        private string GetRelativePath(string pathFromWorkingFolder, string relativeToPath)
        {
            return ((TypeForwardedToRelativePath)pathFromWorkingFolder).MakeRelativeTo((((TypeForwardedToRelativePath)relativeToPath).UrlDecode()).GetPathFromWorkingFolder());
        }
    }
}
