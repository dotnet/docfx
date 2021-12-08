// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.CompilerServices;

namespace Microsoft.Docs.Build;

internal class ReferenceEqualsComparer : IEqualityComparer, IEqualityComparer<object>
{
    public static readonly ReferenceEqualsComparer Default
        = new();

    private ReferenceEqualsComparer() { }

    public new bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}
