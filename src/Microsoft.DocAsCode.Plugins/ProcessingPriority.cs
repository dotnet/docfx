// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.ComponentModel;

    public enum ProcessingPriority
    {
        NotSupported = -1,
        [EditorBrowsable(EditorBrowsableState.Never)]
        Lowest = 0,
        Low = 64,
        BelowNormal = 128,
        Normal = 256,
        AboveNormal = 512,
        High = 1024,
        [EditorBrowsable(EditorBrowsableState.Never)]
        Highest = int.MaxValue,
    }
}
