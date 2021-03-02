// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RegressionTest
    {
        private static async Task Main(string[] args)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new("Bearer", Environment.GetEnvironmentVariable("System.AccessToken"));
            http.DefaultRequestHeaders.Accept.Add(new("application/json"));
            var endpoints = await (await http.GetAsync(
                "https://dev.azure.com/ceapex/Engineering/_apis/serviceendpoint/endpoints?endpointNames=Engineering&api-version=6.1-preview.4"))
                .EnsureSuccessStatusCode().Content.ReadAsStringAsync();

            Console.WriteLine(endpoints);
        }
    }
}
