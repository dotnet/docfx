// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RetryUtility
    {
        private const int RetryCount = 3;
        private const int RetryInterval = 1000;

        public static async Task<T> Retry<T>(Func<Task<T>> action, Func<Exception, bool> isExpectedException = null)
        {
            var count = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (isExpectedException == null || isExpectedException(ex))
                {
                    if (count++ < RetryCount - 1)
                    {
                        await Task.Delay(RetryInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
