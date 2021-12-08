// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class DelegatingDisposable : IDisposable
{
    private readonly Action _dispose;

    public DelegatingDisposable(Action dispose) => _dispose = dispose;

    public void Dispose() => _dispose();
}
