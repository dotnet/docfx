// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class LegacyOutput
    {
        public static void Convert(Docset docset, Context context, List<(LegacyManifestItem manifestItem, Document document)> files)
        {
            using (Progress.Start("Convert Legacy Files"))
            {
                using (Progress.Start("Convert Legacy TOC Files"))
                {
                    ParallelUtility.ForEach(
                        files.Where(f => f.document.ContentType == ContentType.TableOfContents),
                        file => LegacyTableOfContents.Convert(docset, context, file.document, file.manifestItem),
                        Progress.Update);
                }

                using (Progress.Start("Convert Legacy Markdown/Redirection Files"))
                {
                    ParallelUtility.ForEach(
                        files.Where(f => f.document.ContentType == ContentType.Page || f.document.ContentType == ContentType.Redirection),
                        file => LegacyPage.Convert(docset, context, file.document, file.manifestItem),
                        Progress.Update);
                }

                using (Progress.Start("Convert Legacy Resource Files"))
                {
                    ParallelUtility.ForEach(
                        files.Where(f => f.document.ContentType == ContentType.Resource),
                        file => LegacyResource.Convert(docset, context, file.document, file.manifestItem),
                        Progress.Update);
                }
            }
        }
    }
}
