// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class XrefClient
    {
        public static readonly XrefClient Default = new XrefClient();
        private static readonly HttpClient _sharedClient =
            new Func<HttpClient>(() =>
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                return client;
            })();
        private readonly HttpClient _client;
        private readonly SemaphoreSlim _semaphore;

        public XrefClient()
            : this(_sharedClient, null) { }

        public XrefClient(HttpClient client)
            : this(null, null) { }

        public XrefClient(int maxParallism)
            : this(null, new SemaphoreSlim(maxParallism)) { }

        public XrefClient(SemaphoreSlim semaphore)
            : this(null, semaphore) { }

        public XrefClient(HttpClient client, int maxParallism)
            : this(client, new SemaphoreSlim(maxParallism)) { }

        public XrefClient(HttpClient client, SemaphoreSlim semaphore)
        {
            _client = client ?? _sharedClient;
            _semaphore = semaphore;
        }

        public async Task<List<XRefSpec>> ResolveAsync(string url)
        {
            if (_semaphore == null)
            {
                return await ResolveCoreAsync(url);
            }
            await _semaphore.WaitAsync();
            try
            {
                return await ResolveCoreAsync(url);
            }
            finally
            {
                if (_semaphore != null)
                {
                    _semaphore.Release();
                }
            }
        }

        private async Task<List<XRefSpec>> ResolveCoreAsync(string url)
        {
            using (var stream = await _client.GetStreamAsync(url))
            using (var sr = new StreamReader(stream))
            {
                var xsList = JsonUtility.Deserialize<List<Dictionary<string, object>>>(sr);
                return xsList.ConvertAll(item =>
                {
                    var spec = new XRefSpec();
                    foreach (var pair in item)
                    {
                        if (pair.Value is string s)
                        {
                            spec[pair.Key] = s;
                        }
                    }
                    return spec;
                });
            }
        }
    }
}
