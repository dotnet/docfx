// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerTestHost
    {
        private static readonly string[] s_notificationsToListen = { "window/showMessage", "textDocument/publishDiagnostics" };

        private readonly Channel<LanguageServerNotification> _notifications = Channel.CreateUnbounded<LanguageServerNotification>();
        private readonly Dictionary<string, string> _variables;
        private readonly Lazy<Task<ILanguageClient>> _client;

        public LanguageServerTestHost(string workingDirectory, Dictionary<string, string> variables, Package package)
        {
            _variables = variables;
            _client = new Lazy<Task<ILanguageClient>>(() => InitializeClient(workingDirectory, package));
        }

        public async Task SendNotification(LanguageServerNotification notification)
        {
            var client = await _client.Value;
            client.SendNotification(notification.Method, TestUtility.ApplyVariables(notification.Params, _variables));
        }

        public async Task<LanguageServerNotification> GetExpectedNotification(string method)
        {
            var timeout = Debugger.IsAttached ? int.MaxValue : 60000;
            using var cts = new CancellationTokenSource(timeout);
            while (await _notifications.Reader.WaitToReadAsync(cts.Token))
            {
                var notification = await _notifications.Reader.ReadAsync();
                if (notification.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                {
                    return notification;
                }
            }
            return default;
        }

        private async Task<ILanguageClient> InitializeClient(string workingDirectory, Package package)
        {
            var clientPipe = new Pipe();
            var serverPipe = new Pipe();

            var client = LanguageClient.Create(options =>
            {
                options
                    .WithInput(serverPipe.Reader)
                    .WithOutput(clientPipe.Writer)
                    .WithCapability(new WorkspaceEditCapability()
                    {
                        DocumentChanges = true,
                        FailureHandling = FailureHandlingKind.Undo,
                    });

                foreach (var name in s_notificationsToListen)
                {
                    options.OnJsonNotification(name, @params =>
                    {
                        _notifications.Writer.TryWrite(new LanguageServerNotification(name, @params));
                    });
                }
            });

            await Task.WhenAll(
                client.Initialize(default),
                LanguageServerHost.StartLanguageServer(workingDirectory, new CommandLineOptions(), clientPipe.Reader, serverPipe.Writer, package));

            return client;
        }
    }
}
