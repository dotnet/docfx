// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc.Testing;
using OmniSharp.Extensions.LanguageProtocol.Testing;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace Microsoft.Docs.Build
{
    public class DocfxLanguageServerTestHost : LanguageServerTestBase
    {
        private readonly ConcurrentBag<LanguageServerNotification> _notifications =
           new ConcurrentBag<LanguageServerNotification>();

        private LanguageServerHost _host;
        private Task startUpTask;

        protected ILanguageClient Client { get; private set; }

        protected ILanguageServer Server => _host.Server;

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

                x.OnShowMessage(@params =>
                {
                    _notifications.Add(new LanguageServerNotification("window/showMessage", JsonUtility.Serialize(@params)));
                });
            });

            await startUpTask;
        }

        public void SendNotifications(List<LanguageServerNotification> notifications)
        {
            _notifications.Clear();
            foreach (var item in notifications)
            {
                Client.SendNotification(item.Method, JsonUtility.DeserializeData<JObject>(item.Params, null));
            }
        }

        public async Task<List<LanguageServerNotification>> GetExpectedNotifications(int count)
        {
            var waitTask = Task.Run(async () =>
            {
                if (_notifications.Count < count)
                {
                    await Task.Delay(100);
                }
            });
            await Task.WhenAny(waitTask, Task.Delay(10000));
            return _notifications.ToList();
        }

        protected override (Stream clientOutput, Stream serverInput) SetupServer()
        {
            var clientPipe = new Pipe(TestOptions.DefaultPipeOptions);
            var serverPipe = new Pipe(TestOptions.DefaultPipeOptions);

            _host = new LanguageServerHost(clientPipe.Reader.AsStream(), serverPipe.Writer.AsStream());
            startUpTask = _host.Start();

            return (serverPipe.Reader.AsStream(), clientPipe.Writer.AsStream());
        }
    }
}
