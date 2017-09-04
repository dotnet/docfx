// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    internal class RendererWithResourcePool : ITemplateRenderer
    {
        private readonly ResourcePoolManager<ITemplateRenderer> _rendererPool;
        public RendererWithResourcePool(Func<ITemplateRenderer> creater, int maxParallelism)
        {
            _rendererPool = ResourcePool.Create(creater, maxParallelism);

            using (var lease = _rendererPool.Rent())
            {
                var inner = lease.Resource;
                Raw = inner.Raw;
                Dependencies = inner.Dependencies;
                Path = inner.Path;
                Name = inner.Name;
            }
        }

        public IEnumerable<string> Dependencies { get; }

        public string Raw { get; }

        public string Path { get; }

        public string Name { get; }

        public string Render(object model)
        {
            if (model == null)
            {
                return null;
            }

            using (var lease = _rendererPool.Rent())
            {
                return lease.Resource?.Render(model);
            }
        }
    }
}
