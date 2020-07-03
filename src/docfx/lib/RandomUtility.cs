// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal static class RandomUtility
    {
        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        public static Random Random => t_random.Value!;
    }
}
