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
    internal class DidChangeWatcherFilesHandler : IDidChangeWatchedFilesHandler
    {
        private readonly LanguageServerBuilder _languageServerBuilder;
        private readonly Container<FileSystemWatcher> _watcher = new Container<FileSystemWatcher>(
                new FileSystemWatcher() { GlobPattern = "**/*.{md,yml,json}", Kind = WatchKind.Create },
                new FileSystemWatcher() { GlobPattern = "**/*.{md,yml,json}", Kind = WatchKind.Delete });

        public DidChangeWatcherFilesHandler(
            LanguageServerBuilder languageServerBuilder)
        {
            _languageServerBuilder = languageServerBuilder;
        }

        public Task<Unit> Handle(DidChangeWatchedFilesParams notification, CancellationToken cancellationToken)
        {
            foreach (var change in notification.Changes)
            {
                switch (change.Type)
                {
                    case FileChangeType.Created:
                        {
                            _languageServerBuilder.QueueBuild();
                            break;
                        }
                    case FileChangeType.Deleted:
                        {
                            _languageServerBuilder.QueueBuild();
                            break;
                        }
                }
            }
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
