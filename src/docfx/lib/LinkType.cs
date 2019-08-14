// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum LinkType
    {
        /// <summary>
        /// The link points to an external URL, e.g. https://docs.com
        /// </summary>
        External,

        /// <summary>
        /// The link points to an absolute URL, e.g. /base-path/landing-page
        /// </summary>
        AbsolutePath,

        /// <summary>
        /// The link points to a relative path, e.g. ../summary.md
        /// </summary>
        RelativePath,

        /// <summary>
        /// The link is a bookmark on same page, e.g. #title
        /// </summary>
        SelfBookmark,

        /// <summary>
        /// The link points to an absolute windows file path with volumn separator, e.g. C:/foo.md
        /// </summary>
        WindowsAbsolutePath,
    }
}
