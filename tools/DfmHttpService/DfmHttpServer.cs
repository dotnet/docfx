﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.Net;
    using System.Threading;

    internal class DfmHttpServer
    {
        private const int DefaultPort = 4001;
        private const string UrlPrefixTemplate = "http://localhost:{0}/";
        private readonly HttpListener _listener = new HttpListener();
        private ManualResetEvent _processing = new ManualResetEvent(true);
        private readonly IHttpHandler _handler;
        private int _status;

        // TODO: make UrlPrefix configurable
        private static string UrlPrefix => string.Format(UrlPrefixTemplate, DefaultPort);

        public DfmHttpServer(IHttpHandler handler)
        {
            _listener.Prefixes.Add(UrlPrefix);
            _handler = handler;
        }

        public void Start()
        {
            var status = Interlocked.CompareExchange(ref _status, 1, 0);
            if (status != 0)
            {
                return;
            }

            _listener.Start();
            _processing.Reset();
            RunServerCore();
        }

        public void Terminate()
        {
            var status = Interlocked.CompareExchange(ref _status, 0, 1);
            if (status != 1)
            {
                return;
            }
            _listener.Stop();
            _processing.Set();
        }

        public void WaitForExit()
        {
            _processing.WaitOne();
            _listener.Close();
            _processing = null;
        }

        private void RunServerCore()
        {
            _listener.BeginGetContext(async ar =>
            {
                try
                {
                    var httpContext = _listener.EndGetContext(ar);
                    RunServerCore();
                    try
                    {
                        var context = new ServiceContext { HttpContext = httpContext, Server = this };
                        await _handler.HandleAsync(context);
                    }
                    catch (HandlerClientException ex)
                    {
                        Utility.ReplyClientErrorResponse(httpContext, $"Error occurs while handling context, {ex.Message}");
                    }
                    catch (HandlerServerException ex)
                    {
                        Utility.ReplyServerErrorResponse(httpContext, $"Error occurs while handling context, {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Utility.ReplyServerErrorResponse(httpContext, $"Error occurs, {ex.ToString()}");
                    }
                }
                catch (HttpListenerException)
                {
                }
            }, null);
        }
    }
}