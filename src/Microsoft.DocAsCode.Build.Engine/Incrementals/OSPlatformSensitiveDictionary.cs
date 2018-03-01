// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class OSPlatformSensitiveDictionary<V> : Dictionary<string, V>
    {
        public OSPlatformSensitiveDictionary() : base(FilePathComparer.OSPlatformSensitiveStringComparer)
        {
        }

        public OSPlatformSensitiveDictionary(IDictionary<string, V> dictionary) : base(dictionary, FilePathComparer.OSPlatformSensitiveStringComparer)
        {
        }

        public OSPlatformSensitiveDictionary(IEnumerable<KeyValuePair<string, V>> list) : this()
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            foreach (var item in list)
            {
                this[item.Key] = item.Value;
            }
        }
    }
}
