// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Common;

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

        public string GetHrefFromRoot(string originalHref, string sourceFileKey)
        {
            if (string.IsNullOrEmpty(sourceFileKey) || string.IsNullOrEmpty(originalHref) || !RelativePath.IsRelativePath(originalHref))
            {
                return originalHref;
            }
            var path = (RelativePath)sourceFileKey + (RelativePath)UriUtility.GetPath(originalHref);
            var file = path.GetPathFromWorkingFolder().UrlDecode();
            if (!_context.AllSourceFiles.ContainsKey(file))
            {
                return originalHref;
            }
            return file.UrlEncode().ToString() + UriUtility.GetQueryStringAndFragment(originalHref);
        }
    }
}
