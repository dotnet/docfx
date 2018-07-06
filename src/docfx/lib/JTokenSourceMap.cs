// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JTokenSourceMap : Dictionary<JToken, Range>
    {
        public JTokenSourceMap()
            : base(new JTokenRefComparer())
        {
        }

        private class JTokenRefComparer : IEqualityComparer<JToken>
        {
            public bool Equals(JToken x, JToken y) => ReferenceEquals(x, y);

            public int GetHashCode(JToken obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
