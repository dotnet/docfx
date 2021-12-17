// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal readonly struct LocInfo<T>
{
    public readonly T Value;

    public LocInfo Loc { get; } = new LocInfo(false);

    public LocInfo(T value, LocInfo loc)
    {
        Value = value;
        Loc = loc;
    }
}
