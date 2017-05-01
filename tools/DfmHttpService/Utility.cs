// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;

    using Newtonsoft.Json;

    internal class Utility
    {
        public static CommandMessage GetCommandMessage(HttpListenerContext context)
        {
            if (context == null)
            {
                throw new HandlerServerException($"{nameof(context)} can't be null");
            }

            var request = context.Request;
            if (request == null)
            {
                throw new HandlerServerException($"{nameof(request)} can't be null");
            }

            if (request.HttpMethod != HttpMethod.Post.ToString())
            {
                throw new HandlerClientException("Only POST method allowed.");
            }

            if (!request.HasEntityBody)
            {
                throw new HandlerClientException("No body in this request");
            }

            string content;
            using (var body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    content = reader.ReadToEnd();
                }
            }

            CommandMessage message;
            try
            {
                message = JsonConvert.DeserializeObject<CommandMessage>(content);
            }
            catch (JsonException ex)
            {
                throw new HandlerClientException($"Error happened while parsing body in request, {ex.Message}");
            }
            return message;
        }

        public static void ReplySuccessfulResponse(HttpListenerContext context, string content, string contentType)
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

        public static void ReplyClientErrorResponse(HttpListenerContext context, string message)
        {
            ReplyResponse(context, HttpStatusCode.BadRequest, message);
        }

        public static void ReplyServerErrorResponse(HttpListenerContext context, string message)
        {
            ReplyResponse(context, HttpStatusCode.InternalServerError, message);
        }

        public static void ReplyNoContentResponse(HttpListenerContext context, string message)
        {
            ReplyResponse(context, HttpStatusCode.NoContent, message);
        }

        public static void ReplyResponse(HttpListenerContext context, HttpStatusCode statusCode, string message)
        {
            var response = context.Response;
            response.StatusCode = (int)statusCode;
            response.StatusDescription = message;
            response.Close();
        }
    }
}
