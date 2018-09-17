// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    public static class ResolveXrefMap
    {
        private static readonly ConcurrentDictionary<string, ConcurrentHashSet<XRefSpec>> _map = new ConcurrentDictionary<string, ConcurrentHashSet<XRefSpec>>();
        private static readonly Lazy<HttpClient> s_client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        });

        internal static async Task RestoreAsync(Uri uri)
        {
            using (var stream = await s_client.Value.GetStreamAsync(uri))
            using (var reader = new StreamReader(stream))
            {
                var (_, xrefs) = JsonUtility.Deserialize<List<XRefSpec>>(reader);
                Parallel.ForEach(xrefs, xref =>
                {
                    var set = new ConcurrentHashSet<XRefSpec>(XRefSpec.Comparer);
                    set.TryAdd(xref);
                    _map.AddOrUpdate(xref.Uid, set, (key, oldValue) =>
                    {
                        oldValue.TryAdd(xref);
                        return oldValue;
                    });
                });
            }
        }

        internal static XRefSpec Resolve(string uid)
        {
            if (_map.TryGetValue(uid, out var xRefSpecs))
            {
                // TODO: get the one with highest priority if multiple with the same uid
                return xRefSpecs.FirstOrDefault();
            }
            return null;
        }
    }
}
