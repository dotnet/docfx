// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RuledHttpClient
    {
        private static readonly HttpClient s_httpClient = new HttpClient();

        public static async Task<HttpResponseMessage> GetAsync(string requestUri, HttpSecretConfig[] rules)
            => await SendAsync(
                requestUri,
                rules,
                async rule =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, requestUri + rule.Query);
                    foreach (var header in rule.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                    return await s_httpClient.SendAsync(request);
                },
                async () => await s_httpClient.GetAsync(new Uri(requestUri)));

        public static async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, HttpSecretConfig[] rules)
            => await SendAsync(
                requestUri,
                rules,
                async rule =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, requestUri + rule.Query)
                    {
                        Content = content,
                    };
                    foreach (var header in rule.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                    return await s_httpClient.SendAsync(request);
                },
                async () => await s_httpClient.PutAsync(new Uri(requestUri), content));

        private static async Task<HttpResponseMessage> SendAsync(
            string requestUri,
            HttpSecretConfig[] rules,
            Func<HttpSecretConfig, Task<HttpResponseMessage>> actionIfMatch,
            Func<Task<HttpResponseMessage>> actionIfNoMatch)
        {
            foreach (var rule in rules)
            {
                if (rule.Match(requestUri))
                {
                    return await actionIfMatch(rule);
                }
            }
            return await actionIfNoMatch();
        }

        private static bool Match(this HttpSecretConfig rule, string uri)
        {
            if (!string.IsNullOrEmpty(rule.BaseUrl) && uri.StartsWith(rule.BaseUrl))
                return true;

            return false;
        }
    }
}
