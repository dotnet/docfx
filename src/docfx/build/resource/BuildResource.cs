// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class BuildResource
    {
        internal static (List<Error> errors, PublishItem publishItem) Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            var (errors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, context.MetadataProvider);
            var outputPath = file.GetOutputPath(monikers);

            // Output path is source file path relative to output folder when copy resource is disabled
            var publishPash = file.Docset.Config.Output.CopyResources
                ? outputPath
                : PathUtility.NormalizeFile(
                    Path.GetRelativePath(
                        Path.GetFullPath(Path.Combine(file.Docset.DocsetPath, file.Docset.Config.Output.Path)),
                        Path.GetFullPath(Path.Combine(file.Docset.DocsetPath, file.FilePath))));

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = publishPash,
                Hash = HashUtility.GetFileSha1Hash(Path.Combine(file.Docset.DocsetPath, file.FilePath)),
                Locale = file.Docset.Locale,
                Monikers = monikers,
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && file.Docset.Config.Output.CopyResources)
            {
                context.Output.Copy(file, outputPath);
            }

            return (errors, publishItem);
        }
    }
}
