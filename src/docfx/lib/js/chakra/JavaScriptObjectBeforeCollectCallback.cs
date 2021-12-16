// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     A callback called before collecting an object.
/// </summary>
/// <remarks>
///     Use <c>JsSetObjectBeforeCollectCallback</c> to register this callback.
/// </remarks>
/// <param name="ref">The object to be collected.</param>
/// <param name="callbackState">The state passed to <c>JsSetObjectBeforeCollectCallback</c>.</param>
public delegate void JavaScriptObjectBeforeCollectCallback(JavaScriptValue reference, IntPtr callbackState);
