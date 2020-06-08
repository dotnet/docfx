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

            var outputPath = context.DocumentProvider.GetOutputPath(file.FilePath);

            // Output path is source file path relative to output folder when copy resource is disabled
            var copy = true;

            if (!context.Config.CopyResources &&
                context.Input.TryGetPhysicalPath(file.FilePath, out _))
            {
                copy = false;
            }

            if (copy && !context.Config.DryRun)
            {
                context.Output.Copy(outputPath, file.FilePath);
            }

            return new List<Error>();
        }
    }
}
