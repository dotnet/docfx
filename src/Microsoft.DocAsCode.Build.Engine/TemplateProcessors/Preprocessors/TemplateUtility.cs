// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

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
                Logger.LogWarning($"Invalid file link: ({originalHref})", file:sourceFileKey, code:WarningCodes.Build.InvalidFileLink);
                return originalHref;
            }
            return file.UrlEncode().ToString() + UriUtility.GetQueryStringAndFragment(originalHref);
        }

        // TODO: remove this function as it breaks incremental build's design.
        public string Markup(string markdown, string sourceFileKey)
        {
            if (string.IsNullOrEmpty(sourceFileKey) || string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }
            if (_context.MarkdownService == null)
            {
                var message = $"Markup failed: {nameof(_context.MarkdownService)} should not be null";
                Logger.LogError(message);
                throw new DocfxException(message);
            }
            MarkupResult mr;
            try
            {
                mr = _context.MarkdownService.Markup(markdown, sourceFileKey);
                mr = MarkupUtility.Parse(mr, sourceFileKey, _context.AllSourceFiles);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail("Markup failed!");
                var message = $"Markup failed: {ex.Message}.";
                Logger.LogError(message);
                throw new DocumentException(message, ex);
            }
            return mr.Html;
        }
    }
}
