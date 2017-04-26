// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal class DeleteTempPreviewFileHandler: IHttpHandler
    {
        public bool CanHandle(ServiceContext context)
        {
            return context.Message.Name == CommandName.DeleteTempPreviewFile;
        }

        public Task HandleAsync(ServiceContext context)
        {
            return Task.Run(() =>
            {
                try
                {
                    string previewFilePath = new Uri(context.Message.TempPreviewFilePath).LocalPath;
                    File.Delete(previewFilePath);
                    Utility.ReplyNoContentResponse(context.HttpContext, "Delete temp preview file successfully");
                }
                catch (Exception ex)
                {
                    Utility.ReplyServerErrorResponse(context.HttpContext, ex.Message);
                }
            });
        }
    }
}
