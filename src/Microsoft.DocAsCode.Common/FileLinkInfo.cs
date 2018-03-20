// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using Microsoft.DocAsCode.Plugins;

    public struct FileLinkInfo
        : IFileLinkInfo
    {
        public string Href { get; set; }

        public string FromFileInDest { get; set; }

        public string FromFileInSource { get; set; }

        public string ToFileInDest { get; set; }

        public string ToFileInSource { get; set; }

        public string FileLinkInSource { get; set; }

        public string FileLinkInDest { get; set; }

        public bool IsResolved => ToFileInDest != null;

        public GroupInfo GroupInfo { get;set; }

        public static FileLinkInfo Create(string fromFileInSource, string fromFileInDest, string href, IDocumentBuildContext context)
        {
            if (fromFileInSource == null)
            {
                throw new ArgumentNullException(nameof(fromFileInSource));
            }
            if (fromFileInDest == null)
            {
                throw new ArgumentNullException(nameof(fromFileInDest));
            }
            if (href == null)
            {
                throw new ArgumentNullException(nameof(href));
            }
            if (UriUtility.HasFragment(href) || UriUtility.HasQueryString(href))
            {
                throw new ArgumentException("fragment and query string is not supported", nameof(href));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var path = RelativePath.TryParse(href)?.UrlDecode();
            if (path == null)
            {
                throw new ArgumentException("only relative path is supported", nameof(href));
            }

            var fli = new FileLinkInfo
            {
                FromFileInSource = fromFileInSource,
                FromFileInDest = fromFileInDest,
                GroupInfo = context.GroupInfo,
            };
            if (path.IsFromWorkingFolder())
            {
                var targetInSource = path;
                fli.ToFileInSource = targetInSource.RemoveWorkingFolder();
                fli.ToFileInDest = RelativePath.GetPathWithoutWorkingFolderChar(context.GetFilePath(targetInSource));
                fli.FileLinkInSource = targetInSource - (RelativePath)fromFileInSource;
                if (fli.ToFileInDest != null)
                {
                    var resolved = (RelativePath)fli.ToFileInDest - (RelativePath)fromFileInDest;
                    fli.FileLinkInDest = resolved;
                    fli.Href = resolved.UrlEncode();
                }
                else
                {
                    fli.Href = (targetInSource.RemoveWorkingFolder() - ((RelativePath)fromFileInSource).RemoveWorkingFolder()).UrlEncode();
                }
            }
            else
            {
                fli.FileLinkInSource = path;
                fli.ToFileInSource = ((RelativePath)fromFileInSource + path).RemoveWorkingFolder();
                fli.FileLinkInDest = fli.FileLinkInSource;
                fli.Href = href;
            }

            return fli;
        }
    }
}
