// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.Net;
    using System.Threading;

    using Microsoft.DocAsCode.Common;

    public class DfmHttpServer
    {
        private const int DefaultPort = 4001;
        private const string UrlPrefixTemplate = "http://localhost:{0}/";
        private readonly HttpListener _listener;
        private readonly IHttpHandler _handler;
        private CountdownEvent _processing;

        // TODO: make UrlPrefix configurable
        private static string UrlPrefix => string.Format(UrlPrefixTemplate, DefaultPort);

        public DfmHttpServer(IHttpHandler handler)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(UrlPrefix);
            _handler = handler;
        }

        public void Start()
        {
            _listener.Start();
            _processing = new CountdownEvent(1);
            RunServerCore();
        }

        public void Stop()
        {
            _listener.Stop();
            _processing.Signal();
        }

        public void WaitForExit()
        {
            _processing.Wait();
            _listener.Close();
            _processing = null;
        }

        private void RunServerCore()
        {
            _listener.BeginGetContext(ar =>
            {
                try
                {
                    var context = _listener.EndGetContext(ar);
                    RunServerCore();
                    try
                    {
                        var wrapper = new HttpContext { Context = context, Server = this };
                        _handler.HandleAsync(wrapper).Wait();
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var ex in ae.InnerExceptions)
                        {
                            if (ex is HandlerClientException)
                            {
                                Utility.ReplyClientErrorResponse(context, $"Error occurs while handling context, {ex.Message}");
                            }
                            else if (ex is HandlerServerException)
                            {
                                Utility.ReplyServerErrorResponse(context, $"Error occurs while handling context, {ex.Message}");
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                catch (HttpListenerException ex)
                {
                    Logger.LogError($"Error occurs while processing context, {ex.Message}");
                }
            }, null);
        }
    }
}