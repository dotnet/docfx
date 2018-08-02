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
            }

            // var currentFileFolder = Path.Combine(context.GetBaseFolder(), Path.Combine(context.GetFilePathStack().Select(path => Path.GetDirectoryName(path)).ToArray()));
            // var originalFilePath = Path.Combine(context.GetBaseFolder(),  orginalRelativePath);
            RelativePath filePathToDocset = null;
            string parentFileDirectoryToDocset = context.GetBaseFolder();
            var parents = context.GetFilePathStack();
            if (parents != null)
            {
                var parent = parents.Peek();
                filePathToDocset = ((RelativePath)parent + (RelativePath)relativePath).RemoveWorkingFolder();
                parentFileDirectoryToDocset = Path.GetDirectoryName(Path.Combine(context.GetBaseFolder(), parent));
            }
            else
            {
                filePathToDocset = ((RelativePath)relativePath).RemoveWorkingFolder();
            }

            var originalFullPath = Path.Combine(context.GetBaseFolder(), filePathToDocset);

            bool isFallback;
            ReportFallbackToDependency(context, filePathToDocset, parentFileDirectoryToDocset, out isFallback);

            if (EnvironmentContext.FileAbstractLayer.Exists(filePathToDocset))
            {
                return Tuple.Create((string)filePathToDocset, isFallback);
            }

            throw new FileNotFoundException($"Couldn't find file {filePathToDocset} in folders: {string.Join(",", EnvironmentContext.FileAbstractLayer.GetExpectedPhysicalPath(filePathToDocset))}", filePathToDocset);
        }

        private static void ReportFallbackToDependency(IMarkdownContext context, RelativePath filePathToDocset, string parentFileDirectoryToDocset, out bool isFallback)
        {
            var expectedPhysicalPath = EnvironmentContext.FileAbstractLayer.GetExpectedPhysicalPath(filePathToDocset);
            isFallback = false;
            foreach(var physicalPath in expectedPhysicalPath)
            {
                if (isFallback)
                {
                    var fallbackFileRelativePath = PathUtility.MakeRelativePath(parentFileDirectoryToDocset, physicalPath);
                    context.ReportDependency(fallbackFileRelativePath); // All the high priority fallback files should be reported to the dependency.
                }

                if (File.Exists(physicalPath)) break;
                isFallback = true;
            }
        }

        private static Tuple<string, bool> FindInFallbackFolders(IMarkdownContext context, RelativePath filePathToDocset, string parentFileDirectoryToDocset, string originalFullPath)
        {
            var fallbackFolders = context.GetFallbackFolders();
            foreach (var folder in fallbackFolders)
            {
                var fallbackFilePath = Path.Combine(folder, filePathToDocset);
                var fallbackFileRelativePath = PathUtility.MakeRelativePath(parentFileDirectoryToDocset, fallbackFilePath);
                context.ReportDependency(fallbackFileRelativePath); // All the high priority fallback files should be reported to the dependency.
                if (EnvironmentContext.FileAbstractLayer.Exists(fallbackFilePath))
                {
                    return Tuple.Create(fallbackFilePath, true);
                }
            }
            if (fallbackFolders.Count > 0)
            {
                throw new FileNotFoundException($"Couldn't find file {filePathToDocset}. Fallback folders: {string.Join(",", fallbackFolders)}", filePathToDocset);
            }
            throw new FileNotFoundException($"Couldn't find file {filePathToDocset}.", originalFullPath);
        }
    }
}
