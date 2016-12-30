﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Net.Http;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    public class DfmHttpService
    {
        private volatile bool _keepGoing = true;
        private const int DefaultPort = 4001;
        private const string UrlPrefixTemplate = "http://localhost:{0}/";

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
                case CommandName.Preview:
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
                    ReplySuccessfulResponse(context, content, ContentType.Html);
                    return;
                case CommandName.GenerateTokenTree:
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
                    ReplySuccessfulResponse(context, tokenTree, ContentType.Json);
                    return;
                case CommandName.Exit:
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
            return string.Format(UrlPrefixTemplate, DefaultPort);
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
            if (!request.HasEntityBody || request.HttpMethod != HttpMethod.Post.ToString())
            {
                throw new HttpListenerException((int) HttpStatusCode.BadRequest, "No body in this request");
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

        private static void ReplySuccessfulResponse(HttpListenerContext context, string content, string contentType)
        {
            var response = context.Response;
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            response.ContentType = contentType;
            using (var write = response.OutputStream)
            {
                write.Write(buffer, 0, buffer.Length);
            }
            response.Close();
        }

        private static void ReplyClientErrorResponse(HttpListenerContext context, string message)
        {
            ReplyResponse(context, HttpStatusCode.BadRequest, message);
        }

        private static void ReplyServerErrorResponse(HttpListenerContext context, string message)
        {
            ReplyResponse(context, HttpStatusCode.InternalServerError, message);
        }

        private static void ReplyExitResponse(HttpListenerContext context, string message)
        {
            ReplyResponse(context, HttpStatusCode.NoContent, message);
        }

        private static void ReplyResponse(HttpListenerContext context, HttpStatusCode statusCode, string message)
        {
            var response = context.Response;
            response.StatusCode = (int) statusCode;
            response.StatusDescription = message;
            response.Close();
        }
    }
}