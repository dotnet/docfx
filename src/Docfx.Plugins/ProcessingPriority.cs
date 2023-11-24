// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Docfx.Plugins;

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
