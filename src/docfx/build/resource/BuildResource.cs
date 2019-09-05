// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class BuildResource
    {
        internal static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            var errors = new List<Error>();
            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);

            var outputPath = file.GetOutputPath(monikers, file.Docset.SiteBasePath, isPage: false);

            // Output path is source file path relative to output folder when copy resource is disabled
            var copy = true;
            var publishPath = outputPath;

            if (!file.Docset.Config.Output.CopyResources &&
                context.Input.TryGetPhysicalPath(file.FilePath, out var physicalPath))
            {
                copy = false;
                publishPath = PathUtility.NormalizeFile(Path.GetRelativePath(
                    Path.GetFullPath(Path.Combine(file.Docset.DocsetPath, file.Docset.Config.Output.Path)),
                    Path.GetFullPath(physicalPath)));
            }

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = publishPath,
                SourcePath = file.FilePath.Path,
                Locale = file.Docset.Locale,
                Monikers = monikers,
                MonikerGroup = MonikerUtility.GetGroup(monikers),
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && copy)
            {
                context.Output.Copy(file, outputPath);
            }

            return errors;
        }
    }
}
