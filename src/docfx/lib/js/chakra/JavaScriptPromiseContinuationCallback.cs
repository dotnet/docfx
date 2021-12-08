// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     A promise continuation callback.
/// </summary>
/// <remarks>
///     The host can specify a promise continuation callback in <c>JsSetPromiseContinuationCallback</c>. If
///     a script creates a task to be run later, then the promise continuation callback will be called with
///     the task and the task should be put in a FIFO queue, to be run when the current script is
///     done executing.
/// </remarks>
/// <param name="task">The task, represented as a JavaScript function.</param>
/// <param name="callbackState">The data argument to be passed to the callback.</param>
public delegate void JavaScriptPromiseContinuationCallback(JavaScriptValue task, IntPtr callbackState);
