// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    public enum SchemaContentType
    {
        None,
        Markdown,
        Href,
        Xref,
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class SchemaContentTypeAttribute : Attribute
    {
        public SchemaContentType ContentType { get; }

        public SchemaContentTypeAttribute(SchemaContentType contentType) => ContentType = contentType;
    }
}
