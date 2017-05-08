// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class ResourcePoolManager<TResource>
        : IDisposable
        where TResource : class
    {
        private readonly object _syncRoot = new object();
        private readonly List<TResource> _resources = new List<TResource>();
        private readonly Stack<TResource> _stack = new Stack<TResource>();
        private readonly Func<TResource> _creator;
        private readonly int _maxResourceCount;

        public ResourcePoolManager(Func<TResource> creator, int maxResourceCount)
        {
            _creator = creator;
            _maxResourceCount = maxResourceCount;
        }

        public ResourceLease<TResource> Rent()
        {
            lock (_syncRoot)
            {
                while (true)
                {
                    if (_stack.Count > 0)
                    {
                        return new ResourceLease<TResource>(GiveBack, _stack.Pop());
                    }
                    if (_resources.Count < _maxResourceCount)
                    {
                        var resource = _creator();
                        _resources.Add(resource);
                        return new ResourceLease<TResource>(GiveBack, resource);
                    }
                    Monitor.Wait(_syncRoot);
                }
            }
        }

        private void GiveBack(ResourceLease<TResource> lease)
        {
            lock (_syncRoot)
            {
                _stack.Push(lease.Resource);
                if (_stack.Count == 1)
                {
                    Monitor.PulseAll(_syncRoot);
                }
            }
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ResourcePoolManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var resource in _resources)
                {
                    (resource as IDisposable)?.Dispose();
                }
            }
        }

        #endregion
    }
}
