// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs
{
    internal static class ParallelUtility
    {
        public static Task ForEach<T>(IEnumerable<T> source, Func<T, Task> action)
        {
            return Task.CompletedTask;
        }
    }
}
