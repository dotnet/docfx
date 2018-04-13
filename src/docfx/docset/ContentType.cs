// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs
{
    internal enum ContentType
    {
        /// <summary>
        /// Unknown content, will not be build
        /// </summary>
        Unknown,

        /// <summary>
        /// Markdown documents
        /// </summary>
        Markdown,

        /// <summary>
        /// Table of contents
        /// </summary>
        TableOfContents,

        /// <summary>
        /// Schema driven document in yaml or json format
        /// </summary>
        SchemaDocument,

        /// <summary>
        /// Static asserts copied to output
        /// </summary>
        Asset,
    }
}
