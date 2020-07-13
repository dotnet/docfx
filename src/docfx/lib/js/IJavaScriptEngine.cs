// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents a javascript engine for a single thread
    /// </summary>
    internal interface IJavaScriptEngine
    {
        JToken Run(string scriptPath, string methodName, JToken arg);
    }
}
