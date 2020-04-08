// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal enum TableOfContentsLinkType
    {
        /// <summary>
        /// Link to a folder to reference a TOC but don't embed the items inside this TOC node.
        /// </summary>
        Folder,

        /// <summary>
        /// Link to a TOC file to embed the items into TOC nodes.
        /// </summary>
        TocFile,

        /// <summary>
        /// Breadcrumbs set tocHref to absolute path.
        /// </summary>
        AbsolutePath,

        /// <summary>
        /// Other link types
        /// </summary>
        Other,
    }
}
