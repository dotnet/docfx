// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HrefAttribute : SchemaContentTypeAttribute
    {
        public HrefAttribute()
            : base(SchemaContentType.Href) { }
    }
}
