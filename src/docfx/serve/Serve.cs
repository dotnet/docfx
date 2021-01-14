// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerdbank.Streams;

namespace Microsoft.Docs.Build
{
    internal static class Serve
    {
        public static bool Run(CommandLineOptions options, Package? package = null)
        {
            new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://{options.Address}:{options.Port}/")
                .Configure(Configure)
                .Build()
                .Run();
            return false;

            void Configure(IApplicationBuilder app)
            {
                if (options.LanguageServer)
                {
                    app.UseWebSockets()
                       .Map("/lsp", app => app.Run(context => StartLanguageServer(context, options, package)));
                }
            }
        }

        private static async Task StartLanguageServer(HttpContext context, CommandLineOptions options, Package? package)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var stream = webSocket.AsStream();
                await LanguageServerHost.RunLanguageServer(options, PipeReader.Create(stream), PipeWriter.Create(stream), package);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
    }
}
