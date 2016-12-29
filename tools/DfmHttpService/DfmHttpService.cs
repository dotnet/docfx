// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    public class DfmHttpService
    {
        private bool _keepGoing = true;
        private const string UrlPrefix = "http://localhost:4001/";

        public void StartService(string urlPrefix)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(urlPrefix);
            listener.Start();

            while (_keepGoing)
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(HandleRequest, context);
            }

            listener.Stop();
            listener.Close();
        }

        private void HandleRequest(object ctx)
        {
            // TODO: Add log for request information
            var context = (HttpListenerContext) ctx;
            var request = context.Request;

            CommandMessage command;
            try
            {
                command = GetCommandMessage(request);
            }
            catch (Exception ex)
            {
                ReplyClientErrorResponse(context, ex.Message);
                return;
            }

            switch (command.Name)
            {
                case Constants.PreviewCommand:
                    string content;
                    try
                    {
                        content = Preview(command.Documentation, command.FilePath, command.WorkspacePath);
                    }
                    catch (Exception ex)
                    {
                        ReplyServerErrorResponse(context, ex.Message);
                        return;
                    }
                    ReplySuccessfulResponse(context, content);
                    return;
                case Constants.GenerateTokenTreeCommand:
                    string tokenTree;
                    try
                    {
                        tokenTree = GenerateTokenTree(command.Documentation, command.FilePath, command.WorkspacePath);
                    }
                    catch (Exception ex)
                    {
                        ReplyServerErrorResponse(context, ex.Message);
                        return;
                    }
                    ReplySuccessfulResponse(context, tokenTree);
                    return;
                case Constants.ExitServiceCommand:
                    _keepGoing = false;
                    ReplyExitResponse(context, "Dfm service exit");
                    return;
                default:
                    ReplyClientErrorResponse(context, "Can't find the name of command");
                    return;
            }
        }

        public static string GetAvailablePrefix()
        {
            // TODO: generate new random port if default port is not avaliable
            return UrlPrefix;
        }

        private static string GenerateTokenTree(string documentation, string filePath, string workspacePath = null)
        {
            var provider = new DfmJsonTokenTreeServiceProvider();
            var service = provider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = workspacePath});

            return service.Markup(documentation, filePath).Html;
        }

        private static string Preview(string documentation, string filePath, string workspacePath = null)
        {
            var provider = new DfmServiceProvider();
            var service = provider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = workspacePath });

            return service.Markup(documentation, filePath).Html;
        }

        private static CommandMessage GetCommandMessage(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                throw new HttpListenerException(Constants.ClientErrorStatusCode, "No body in this request");
            }

            string content;
            using (var body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    content = reader.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject<CommandMessage>(content);
        }

        private static void ReplySuccessfulResponse(HttpListenerContext context, string content)
        {
            var response = context.Response;
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            using (var write = response.OutputStream)
            {
                write.Write(buffer, 0, buffer.Length);
            }
            response.Close();
        }

        private static void ReplyClientErrorResponse(HttpListenerContext context, string message)
        {
            ReplayResponse(context, Constants.ClientErrorStatusCode, message);
        }

        private static void ReplyServerErrorResponse(HttpListenerContext context, string message)
        {
            ReplayResponse(context, Constants.ServerErrorStatusCode, message);
        }

        private static void ReplyExitResponse(HttpListenerContext context, string message)
        {
            ReplayResponse(context, Constants.ServiceExitStatusCode, message);
        }

        private static void ReplayResponse(HttpListenerContext context, int statusCode, string message)
        {
            var response = context.Response;
            response.StatusCode = statusCode;
            response.StatusDescription = message;
            response.Close();
        }
    }
}