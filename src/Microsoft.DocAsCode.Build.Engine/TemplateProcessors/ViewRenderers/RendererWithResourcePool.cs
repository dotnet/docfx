// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    internal class RendererWithResourcePool : ITemplateRenderer
    {
        private readonly ITemplateRenderer _inner;
        private readonly ResourcePoolManager<ITemplateRenderer> _rendererPool;
        public RendererWithResourcePool(Func<ITemplateRenderer> creater, int maxParallelism)
        {
            _rendererPool = ResourcePool.Create(creater, maxParallelism);

            using (var lease = _rendererPool.Rent())
            {
                _inner = lease.Resource;
            }
        }

        public IEnumerable<string> Dependencies => _inner?.Dependencies;

        public string Raw => _inner?.Raw;

        public string Render(object model)
        {
            if (model == null || _inner == null)
            {
                return null;
            }

            using (var lease = _rendererPool.Rent())
            {
                return lease.Resource.Render(model);
            }
        }
    }
}
