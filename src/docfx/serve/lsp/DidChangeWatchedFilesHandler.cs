// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
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
        private readonly ILanguageServerNotificationListener _notificationListener;
        private readonly LanguageServerPackage _package;

        private readonly Container<FileSystemWatcher> _watcher = new Container<FileSystemWatcher>(
                new FileSystemWatcher() { GlobPattern = "**/*", Kind = WatchKind.Create | WatchKind.Delete | WatchKind.Change });

        public DidChangeWatchedFilesHandler(
            LanguageServerBuilder languageServerBuilder,
            ILanguageServerNotificationListener notificationListener,
            LanguageServerPackage package)
        {
            _languageServerBuilder = languageServerBuilder;
            _notificationListener = notificationListener;
            _package = package;
        }

        public Task<Unit> Handle(DidChangeWatchedFilesParams notification, CancellationToken cancellationToken)
        {
            var hasInScopedFileChange = notification.Changes.Any(
                @event =>
                {
                    if (@event.Uri.Scheme != "file")
                    {
                        return false;
                    }
                    var filePath = new PathString(@event.Uri.GetFileSystemPath());
                    if (!filePath.StartsWithPath(_package.BasePath, out var relativePath) || relativePath.Value.StartsWith(".git"))
                    {
                        return false;
                    }
                    return true;
                });
            if (hasInScopedFileChange)
            {
                if (notification.Changes.Any(
                    @event => @event.Type == FileChangeType.Created || @event.Type == FileChangeType.Deleted))
                {
                    _package.RefreshPackageFilesUpdateTime();
                }
                _languageServerBuilder.QueueBuild();
            }
            else
            {
                _notificationListener.OnNotificationHandled();
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
