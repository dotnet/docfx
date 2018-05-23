// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyMarkdown
    {
        public static void Convert(
            Docset docset,
            Context context,
            string absoluteOutputFilePath,
            string relativeOutputFilePath,
            string legacyOutputFilePathRelativeToSiteBasePath)
        {
            var rawPageOutputPath = Path.ChangeExtension(absoluteOutputFilePath, ".raw.page.json");
            var metaOutputPath = Path.ChangeExtension(absoluteOutputFilePath, ".mta.json");

            File.Move(absoluteOutputFilePath, rawPageOutputPath);
        }
    }
}
