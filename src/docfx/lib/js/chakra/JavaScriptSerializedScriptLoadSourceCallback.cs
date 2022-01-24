// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     Called by the runtime to load the source code of the serialized script.
///     The caller must keep the script buffer valid until the JsSerializedScriptUnloadCallback.
/// </summary>
/// <param name="sourceContext">The context passed to Js[Parse|Run]SerializedScriptWithCallback</param>
/// <param name="scriptBuffer">The script returned.</param>
/// <returns>
///     true if the operation succeeded, false otherwise.
/// </returns>
public delegate bool JavaScriptSerializedScriptLoadSourceCallback(JavaScriptSourceContext sourceContext, out string scriptBuffer);
