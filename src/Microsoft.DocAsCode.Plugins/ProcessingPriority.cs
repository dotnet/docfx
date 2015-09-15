// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.ComponentModel;

    public enum ProcessingPriority
    {
        NotSupportted,
        [EditorBrowsable(EditorBrowsableState.Never)]
        Lowest,
        Low,
        BelowNormal,
        Normal,
        AboveNormal,
        High,
        [EditorBrowsable(EditorBrowsableState.Never)]
        Highest,
    }
}
