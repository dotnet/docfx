// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading.Channels;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build;

internal class LanguageServerTestClient : ILanguageServerNotificationListener, IAsyncDisposable
{
    private static readonly JsonDiff s_languageServerJsonDiff = CreateLanguageServerJsonDiff();

    private readonly string _workingDirectory;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly Lazy<Task<ILanguageClient>> _client;
    private readonly TaskCompletionSource _serverInitializedTcs = new();
    private readonly Package _package;

    private readonly ConcurrentDictionary<string, JToken> _diagnostics = new();
    private readonly Channel<(JToken request, TaskCompletionSource<JToken> response)> _requestChannel =
        Channel.CreateUnbounded<(JToken, TaskCompletionSource<JToken>)>();

    private int _serverNotificationSent;
    private int _serverNotificationHandled;
    private int _clientNotificationSent;
    private int _clientNotificationReceived;
    private int _clientNotificationReceivedBeforeSync; // For expectNoNotification
    private TaskCompletionSource _notificationSync = new();

    public LanguageServerTestClient(string workingDirectory, Package package, bool noCache)
    {
        _workingDirectory = workingDirectory;
        _client = new(() => InitializeClient(workingDirectory, package, noCache));
        _package = package;
    }

    public async Task ProcessCommand(LanguageServerTestCommand command)
    {
        var client = await _client.Value;

        if (command.OpenFiles != null)
        {
            foreach (var (file, text) in command.OpenFiles)
            {
                BeforeSendNotification();

                client.DidOpenTextDocument(new() { TextDocument = new() { Uri = ToUri(file), Text = text } });
            }
        }
        else if (command.EditFiles != null)
        {
            foreach (var (file, text) in command.EditFiles)
            {
                BeforeSendNotification();

                client.DidChangeTextDocument(new()
                {
                    TextDocument = new() { Uri = ToUri(file) },
                    ContentChanges = new(new TextDocumentContentChangeEvent() { Text = text }),
                });
            }
        }
        else if (command.CreateFiles != null)
        {
            var fileEvents = new List<FileEvent>();
            foreach (var (file, text) in command.CreateFiles)
            {
                if (_package is MemoryPackage memoryPackage)
                {
                    memoryPackage.AddOrUpdate(new PathString(file), text ?? string.Empty);
                }
                else
                {
                    var fullPath = Path.Combine(_workingDirectory, file);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    File.Create(fullPath);
                    File.WriteAllText(fullPath, text ?? string.Empty);
                }
                fileEvents.Add(new FileEvent()
                {
                    Uri = ToUri(file),
                    Type = FileChangeType.Created,
                });
            }

            BeforeSendNotification();
            client.DidChangeWatchedFiles(new()
            {
                Changes = new(fileEvents),
            });
        }
        else if (command.EditFilesWithoutEditor != null)
        {
            var fileEvents = new List<FileEvent>();
            foreach (var (file, text) in command.EditFilesWithoutEditor)
            {
                if (_package is MemoryPackage memoryPackage)
                {
                    memoryPackage.AddOrUpdate(new PathString(file), text ?? string.Empty);
                }
                else
                {
                    var fullPath = Path.Combine(_workingDirectory, file);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    File.Create(fullPath);
                    File.WriteAllText(fullPath, text ?? string.Empty);
                }
                fileEvents.Add(new FileEvent()
                {
                    Uri = ToUri(file),
                    Type = FileChangeType.Changed,
                });
            }

            BeforeSendNotification();
            client.DidChangeWatchedFiles(new()
            {
                Changes = new(fileEvents),
            });
        }
        else if (command.CloseFiles != null)
        {
            foreach (var file in command.CloseFiles)
            {
                BeforeSendNotification();

                client.DidCloseTextDocument(new()
                {
                    TextDocument = new(ToUri(file)),
                });
            }
        }
        else if (command.DeleteFiles != null)
        {
            var fileEvents = new List<FileEvent>();
            foreach (var file in command.DeleteFiles)
            {
                if (_package is MemoryPackage memoryPackage)
                {
                    memoryPackage.RemoveFile(new PathString(file));
                }
                else
                {
                    File.Delete(Path.Combine(_workingDirectory, file));
                }
                fileEvents.Add(new FileEvent()
                {
                    Uri = ToUri(file),
                    Type = FileChangeType.Deleted,
                });
            }

            BeforeSendNotification();
            client.DidChangeWatchedFiles(new()
            {
                Changes = new(fileEvents),
            });
        }
        else if (command.ExpectDiagnostics != null)
        {
            await SynchronizeNotifications();

            s_languageServerJsonDiff.Verify(command.ExpectDiagnostics, _diagnostics);
        }
        else if (command.ExpectGetCredentialRequest != null)
        {
            var (request, response) = await _requestChannel.Reader.ReadAsync(CancelAfterTimeout());

            s_languageServerJsonDiff.Verify(command.ExpectGetCredentialRequest, request);
            response.SetResult(ApplyCredentialVariables(command.Response));
        }
        else if (command.ExpectNoNotification)
        {
            await SynchronizeNotifications();
            Assert.Equal(_clientNotificationReceivedBeforeSync, _clientNotificationReceived);
        }
        else
        {
            throw new NotSupportedException("Invalid language server test command");
        }
    }

