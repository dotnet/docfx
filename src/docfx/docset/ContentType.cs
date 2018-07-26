// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal enum ContentType
    {
        /// <summary>
        /// Unknown content, will not be build
        /// </summary>
        Unknown,

        /// <summary>
        /// Html pages generated from markdown documents or schema documents
        /// </summary>
        Page,

        /// <summary>
        /// Table of contents
        /// </summary>
        TableOfContents,

        /// <summary>
        /// Static resources copied to output
        /// </summary>
        Resource,

        /// <summary>
        /// Redirected documents specifed in redirection config
        /// </summary>
        Redirection,
    }
}
