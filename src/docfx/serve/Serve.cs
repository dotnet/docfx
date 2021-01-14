// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerdbank.Streams;

namespace Microsoft.Docs.Build
{
    internal static class Serve
    {
        public static bool Run(CommandLineOptions options, Package? package = null)
        {
            if (options.Port == null)
            {
                throw new InvalidOperationException("`Port` parameter is required to serve curent docset as language server, please use `--port {port}`");
            }

            if (!options.LanguageServer)
            {
                Console.WriteLine("Docfx only support serving as a language server in 'Serve' mode, please use `--language-server`");
                return true;
            }

            var host = AspNetCore.WebHost.CreateDefaultBuilder()
                 .UseEnvironment(Environments.Production)
                 .UseUrls($"http://localhost:{options.Port}/")
                 .Configure(Configure)
                 .Build();

            var hostTask = host.RunAsync();
            Console.WriteLine("Ready");
            hostTask.GetAwaiter().GetResult();
            return false;

            void Configure(IApplicationBuilder app)
            {
                app.UseWebSockets()
                   .Use(async (context, next) =>
                   {
                       if (context.Request.Path == "/lsp")
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
                       else
                       {
                           context.Response.StatusCode = 400;
                       }
                   });
            }
        }
    }
}
