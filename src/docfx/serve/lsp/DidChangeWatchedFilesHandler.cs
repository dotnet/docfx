// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

using FileSystemWatcher = OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher;

namespace Microsoft.Docs.Build;

internal class DidChangeWatchedFilesHandler : IDidChangeWatchedFilesHandler
{
    private readonly LanguageServerBuilder _languageServerBuilder;
    private readonly ILanguageServerNotificationListener _notificationListener;
    private readonly LanguageServerPackage _package;

    private readonly Container<FileSystemWatcher> _watcher;

    public DidChangeWatchedFilesHandler(
        LanguageServerBuilder languageServerBuilder,
        ILanguageServerNotificationListener notificationListener,
        LanguageServerPackage package)
    {
        _languageServerBuilder = languageServerBuilder;
        _notificationListener = notificationListener;
        _package = package;
        _watcher = new Container<FileSystemWatcher>(
            new FileSystemWatcher() { GlobPattern = $"{_package.BasePath}/**/*", Kind = WatchKind.Create | WatchKind.Delete | WatchKind.Change });
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
                if (_package.BasePath.GetRelativePath(filePath).StartsWith(".git"))
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

    public DidChangeWatchedFilesRegistrationOptions GetRegistrationOptions(
        DidChangeWatchedFilesCapability capability, ClientCapabilities clientCapabilities)
    {
        return new()
        {
            Watchers = _watcher,
        };
    }
}
