// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public static class DfmFallbackHelper
    {
        /// <summary>
        /// Get file path with fallback
        /// </summary>
        /// <param name="orginalRelativePath">original relative path in markdown.</param>
        /// <param name="context">markdown context</param>
        /// <returns>item1: acutal file path. item: true if it hit fallback file. Otherwise false</returns>
        public static Tuple<string, bool> GetFilePathWithFallback(string orginalRelativePath, IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (orginalRelativePath == null)
            {
                throw new ArgumentNullException(nameof(orginalRelativePath));
                throw new FileNotFoundException($"Couldn't resolve path {orginalRelativePath}.");
            }

            var originalFilePath = Path.Combine(context.GetBaseFolder(), orginalRelativePath);
            var actualFilePath = originalFilePath;
            bool hitFallback = false;
            if (!File.Exists(originalFilePath))
            {
                var fallbackFolders = context.GetFallbackFolders();
                foreach (var folder in fallbackFolders)
                {
                    var fallbackFilePath = Path.Combine(folder, orginalRelativePath);
                    var fallbackFileRelativePath = PathUtility.MakeRelativePath(Path.GetDirectoryName(originalFilePath), fallbackFilePath);
                    context.ReportDependency(fallbackFileRelativePath); // All the high priority fallback files should be reported to the dependency.
                    if (File.Exists(fallbackFilePath))
                    {
                        actualFilePath = fallbackFilePath;
                        hitFallback = true;
                        break;
                    }
                }

                if (!hitFallback)
                {
                    if (fallbackFolders.Count > 0)
                    {
                        throw new FileNotFoundException($"Couldn't find file {originalFilePath}. Fallback folders: {string.Join(",", fallbackFolders)}", originalFilePath);
                    }
                    throw new FileNotFoundException($"Couldn't find file {originalFilePath}.", originalFilePath);
                }
            }

            return Tuple.Create(actualFilePath, hitFallback);
        }
    }
}
