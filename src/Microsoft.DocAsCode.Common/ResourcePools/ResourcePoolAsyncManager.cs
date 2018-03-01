// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public class ResourcePoolAsyncManager<TResource>
        : IDisposable
        where TResource : class
    {
        private readonly ConcurrentBag<TResource> _resources = new ConcurrentBag<TResource>();
        private readonly ConcurrentStack<TResource> _stack = new ConcurrentStack<TResource>();
        private readonly Func<Task<TResource>> _creator;
        private readonly SemaphoreSlim _semaphore;

        public ResourcePoolAsyncManager(Func<Task<TResource>> creator, int maxResourceCount)
        {
            _creator = creator;
            _semaphore = new SemaphoreSlim(maxResourceCount);
        }

        public async Task<ResourceLease<TResource>> RentAsync()
        {
            await _semaphore.WaitAsync();
            if (!_stack.TryPop(out TResource resource))
            {
                resource = await _creator();
                _resources.Add(resource);
            }
            return new ResourceLease<TResource>(GiveBack, resource);
        }

        private void GiveBack(ResourceLease<TResource> lease)
        {
            _stack.Push(lease.Resource);
            _semaphore.Release();
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ResourcePoolAsyncManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_resources != null)
                {
                    foreach (var resource in _resources)
                    {
                        (resource as IDisposable)?.Dispose();
                    }
                }
            }
        }

        #endregion
    }
}
