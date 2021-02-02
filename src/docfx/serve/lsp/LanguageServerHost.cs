// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.General;
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
            ILanguageServerNotificationListener? notificationListener = null)
        {
            using var cts = new CancellationTokenSource();
            using (Log.BeginScope(commandLineOptions.Verbose))
            {
                var languageServerPackage = new LanguageServerPackage(
                    new(commandLineOptions.WorkingDirectory),
                    package ?? new LocalPackage(commandLineOptions.WorkingDirectory));

                var server = await LanguageServer.From(options => options
                    .WithInput(input)
                    .WithOutput(output)
                    .ConfigureLogging(x => x.AddLanguageProtocolLogging())
                    .WithHandler<TextDocumentHandler>()
                    .WithHandler<DidChangeWatchedFilesHandler>()
                    .WithServices(services => services
                        .AddSingleton(notificationListener ?? new LanguageServerNotificationListener())
                        .AddSingleton(languageServerPackage)
                        .AddSingleton(commandLineOptions)
                        .AddSingleton<DiagnosticPublisher>()
                        .AddSingleton<LanguageServerCredentialProvider>()
                        .AddSingleton<LanguageServerBuilder>()
                        .AddOptions()
                        .AddLogging())
                    .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
                    .OnExit(_ =>
                    {
                        cts.Cancel();
                        return Task.CompletedTask;
                    }));

                var builder = server.GetRequiredService<LanguageServerBuilder>();

                await Task.WhenAll(
                    server.WaitForExit,
                    Task.Run(() => builder.Run(cts.Token)));
            }
        }
    }
}
