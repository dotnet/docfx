// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> GetWithRetryAsync(this HttpClient client, string url, SemaphoreSlim semaphore, params int[] retryDelay)
        {
            if (retryDelay.Any(delay => delay <= 0))
            {
                throw new ArgumentException("Delay should be greate than 0.", nameof(retryDelay));
            }
            await semaphore.WaitAsync();
            try
            {
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        return await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    }
                    catch (TaskCanceledException)
                    {
                        if (retryCount >= retryDelay.Length)
                        {
                            throw;
                        }
                    }
                    await Task.Delay(retryDelay[retryCount]);
                    retryCount++;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
