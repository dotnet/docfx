// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal abstract class RestoreIndexModel
    {
        public int Id { get; set; }

        public DateTime RestoredDate { get; set; }

        public bool Restored { get; set; }

        public LockType LockType { get; set; }

        public List<RestoreIndexAcquirer> RequiredBy { get; set; } = new List<RestoreIndexAcquirer>();
    }
}
