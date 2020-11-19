// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public class LanguageServerNotification
    {
        public string Method { get; set; } = string.Empty;

        public string Params { get; set; } = string.Empty;

        public LanguageServerNotification(string method, string @params)
        {
            Method = method;
            Params = @params;
        }
    }
}
