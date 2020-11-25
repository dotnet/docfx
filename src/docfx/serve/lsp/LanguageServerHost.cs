// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerHost
    {
        public static Task<LanguageServer> StartLanguageServer(Stream input, Stream output)
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
