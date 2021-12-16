// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class WatchFunction : IFunction
{
    private const int MaxSerialProcessingCount = 10;

    private readonly HashSet<IFunction> _children = new();

    private bool _hasChanged;
    private object _hasChangedScope;
    private object _replayScope;

    public WatchFunction()
    {
        _hasChangedScope = _replayScope = Watcher.GetCurrentScope();
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
        var scope = Watcher.GetCurrentScope();
        if (scope == _hasChangedScope)
        {
            return _hasChanged;
        }

        lock (_children)
        {
            if (scope == _hasChangedScope)
            {
                return _hasChanged;
            }

            _hasChanged = HasChangedCore();
            _hasChangedScope = scope;
            return _hasChanged;
        }
    }

    public void Replay()
    {
        var scope = Watcher.GetCurrentScope();
        if (scope == _replayScope)
        {
            return;
        }

        lock (_children)
        {
            if (scope == _replayScope)
            {
                return;
            }

            _replayScope = scope;
            ReplayCore();
        }
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
