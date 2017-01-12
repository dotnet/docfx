// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    internal class DfmPreviewHandler : IHttpHandler
    {
        private readonly DfmServiceProvider _provider = new DfmServiceProvider();

        public bool CanHandle(ServiceContext context)
        {
            return context.Message.Name == CommandName.Preview;
        }

        public Task HandleAsync(ServiceContext context)
        {
            return Task.Run(() =>
            {
                string content;
                try
                {
                    content = Preview(context.Message.Documentation, context.Message.FilePath, context.Message.WorkspacePath);
                    Utility.ReplySuccessfulResponse(context.HttpContext, content, ContentType.Html);
                }
                catch (Exception ex)
                {
                    Utility.ReplyServerErrorResponse(context.HttpContext, ex.Message);
                }
            });
        }

        private string Preview(string documentation, string filePath, string workspacePath = null)
        {
            var service = _provider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = workspacePath });

            return service.Markup(documentation, filePath).Html;
        }
    }
}