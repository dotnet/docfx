// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerHost
    {
        public static Task<LanguageServer> StartLanguageServer(string workingDirectory, CommandLineOptions options, Package? package = null)
        {
            var stdIn = Console.OpenStandardInput();
            var stdOut = Console.OpenStandardOutput();

            return StartLanguageServer(workingDirectory, options, PipeReader.Create(stdIn), PipeWriter.Create(stdOut), package);
        }

        public static Task<LanguageServer> StartLanguageServer(
            string workingDirectory, CommandLineOptions commandLineOptions, PipeReader input, PipeWriter output, Package? package = null)
        {
            commandLineOptions.DryRun = true;
            ResetConsoleOutput();

            return LanguageServer.From(options => options
                .WithInput(input)
                .WithOutput(output)
                .ConfigureLogging(x => x.AddLanguageProtocolLogging())
                .WithHandler<TextDocumentHandler>()
                .WithServices(services =>
                {
                    services.AddSingleton(Channel.CreateUnbounded<FileActionEvent>());
                })
                .WithServices(ConfigureServices)
                .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
                .OnInitialize(Initialize));

            Task Initialize(ILanguageServer server, InitializeParams request, CancellationToken cancellationToken)
            {
                var serviceProvider = server.Services;
                var eventChannel = serviceProvider.GetService<Channel<FileActionEvent>>();

                if (package == null)
                {
                    package = new LocalPackage(workingDirectory);
                }
                _ = new LanguageServerBuilder(workingDirectory, commandLineOptions, eventChannel, server, package).StartAsync();
                return Task.CompletedTask;
            }
        }

        private static void ResetConsoleOutput()
        {
            // TODO: redirect the console output to client through LSP
            Console.SetOut(StreamWriter.Null);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging();
        }
    }
}
