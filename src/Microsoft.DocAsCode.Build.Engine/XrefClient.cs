// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
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

        public XrefClient()
            : this(_sharedClient) { }

        public XrefClient(HttpClient client)
        {
            _client = client ?? _sharedClient;
        }

        public async Task<XRefSpec[]> ResloveAsync(string url)
        {
            using (var stream = await _client.GetStreamAsync(url))
            using (var sr = new StreamReader(stream))
            {
                return JsonUtility.Deserialize<XRefSpec[]>(sr);
            }
        }
    }
}
