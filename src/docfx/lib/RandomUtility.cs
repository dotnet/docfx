// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal static class RandomUtility
{
    private static readonly ThreadLocal<Random> s_random = new(() => new(Interlocked.Increment(ref s_randomSeed)));

    public static Random Random => s_random.Value!;

    private static int s_randomSeed = Environment.TickCount;
}
