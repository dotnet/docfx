// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    internal class DfmTokenTreeHandler : IHttpHandler
    {
        private readonly IMarkdownService _service;

        public DfmTokenTreeHandler(string workspacePath)
        {
            DfmJsonTokenTreeServiceProvider provider = new DfmJsonTokenTreeServiceProvider();
            _service = provider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = workspacePath });
        }

        public bool CanHandle(ServiceContext context)
        {
            return context.Message.Name == CommandName.GenerateTokenTree;
        }

        public Task HandleAsync(ServiceContext context)
        {
            return Task.Run(() =>
            {
                try
                {
                    var tokenTree = GenerateTokenTree(context.Message.MarkdownContent, context.Message.RelativePath, context.Message.WorkspacePath);
                    Utility.ReplySuccessfulResponse(context.HttpContext, tokenTree, ContentType.Json);
                }
                catch (Exception ex)
                {
                    Utility.ReplyServerErrorResponse(context.HttpContext, ex.Message);
                }
            });
        }

        private string GenerateTokenTree(string documentation, string filePath, string workspacePath = null)
        {
            return _service.Markup(documentation, filePath).Html;
        }
    }
}
