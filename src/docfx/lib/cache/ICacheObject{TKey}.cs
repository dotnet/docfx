// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal interface ICacheObject<TKey>
    {
        DateTime? UpdatedAt { get; set; }

        IEnumerable<TKey> GetKeys();
    }
}
