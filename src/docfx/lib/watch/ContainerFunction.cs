// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class ContainerFunction : IFunction
    {
        private readonly List<IFunction> _children = new List<IFunction>();

        private volatile int _activityId = Watcher.GetActivityId();
        private volatile bool _hasChanged;

        public void AddChild(IFunction childFunction)
        {
            lock (_children)
            {
                _children.Add(childFunction);
            }
        }

        public bool MayChange()
        {
            return _children.Count > 0;
        }

        public bool HasChanged()
        {
            var currentActivityId = Watcher.GetActivityId();
            if (currentActivityId == _activityId)
            {
                return _hasChanged;
            }

            _activityId = currentActivityId;
            return _hasChanged = HasChangedCore();
        }

        private bool HasChangedCore()
        {
            lock (_children)
            {
                if (_children.Count < 10)
                {
                    foreach (var child in _children)
                    {
                        if (child.HasChanged())
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
                        if (child.HasChanged())
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
