// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     A callback called before collection.
/// </summary>
/// <param name="callbackState">The state passed to SetBeforeCollectCallback.</param>
public delegate void JavaScriptBeforeCollectCallback(IntPtr callbackState);
