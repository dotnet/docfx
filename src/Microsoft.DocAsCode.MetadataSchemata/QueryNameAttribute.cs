// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System;

    public class QueryNameAttribute : Attribute
    {
        public virtual string Name { get; protected set; }

        public QueryNameAttribute(string name)
        {
            Name = name;
        }
    }
}