    public async ValueTask DisposeAsync()
    {
        var client = await _client.Value;
        await client.Shutdown();
        _serverCts.Cancel();
    }

    void ILanguageServerNotificationListener.OnNotificationSent()
    {
        Interlocked.Increment(ref _serverNotificationSent);
    }

    void ILanguageServerNotificationListener.OnNotificationHandled()
    {
        if (Interlocked.Increment(ref _serverNotificationHandled) == _clientNotificationSent &&
            _clientNotificationReceived == _serverNotificationSent)
        {
            _notificationSync.TrySetResult();
        }
    }

    void ILanguageServerNotificationListener.OnException(Exception ex)
    {
        _notificationSync.TrySetException(ex);
    }

    void ILanguageServerNotificationListener.OnInitialized()
    {
        _serverInitializedTcs.TrySetResult();
    }

    private CancellationToken CancelAfterTimeout()
        => new CancellationTokenSource(60000).Token;

    private JToken ApplyCredentialVariables(JToken @params)
    {
        return TestUtility.ApplyVariables(
            @params,
            new Dictionary<string, string>
            {
                    { "DOCS_OPS_TOKEN", Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN") },
            });
    }

    private void BeforeSendNotification()
    {
        Interlocked.Increment(ref _clientNotificationSent);
        _notificationSync = new TaskCompletionSource();
        _clientNotificationReceivedBeforeSync = _clientNotificationReceived;
    }

    private async Task SynchronizeNotifications()
    {
        using (CancelAfterTimeout().Register(() =>
        {
            Console.WriteLine("2022517 Timeout");
            var cancelRes = _notificationSync.TrySetCanceled();
            Console.WriteLine("2022517 cancel result: " + cancelRes);
            }))
        {
            Console.WriteLine("2022517 wait for _notificationSync.Task");
            await _notificationSync.Task;
            Console.WriteLine("2022517 finish _notificationSync.Task");
        }
    }

    private void OnNotification()
    {
        if (Interlocked.Increment(ref _clientNotificationReceived) == _serverNotificationSent &&
            _clientNotificationSent == _serverNotificationHandled)
        {
            _notificationSync.TrySetResult();
        }
    }

    private DocumentUri ToUri(string file)
    {
        return DocumentUri.File(Path.Combine(_workingDirectory, file));
    }

    private string ToFile(DocumentUri uri)
    {
        var path = DocumentUri.GetFileSystemPath(uri);
        return path is null ? uri.ToString() : PathUtility.NormalizeFile(Path.GetRelativePath(_workingDirectory, path));
    }

    private async Task<ILanguageClient> InitializeClient(string workingDirectory, Package package, bool noCache)
    {
        var clientPipe = new Pipe();
        var serverPipe = new Pipe();

        var client = LanguageClient.Create(options => options
            .WithInput(serverPipe.Reader)
            .WithOutput(clientPipe.Writer)
            .WithCapability(new WorkspaceEditCapability()
            {
                DocumentChanges = true,
                FailureHandling = FailureHandlingKind.Undo,
            })
            .OnLogMessage(message =>
            {
                Console.WriteLine($"[LanguageServerTestClient] on message: (${message.Type}){message.Message}");
                if (message.Type == MessageType.Error || message.Type == MessageType.Warning)
                {
                    _notificationSync.TrySetException(new InvalidOperationException(message.Message));
                }
            })
            .OnRequest("docfx/getCredential", async (GetCredentialParams @params) =>
            {
                var response = new TaskCompletionSource<JToken>();
                _requestChannel.Writer.TryWrite((JToken.Parse(JsonUtility.Serialize(@params)), response));

                return await response.Task;
            })
            .OnPublishDiagnostics(item =>
            {
                _diagnostics[ToFile(item.Uri)] = JToken.FromObject(item.Diagnostics, JsonUtility.Serializer);
                OnNotification();
            }));

        Task.Run(
            () => LanguageServerHost.RunLanguageServer(
            new() { Directory = workingDirectory, NoCache = noCache },
            clientPipe.Reader,
            serverPipe.Writer,
            package,
            notificationListener: this,
            _serverCts.Token)).GetAwaiter();

        await client.Initialize(CancelAfterTimeout());
        await _serverInitializedTcs.Task;
        return client;
    }

    private static JsonDiff CreateLanguageServerJsonDiff()
    {
        return new JsonDiffBuilder()
            .UseAdditionalProperties()
            .UseNegate()
            .UseWildcard()
            .Build();
    }
}
