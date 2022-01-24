// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal sealed class HttpConfig
{
    public Dictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}
