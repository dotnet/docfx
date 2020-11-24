// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
        private readonly ConcurrentQueue<LanguageServerNotification> _notifications =
           new ConcurrentQueue<LanguageServerNotification>();

        private Task startUpTask;

        private static readonly string[] s_notificationsToListen = { "window/showMessage" };

        protected ILanguageClient Client { get; private set; }

        public DocfxLanguageServerTestHost()
            : base(new JsonRpcTestOptions())
        {
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
                        _notifications.Enqueue(new LanguageServerNotification(name, JsonUtility.Serialize(@params)));
                    });
                }
            });

            await startUpTask;
        }

        public void SendNotification(LanguageServerNotification notification)
        {
            Client.SendNotification(notification.Method, JsonUtility.DeserializeData<JObject>(notification.Params, null));
        }

        public async Task<LanguageServerNotification> GetExpectedNotification(string method)
        {
            LanguageServerNotification expectedNotification = null;
            var waitTask = Task.Run(async () =>
            {
                while (true)
                {
                    while (_notifications.TryDequeue(out var notification))
                    {
                        if (notification.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                        {
                            expectedNotification = notification;
                            return;
                        }
                    }
                    await Task.Delay(100);
                }
            });
            await Task.WhenAny(waitTask, Task.Delay(10000));
            return expectedNotification;
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
