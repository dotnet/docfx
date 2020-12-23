// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerProgressReporterFactory
    {
        private readonly ILanguageServer _languageServer;

        public LanguageServerProgressReporterFactory(ILanguageServer languageServer)
        {
            _languageServer = languageServer.GetRequiredService<ILanguageServer>();
        }

        public async Task<LanguageServerProgressReporter> CreateReporter()
            => new LanguageServerProgressReporter(await _languageServer.WorkDoneManager.Create(new WorkDoneProgressBegin()));
    }
}
