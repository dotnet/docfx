// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class CompositeHandler : IHttpHandler
    {
        private readonly List<IHttpHandler> _handlers;

        public CompositeHandler() : this(new List<IHttpHandler>())
        {
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

        public bool CanHandle(ServiceContext context)
        {
            throw new System.NotSupportedException();
        }

        public async Task HandleAsync(ServiceContext context)
        {
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(context))
                {
                    await handler.HandleAsync(context);
                    return;
                }
            }
            Utility.ReplyClientErrorResponse(context.HttpContext, "No handler processes the context");
        }
    }
}