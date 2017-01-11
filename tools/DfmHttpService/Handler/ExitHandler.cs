// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Threading.Tasks;

    public class ExitHandler : IHttpHandler
    {
        public bool IsSupport(ServiceContext context)
        {
            return context.Message.Name == CommandName.Exit;
        }

        public Task HandleAsync(ServiceContext context)
        {
            return Task.Run(() =>
            {
                Utility.ReplyExitResponse(context.HttpContext, "Dfm service exit");
                context.Server.Terminate();
            });
        }
    }
}