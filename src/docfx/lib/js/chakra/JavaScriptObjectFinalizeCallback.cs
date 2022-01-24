// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     A finalization callback.
/// </summary>
/// <param name="data">
///     The external data that was passed in when creating the object being finalized.
/// </param>
public delegate void JavaScriptObjectFinalizeCallback(IntPtr data);
