// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.General;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerHost
    {
        public static async Task RunLanguageServer(CommandLineOptions options, Package? package = null)
        {
            var stdin = Console.OpenStandardInput();
            var stdout = Console.OpenStandardOutput();

            var logPipe = new Pipe();
            using var logWriter = new StreamWriter(logPipe.Writer.AsStream());
            using var logReader = new StreamReader(logPipe.Reader.AsStream());

            Console.SetOut(logWriter);

            await RunLanguageServer(options, PipeReader.Create(stdin), PipeWriter.Create(stdout), package, logReader);
        }

        public static async Task RunLanguageServer(
            CommandLineOptions commandLineOptions,
            PipeReader input,
            PipeWriter output,
            Package? package = null,
            TextReader? logReader = null,
            ILanguageServerNotificationListener? notificationListener = null)
        {
            using var cts = new CancellationTokenSource();

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
                Task.Run(() => builder.Run(cts.Token)),
                Task.Run(() => StreamLogs(server, logReader ?? TextReader.Null, cts.Token)));
        }

        private static void StreamLogs(LanguageServer server, TextReader reader, CancellationToken cancellationToken)
        {
            while (reader.ReadLine() is string line && !cancellationToken.IsCancellationRequested)
            {
                server.Window.Log(line);
            }
        }
    }
}
