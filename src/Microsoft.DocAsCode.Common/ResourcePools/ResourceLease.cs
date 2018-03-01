// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;

    public sealed class ResourceLease<T>
        : IDisposable
        where T : class
    {
        private readonly Action<ResourceLease<T>> _callback;

        internal ResourceLease(Action<ResourceLease<T>> callback, T resource)
        {
            _callback = callback;
            Resource = resource;
        }

        public T Resource { get; private set; }

        #region IDisposable

        public void Dispose()
        {
            _callback(this);
        }

        #endregion
    }
}
