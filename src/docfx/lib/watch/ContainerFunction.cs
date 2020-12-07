// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class ContainerFunction : IFunction
    {
        private readonly HashSet<IFunction> _children = new HashSet<IFunction>();

        public bool HasChildren => _children.Count > 0;

        public void AddChild(IFunction childFunction)
        {
            lock (_children)
            {
                _children.Add(childFunction);
            }
        }

        public bool HasChanged(int activityId)
        {
            lock (_children)
            {
                if (_children.Count < 10)
                {
                    foreach (var child in _children)
                    {
                        if (child.HasChanged(activityId))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    var result = false;
                    Parallel.ForEach(_children, (child, loop) =>
                    {
                        if (child.HasChanged(activityId))
                        {
                            result = true;
                            loop.Break();
                        }
                    });
                    return result;
                }
            }
        }
    }
}
