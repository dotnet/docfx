// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization
{
    using System;

    public sealed class ExtensibleMemberAttribute : Attribute
    {
        public string Prefix { get; }

        public ExtensibleMemberAttribute()
            : this(null)
        {
        }

        public ExtensibleMemberAttribute(string prefix)
        {
            Prefix = prefix ?? string.Empty;
        }
    }
}
