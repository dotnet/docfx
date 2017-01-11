// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class CompositeHandler : IHttpHandler
    {
        private readonly List<IHttpHandler> _handlers;

        public CompositeHandler()
        {
            _handlers = new List<IHttpHandler>();
        }

        public CompositeHandler(IEnumerable<IHttpHandler> handlers)
        {
            _handlers = handlers.ToList();
        }

        public void Add(IHttpHandler handler)
        {
            _handlers.Add(handler);
        }

        public void AddRange(IEnumerable<IHttpHandler> handlers)
        {
            _handlers.AddRange(handlers);
        }

        public bool IsSupport(ServiceContext context)
        {
            throw new System.NotImplementedException();
        }

        public async Task HandleAsync(ServiceContext context)
        {
            foreach (var handler in _handlers)
            {
                if (handler.IsSupport(context))
                {
                    await handler.HandleAsync(context);
                    return;
                }
            }
            Utility.ReplyClientErrorResponse(context.HttpContext, "No handler processes the context");
        }
    }
}