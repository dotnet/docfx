// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerHost
    {
        public static Task<LanguageServer> StartLanguageServer()
        {
            using var stdIn = Console.OpenStandardInput();
            using var stdOut = Console.OpenStandardOutput();

            return StartLanguageServer(PipeReader.Create(stdIn), PipeWriter.Create(stdOut));
        }

        public static Task<LanguageServer> StartLanguageServer(PipeReader input, PipeWriter output)
        {
            return LanguageServer.From(options => options
                .WithInput(input)
                .WithOutput(output)
                .ConfigureLogging(x => x.AddLanguageProtocolLogging())
                .WithHandler<TextDocumentHandler>()
                .WithServices(ConfigureServices)
                .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace))));
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging();
        }
    }
}
