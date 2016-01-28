// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class TocDocumentProcessor : DisposableDocumentProcessor
    {
        public override string Name => nameof(TocDocumentProcessor);

        [ImportMany(nameof(TocDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Article)
            {
                if ("toc.md".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
                if ("toc.yml".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
            }
            return ProcessingPriority.NotSupportted;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var filePath = Path.Combine(file.BaseDir, file.File);
            TocViewModel toc = LoadSingleToc(filePath);

            var repoDetail = GitUtility.GetGitDetail(filePath);

            // todo : metadata.
            return new FileModel(file, toc)
            {
                Uids = new[] { file.File }.ToImmutableArray(),
                LocalPathFromRepoRoot = repoDetail?.RelativePath
            };
        }

        public override SaveResult Save(FileModel model)
        {
            return new SaveResult
            {
                DocumentType = "Toc",
                ModelFile = model.File,
            };
        }

        public override void UpdateHref(FileModel model, IDocumentBuildContext context)
        {
            var toc = (TocViewModel)model.Content;
            var path = model.OriginalFileAndType.File;

            // Add current folder to the toc mapping, e.g. `a/` maps to `a/toc`
            var directory = ((RelativePath)path).GetPathFromWorkingFolder().GetDirectoryPath();
            context.RegisterToc(path, directory);

            if (toc.Count > 0)
            {
                foreach (var item in toc)
                {
                    UpdateTocItemHref(item, path, context);
                }
            }
        }

        private void UpdateTocItemHref(TocItemViewModel toc, string path, IDocumentBuildContext context)
        {
            ResolveUid(toc, path, context);
            RegisterTocMap(toc, path, context);

            if (!string.IsNullOrEmpty(toc.Homepage))
            {
                toc.Href = toc.Homepage;
            }

            toc.Href = GetUpdatedHref(toc.Href, path, context);
            toc.OriginalHref = GetUpdatedHref(toc.OriginalHref, path, context);
            if (toc.Items != null && toc.Items.Count > 0)
            {
                foreach (var item in toc.Items)
                {
                    UpdateTocItemHref(item, path, context);
                }
            }
        }

        private void ResolveUid(TocItemViewModel item, string path, IDocumentBuildContext context)
        {
            if (item.Uid != null)
            {
                var xref = GetXrefFromUid(item.Uid, path, context);
                item.Href = xref.Href;
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

            if (item.HomepageUid != null)
            {
                item.Homepage = GetXrefFromUid(item.HomepageUid, path, context).Href;
            }
        }

        private XRefSpec GetXrefFromUid(string uid, string path, IDocumentBuildContext context)
        {
            var xref = context.GetXrefSpec(uid);
            if (xref == null)
            {
                throw new DocumentException($"Unable to find file with uid \"{uid}\" referenced by TOC file \"{path}\"");
            }
            return xref;
        }

        private void RegisterTocMap(TocItemViewModel item, string file, IDocumentBuildContext context)
        {
            var href = item.Href; // Should be original href from working folder starting with ~
            if (!PathUtility.IsRelativePath(href)) return;

            context.RegisterToc(file, href);
        }

        private string GetUpdatedHref(string originalPathToFile, string filePathToRoot, IDocumentBuildContext context)
        {
            if (!PathUtility.IsRelativePath(originalPathToFile)) return originalPathToFile;

            string href = context.GetFilePath(originalPathToFile);

            if (href == null)
            {
                throw new DocumentException($"Unalbe to find file \"{originalPathToFile}\" referenced by TOC file \"{filePathToRoot}\"");
            }

            var relativePath = GetRelativePath(href, filePathToRoot);
            return relativePath;
        }

        private string GetRelativePath(string pathFromWorkingFolder, string relativeToPath)
        {
            return ((RelativePath)pathFromWorkingFolder).MakeRelativeTo(((RelativePath)relativeToPath).GetPathFromWorkingFolder());
        }

        private TocViewModel LoadSingleToc(string filePath)
        {
            if ("toc.md".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return MarkdownTocReader.LoadToc(File.ReadAllText(filePath), filePath);
            }
            else if ("toc.yml".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return YamlUtility.Deserialize<TocViewModel>(filePath);
            }

            throw new NotSupportedException($"{filePath} is not a valid TOC file, supported toc files could be \"toc.md\" or \"toc.yml\".");
        }
    }
}
