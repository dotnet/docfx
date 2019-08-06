// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal sealed class HttpConfig
    {
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
    }
}
