// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Threading.Tasks;

    internal class TestServerAvailableHandler: IHttpHandler
    {
        public bool CanHandle(ServiceContext context)
        {
            return context.Message.Name == CommandName.TestServerAvailable;
        }

        public Task HandleAsync(ServiceContext context)
        {
            return Task.Run(() =>
            {
                Utility.ReplyNoContentResponse(context.HttpContext, "Server Available");
            });
        }
    }
}
