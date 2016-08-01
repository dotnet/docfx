// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class TemplateUtility
    {
        private readonly DocumentBuildContext _context;

        public TemplateUtility(DocumentBuildContext context)
        {
            _context = context;
        }

        public string ResolveSourceRelativePath(string originPath, string currentFileOutputPath)
        {
            if (string.IsNullOrEmpty(originPath) || !PathUtility.IsRelativePath(originPath))
            {
                return originPath;
            }

            var origin = (RelativePath)originPath;
            if (origin == null)
            {
                return originPath;
            }

            var destPath = _context.GetFilePath(origin.GetPathFromWorkingFolder().ToString());
            if (destPath != null)
            {
                return ((RelativePath)destPath - ((RelativePath)currentFileOutputPath).GetPathFromWorkingFolder()).ToString();
            }
            else
            {
                Logger.LogWarning($"Can't find output file for {originPath}");
                return originPath;
            }
        }
    }
}
