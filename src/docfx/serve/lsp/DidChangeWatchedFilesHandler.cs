// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace Microsoft.Docs.Build
{
    internal class DidChangeWatchedFilesHandler : IDidChangeWatchedFilesHandler
    {
        private readonly LanguageServerBuilder _languageServerBuilder;
        private readonly LanguageServerPackage _package;

        private readonly Container<FileSystemWatcher> _watcher = new Container<FileSystemWatcher>(
                new FileSystemWatcher() { GlobPattern = "**/*", Kind = WatchKind.Create | WatchKind.Delete });

        public DidChangeWatchedFilesHandler(
            LanguageServerBuilder languageServerBuilder,
            LanguageServerPackage package)
        {
            _languageServerBuilder = languageServerBuilder;
            _package = package;
        }

        public Task<Unit> Handle(DidChangeWatchedFilesParams notification, CancellationToken cancellationToken)
        {
            _package.RefreshPackageFilesUpdateTime();
            _languageServerBuilder.QueueBuild();
            return Unit.Task;
        }

        DidChangeWatchedFilesRegistrationOptions IRegistration<DidChangeWatchedFilesRegistrationOptions>.GetRegistrationOptions()
        {
            return new DidChangeWatchedFilesRegistrationOptions()
            {
                Watchers = _watcher,
            };
        }

        public void SetCapability(DidChangeWatchedFilesCapability capability)
        {
        }
    }
}
