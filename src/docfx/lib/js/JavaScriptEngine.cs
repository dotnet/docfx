// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

/// <summary>
/// Represents a javascript engine for a single thread
/// </summary>
internal abstract class JavaScriptEngine : IDisposable
{
    public abstract JToken Run(string scriptPath, string methodName, JToken arg);

    public static JavaScriptEngine Create(Package package, JObject? global = null)
    {
        // TODO: remove JINT after Microsoft.CharkraCore NuGet package
        // supports linux and macOS: https://github.com/microsoft/ChakraCore/issues/2578
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ChakraCoreJsEngine(package, global)
            : new JintJsEngine(package, global);
    }

    public abstract void Dispose();
}
