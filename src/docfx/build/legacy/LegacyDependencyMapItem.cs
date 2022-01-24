// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class LegacyDependencyMapItem
{
    public string From { get; }

    public string To { get; }

    public string? Version { get; }

    public DependencyType Type { get; }

    public LegacyDependencyMapItem(string from, string to, string? version, DependencyType type)
    {
        From = from;
        To = to;
        Version = version;
        Type = type;
    }
}
