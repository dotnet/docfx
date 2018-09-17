// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    public class XRefSpec
    {
        public static readonly IEqualityComparer<XRefSpec> Comparer = new EqualityComparer();

        public string Uid { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public string NameWithType { get; set; }

        public string Href { get; set; }

        private class EqualityComparer : IEqualityComparer<XRefSpec>
        {
            public bool Equals(XRefSpec x, XRefSpec y)
            {
                return x.Uid == y.Uid &&
                       x.Name == y.Name &&
                       x.FullName == y.FullName &&
                       x.NameWithType == y.NameWithType &&
                       x.Href == y.Href;
            }

            public int GetHashCode(XRefSpec obj)
            {
                return HashCode.Combine(obj.Uid, obj.Name, obj.FullName, obj.NameWithType, obj.Href);
            }
        }
    }
}
