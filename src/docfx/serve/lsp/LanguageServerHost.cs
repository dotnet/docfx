// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.General;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerHost
    {
        public static async Task RunLanguageServer(
            CommandLineOptions commandLineOptions,
            PipeReader input,
            PipeWriter output,
            Package? package = null,
            ILanguageServerNotificationListener? notificationListener = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"[LanguageServerHost] Start to run ({commandLineOptions.WorkingDirectory})");

                var languageServerPackage = new LanguageServerPackage(
                    new(commandLineOptions.WorkingDirectory),
                    package ?? new LocalPackage(commandLineOptions.WorkingDirectory));

                var server = await LanguageServer.From(
                    options =>
                    {
                        options
                            .WithInput(input)
                            .WithOutput(output)
                            .ConfigureLogging(x => x.AddLanguageProtocolLogging().SetMinimumLevel(LogLevel.Trace))
                            .WithHandler<TextDocumentHandler>()
                            .WithHandler<DidChangeWatchedFilesHandler>()
                            .WithServices(services => services
                                .AddSingleton(notificationListener ?? new LanguageServerNotificationListener())
                                .AddSingleton(languageServerPackage)
                                .AddSingleton(commandLineOptions)
                                .AddSingleton<DiagnosticPublisher>()
                                .AddSingleton<LanguageServerCredentialProvider>()
                                .AddSingleton<LanguageServerBuilder>()
                                .AddSingleton(new ConfigurationItem
                                {
                                    Section = "docfxLanguageServer",
                                })
                                .AddOptions()
                                .AddLogging())
                            .OnInitialize((ILanguageServer server, InitializeParams request, CancellationToken cancellationToken) =>
                            {
                                Console.WriteLine($"[LanguageServerHost] Server receive intialize request ({commandLineOptions.WorkingDirectory})");
                                return Task.CompletedTask;
                            })
                            .OnInitialized((ILanguageServer server, InitializeParams request, InitializeResult response, CancellationToken cancellationToken) =>
                            {
                                Console.WriteLine($"[LanguageServerHost] Server initialized ({commandLineOptions.WorkingDirectory})");
                                return Task.CompletedTask;
                            })
                            .OnExit(_ =>
                            {
                                Console.WriteLine("Server exit");
                                return Task.CompletedTask;
                            });
                        options.OnUnhandledException = (e) =>
                        {
                            Console.Write($"[LanguageServerHost unhandled] Unexpected exception: {e}");
                        };
                    },
                    cancellationToken);

                var builder = server.GetRequiredService<LanguageServerBuilder>();

                await Task.WhenAll(
                    server.WaitForExit,
                    Task.Run(() => builder.Run(cancellationToken), cancellationToken));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LanguageServerHost] Unexpected exception: {ex}");
            }
        }
    }
}
