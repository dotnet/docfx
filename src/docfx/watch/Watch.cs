// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class Watch
    {
        public static Task Run(string docsetPath, CommandLineOptions options)
        {
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, e) => cts.Cancel();
                return CreateWebServer(docsetPath, options).Build().RunAsync(cts.Token);
            }
        }

        public static IWebHostBuilder CreateWebServer(string docsetPath, CommandLineOptions options)
        {
            return new WebHostBuilder()
                .UseUrls($"http://*:{options.Port}")
                .Configure(Configure);

            void Configure(IApplicationBuilder app)
            {
                app.Use(next => async httpContext =>
                {
                    await httpContext.Response.WriteAsync(docsetPath);
                });
            }
        }
    }
}
