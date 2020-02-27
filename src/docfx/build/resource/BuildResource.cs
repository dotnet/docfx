// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class BuildResource
    {
        internal static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            var errors = new List<Error>();
            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddIfNotNull(monikerError);

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

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = publishPath,
                SourcePath = file.FilePath.Path,
                Locale = context.LocalizationProvider.Locale,
                Monikers = monikers,
                MonikerGroup = MonikerUtility.GetGroup(monikers),
                ConfigMonikerRange = context.MonikerProvider.GetConfigMonikerRange(file.FilePath),
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && copy && !context.Config.DryRun)
            {
                context.Output.Copy(file, outputPath);
            }

            return errors;
        }
    }
}
