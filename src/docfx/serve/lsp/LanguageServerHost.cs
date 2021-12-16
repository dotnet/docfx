// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.Docs.Build;

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
                        .AddLogging());
                options.OnUnhandledException = (e) =>
                {
                    notificationListener?.OnException(e);
                    Telemetry.TrackException(e);
                };
            },
            cancellationToken);

        await server.WasStarted;
        notificationListener?.OnInitialized();

        await server.GetRequiredService<LanguageServerBuilder>().Run(cancellationToken);
    }
}
