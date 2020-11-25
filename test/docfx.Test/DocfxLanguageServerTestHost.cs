// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc.Testing;
using OmniSharp.Extensions.LanguageProtocol.Testing;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.Docs.Build
{
    public class DocfxLanguageServerTestHost : LanguageServerTestBase
    {
        private static readonly string[] s_notificationsToListen = { "window/showMessage" };

        private readonly Channel<LanguageServerNotification> _notifications = Channel.CreateUnbounded<LanguageServerNotification>();
        private readonly Dictionary<string, string> _variables;
        private readonly Lazy<Task<ILanguageClient>> _client;

        public DocfxLanguageServerTestHost(Dictionary<string, string> variables)
            : base(new JsonRpcTestOptions())
        {
            _variables = variables;
            _client = new Lazy<Task<ILanguageClient>>(() => InitializeClient(x =>
            {
                x.WithCapability(new WorkspaceEditCapability()
                {
                    DocumentChanges = true,
                    FailureHandling = FailureHandlingKind.Undo,
                });

                foreach (var name in s_notificationsToListen)
                {
                    x.OnJsonNotification(name, @params =>
                    {
                        _notifications.Writer.TryWrite(new LanguageServerNotification(name, @params));
                    });
                }
            }));
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

        protected override (Stream clientOutput, Stream serverInput) SetupServer()
        {
            var clientPipe = new Pipe(TestOptions.DefaultPipeOptions);
            var serverPipe = new Pipe(TestOptions.DefaultPipeOptions);

            LanguageServerHost.StartLanguageServer(clientPipe.Reader.AsStream(), serverPipe.Writer.AsStream()).GetAwaiter().GetResult();

            return (serverPipe.Reader.AsStream(), clientPipe.Writer.AsStream());
        }
    }
}
