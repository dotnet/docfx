// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Concurrent;

internal interface IMemoryCache
{
    void OnMemoryLow();

    void Clear();
}
