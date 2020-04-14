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
            var (monikerErrors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddRange(monikerErrors);

            var outputPath = context.DocumentProvider.GetOutputPath(file.FilePath, monikers);

            // Output path is source file path relative to output folder when copy resource is disabled
            var copy = true;
            var publishPath = outputPath;

            if (!context.Config.CopyResources &&
                context.Input.TryGetPhysicalPath(file.FilePath, out var physicalPath))
            {
                copy = false;
                publishPath = PathUtility.NormalizeFile(Path.GetRelativePath(context.Output.OutputPath, physicalPath));
            }

            var publishItem = new PublishItem(
                file.SiteUrl,
                publishPath,
                file.FilePath.Path,
                context.BuildOptions.Locale,
                monikers,
                context.MonikerProvider.GetConfigMonikerRange(file.FilePath),
                file.ContentType,
                file.Mime.Value);

            context.PublishModelBuilder.Add(file.FilePath, publishItem, () =>
            {
                if (copy && !context.Config.DryRun)
                {
                    context.Output.Copy(outputPath, file.FilePath);
                }
            });

            return errors;
        }
    }
}
