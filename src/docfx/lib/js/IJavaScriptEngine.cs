// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal interface IJavaScriptEngine
    {
        JToken Run(string scriptPath, string methodName, JToken arg);
    }
}
