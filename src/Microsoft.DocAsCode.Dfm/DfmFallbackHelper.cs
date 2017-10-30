// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Obsolete]
    public static class DfmFallbackHelper
    {
        /// <summary>
        /// Get file path with fallback
        /// </summary>
        /// <param name="relativePath">original relative path in markdown.</param>
        /// <param name="context">markdown context</param>
        /// <returns>item1: acutal file path. item: true if it hit fallback file. Otherwise false</returns>
        public static Tuple<string, bool> GetFilePathWithFallback(string relativePath, IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
                throw new FileNotFoundException($"Couldn't resolve path {relativePath}.");
            }

            // var currentFileFolder = Path.Combine(context.GetBaseFolder(), Path.Combine(context.GetFilePathStack().Select(path => Path.GetDirectoryName(path)).ToArray()));
            // var originalFilePath = Path.Combine(context.GetBaseFolder(),  orginalRelativePath);
            var filePathToDocset = relativePath;
            string parentFileDirectoryToDocset = context.GetBaseFolder();
            var parents = context.GetFilePathStack();
            if(parents != null)
            {
                var parent = parents.Peek();
                filePathToDocset = ((RelativePath)parent + (RelativePath)filePathToDocset).RemoveWorkingFolder();
                parentFileDirectoryToDocset = Path.GetDirectoryName(Path.Combine(context.GetBaseFolder(), parent));
            }

            var originalFilePath = Path.Combine(context.GetBaseFolder(), filePathToDocset);
            var actualFilePath = originalFilePath;
            bool hitFallback = false;
            if (!EnvironmentContext.FileAbstractLayer.Exists(originalFilePath))
            {
                var fallbackFolders = context.GetFallbackFolders();
                foreach (var folder in fallbackFolders)
                {
                    var fallbackFilePath = Path.Combine(folder, filePathToDocset);
                    var fallbackFileRelativePath = PathUtility.MakeRelativePath(parentFileDirectoryToDocset, fallbackFilePath);
                    context.ReportDependency(fallbackFileRelativePath); // All the high priority fallback files should be reported to the dependency.
                    if (EnvironmentContext.FileAbstractLayer.Exists(fallbackFilePath))
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
                        throw new FileNotFoundException($"Couldn't find file {filePathToDocset}. Fallback folders: {string.Join(",", fallbackFolders)}", filePathToDocset);
                    }
                    throw new FileNotFoundException($"Couldn't find file {filePathToDocset}.", originalFilePath);
                }
            }

            return Tuple.Create(actualFilePath, hitFallback);
        }
    }
}
