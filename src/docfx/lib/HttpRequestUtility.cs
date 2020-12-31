// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Microsoft.Docs.Build
{
    internal static class HttpRequestUtility
    {
        public static bool TryUpdateHeader(this HttpRequestMessage request, string name, string value)
        {
            if (!request.Headers.Contains(name))
            {
                request.Headers.Add(name, value);
                return true;
            }
            return false;
        }
    }
}
