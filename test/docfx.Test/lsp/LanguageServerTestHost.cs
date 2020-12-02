// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
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
    public class LanguageServerTestHost
    {
        private static readonly string[] s_notificationsToListen = { "window/showMessage" };

        private readonly Channel<LanguageServerNotification> _notifications = Channel.CreateUnbounded<LanguageServerNotification>();
        private readonly Dictionary<string, string> _variables;
        private readonly Lazy<Task<ILanguageClient>> _client;

        public LanguageServerTestHost(Dictionary<string, string> variables)
        {
            _variables = variables;
            _client = new Lazy<Task<ILanguageClient>>(InitializeClient);
        }

        public async Task SendNotification(LanguageServerNotification notification)
        {
            var client = await _client.Value;
            client.SendNotification(notification.Method, TestUtility.ApplyVariables(notification.Params, _variables));
        }

        public async Task<LanguageServerNotification> GetExpectedNotification(string method)
        {
            using var cts = new CancellationTokenSource(60000);

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

        private async Task<ILanguageClient> InitializeClient()
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
                LanguageServerHost.StartLanguageServer(clientPipe.Reader, serverPipe.Writer));

            return client;
        }
    }
}
