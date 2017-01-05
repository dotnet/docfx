// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    public class DfmPreviewHandler : IHttpHandler
    {
        public bool IsSupport(HttpContext wrapper)
        {
            return wrapper.Message.Name == CommandName.Preview;
        }

        public Task HandleAsync(HttpContext wrapper)
        {
            return Task.Run(() =>
            {
                string content;
                try
                {
                    content = Preview(wrapper.Message.Documentation, wrapper.Message.FilePath, wrapper.Message.WorkspacePath);
                }
                catch (Exception ex)
                {
                    Utility.ReplyServerErrorResponse(wrapper.Context, ex.Message);
                    return;
                }
                Utility.ReplySuccessfulResponse(wrapper.Context, content, ContentType.Html);
            });
        }

        private static string Preview(string documentation, string filePath, string workspacePath = null)
        {
            var provider = new DfmServiceProvider();
            var service = provider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = workspacePath });

            return service.Markup(documentation, filePath).Html;
        }
    }
}