// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Threading.Tasks;

    public class ExitHandler : IHttpHandler
    {
        public bool IsSupport(HttpContext wrapper)
        {
            return wrapper.Message.Name == CommandName.Exit;
        }

        public Task HandleAsync(HttpContext wrapper)
        {
            return Task.Run(() =>
            {
                Utility.ReplyExitResponse(wrapper.Context, "Dfm service exit");
                wrapper.Server.Stop();
            });
        }
    }
}