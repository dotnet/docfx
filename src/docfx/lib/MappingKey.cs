// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MappingKey : IEquatable<MappingKey>
    {
        public JToken Key { get; set; }

        public bool Equals(MappingKey other)
        {
            if (other == null)
                return false;
            return Key == other.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as MappingKey;
            return Equals(other);
        }
    }
}
