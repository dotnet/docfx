// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum JsonSchemaContentType
    {
        None,
        Href, // resolve link
        Markdown, // markup
        InlineMarkdown, // inline content markup
        Html,
        Xref, // uid
    }
}
