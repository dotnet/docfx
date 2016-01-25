// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System;

    public class DisplayNameAttribute : Attribute
    {
        public virtual string Name { get; protected set; }

        public DisplayNameAttribute(string name)
        {
            Name = name;
        }
    }
}
