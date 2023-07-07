// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

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
