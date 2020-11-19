// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Threading;
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
        private readonly LanguageServerOptions _options;
        private IServiceCollection? _services;

        internal LanguageServer? Server { get; set; }

        public LanguageServerHost(
            Stream input,
            Stream output)
        {
            _options = new LanguageServerOptions()
                .WithInput(input)
                .WithOutput(output)
                .ConfigureLogging(
                    x => x.AddLanguageProtocolLogging())
                .WithHandler<TextDocumentHandler>()
                .WithServices(ConfigureServices)
                .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
                .OnInitialize(InitializeAsync);
        }

        public async Task Start()
        {
            Server = await LanguageServer.From(_options);
        }

        private Task InitializeAsync(ILanguageServer server, InitializeParams request, CancellationToken cancellationToken)
        {
            Debug.Assert(_services != null);
            _services.AddSingleton(server);
            var loggerFactory = server.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<LanguageServerHost>();
            logger.LogTrace("Initializing...");

            return Task.CompletedTask;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            _services = services;
            _services.AddOptions();
            _services.AddLogging();
        }
    }
}
