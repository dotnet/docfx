// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ServicePageItem
{
    public string? Name { get; }

    public string? Href { get; }

    public string? Uid { get; }

    public ServicePageItem(string? name, string? href, string? uid)
    {
        Name = name;
        Href = href;
        Uid = uid;
    }
}
