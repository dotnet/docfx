// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Threading.Tasks;

    internal class ExitHandler : IHttpHandler
    {
        public bool CanHandle(ServiceContext context)
        {
            return context.Message.Name == CommandName.Exit;
        }

        public Task HandleAsync(ServiceContext context)
        {
            return Task.Run(() =>
            {
                Utility.ReplyNoContentResponse(context.HttpContext, "Dfm service exits");
                context.Server.Terminate();
            });
        }
    }
}
