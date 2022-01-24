// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class WatchDebugView<T>
{
    private readonly Watch<T> _watch;

    public WatchDebugView(Watch<T> watch) => _watch = watch;

    public int ChangeCount => _watch.ChangeCount;

    public T? Value => _watch.ValueForDebugDisplay;
}
