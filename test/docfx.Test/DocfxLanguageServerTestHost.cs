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
        private readonly Channel<LanguageServerNotification> _notificationsChannel =
           Channel.CreateUnbounded<LanguageServerNotification>();

        private readonly Dictionary<string, string> _variables;

        private Task startUpTask;

        private static readonly string[] s_notificationsToListen = { "window/showMessage" };

        protected ILanguageClient Client { get; private set; }

        public DocfxLanguageServerTestHost(Dictionary<string, string> variables)
            : base(new JsonRpcTestOptions())
        {
            _variables = variables;
        }

        public async Task InitializeAsync()
        {
            Client = await InitializeClient(x =>
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
                        _notificationsChannel.Writer.TryWrite(new LanguageServerNotification(name, @params));
                    });
                }
            });

            await startUpTask;
        }

        public void SendNotification(LanguageServerNotification notification)
        {
            Client.SendNotification(notification.Method, TestUtility.ApplyVariables(notification.Params, _variables));
        }

        public async Task<LanguageServerNotification> GetExpectedNotification(string method)
        {
            using var cts = new CancellationTokenSource(10000);
            while (true)
            {
                var notification = await _notificationsChannel.Reader.ReadAsync(cts.Token).AsTask();
                if (notification.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                {
                    return notification;
                }
            }
        }

        protected override (Stream clientOutput, Stream serverInput) SetupServer()
        {
            var clientPipe = new Pipe(TestOptions.DefaultPipeOptions);
            var serverPipe = new Pipe(TestOptions.DefaultPipeOptions);

            startUpTask = LanguageServerHost.StartLanguageServer(clientPipe.Reader.AsStream(), serverPipe.Writer.AsStream());

            return (serverPipe.Reader.AsStream(), clientPipe.Writer.AsStream());
        }
    }
}
