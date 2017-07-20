// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class TocDocumentProcessor : TocDocumentProcessorBase
    {
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

        protected override void RegisterTocToContext(TocItemViewModel toc, FileModel model, IDocumentBuildContext context)
        {
            var key = model.Key;

            // Add current folder to the toc mapping, e.g. `a/` maps to `a/toc`
            var directory = ((RelativePath)key).GetPathFromWorkingFolder().GetDirectoryPath();
            context.RegisterToc(key, directory);

            var tocInfo = new TocInfo(key);
            if (toc.Homepage != null)
            {
                if (PathUtility.IsRelativePath(toc.Homepage))
                {
                    var pathToRoot = ((RelativePath)model.File + (RelativePath)HttpUtility.UrlDecode(toc.Homepage)).GetPathFromWorkingFolder();
                    tocInfo.Homepage = pathToRoot;
                }
            }

            context.RegisterTocInfo(tocInfo);
        }

        protected override void RegisterTocMapToContext(TocItemViewModel item, FileModel model, IDocumentBuildContext context)
        {
            var key = model.Key;
            // If tocHref is set, href is originally RelativeFolder type, and href is set to the homepage of TocHref,
            // So in this case, TocHref should be used to in TocMap
            // TODO: what if user wants to set TocHref?
            var tocHref = item.TocHref;
            var tocHrefType = Utility.GetHrefType(tocHref);
            if (tocHrefType == HrefType.MarkdownTocFile || tocHrefType == HrefType.YamlTocFile)
            {
                context.RegisterToc(key, HttpUtility.UrlDecode(UriUtility.GetPath(tocHref)));
            }
            else
            {
                var href = item.Href; // Should be original href from working folder starting with ~
                if (Utility.IsSupportedRelativeHref(href))
                {
                    context.RegisterToc(key, HttpUtility.UrlDecode(UriUtility.GetPath(href)));
                }
            }
        }
    }
}
