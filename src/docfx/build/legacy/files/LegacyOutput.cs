// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyOutput
    {
        public static void Convert(Docset docset, Context context, List<(LegacyManifestItem manifestItem, Document document, List<string> monikers)> files)
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
            }
        }
    }
}
