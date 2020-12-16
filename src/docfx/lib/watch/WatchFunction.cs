// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class WatchFunction : IFunction
    {
        private const int MaxSerialProcessingCount = 10;

        private readonly HashSet<IFunction> _children = new();

        private volatile bool _hasChanged;
        private volatile int _hasChangedActivityId;
        private volatile int _replayActivityId;

        public WatchFunction()
        {
            _hasChangedActivityId = _replayActivityId = Watcher.GetActivityId();
        }

        public bool HasChildren => _children.Count > 0;

        public void AddChild(IFunction childFunction)
        {
            lock (_children)
            {
                _children.Add(childFunction);
            }
        }

        public bool HasChanged()
        {
            var activityId = Watcher.GetActivityId();
            if (activityId == _hasChangedActivityId)
            {
                return _hasChanged;
            }

            _hasChangedActivityId = activityId;
            return _hasChanged = HasChangedCore();
        }

        public void Replay()
        {
            var activityId = Watcher.GetActivityId();
            if (activityId == _replayActivityId)
            {
                return;
            }

            _replayActivityId = activityId;
            ReplayCore();
        }

        private bool HasChangedCore()
        {
            lock (_children)
            {
                if (_children.Count < MaxSerialProcessingCount)
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

        private void ReplayCore()
        {
            lock (_children)
            {
                if (_children.Count < MaxSerialProcessingCount)
                {
                    foreach (var child in _children)
                    {
                        child.Replay();
                    }
                }
                else
                {
                    Parallel.ForEach(_children, child => child.Replay());
                }
            }
        }
    }
}
