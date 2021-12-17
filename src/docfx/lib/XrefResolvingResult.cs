// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal readonly struct XrefResolvingResult
{
    public readonly string? Href;

    public readonly string Display;

    public readonly bool Localizable;

    public XrefResolvingResult(string? href, string display, bool localizable)
    {
        Href = href;
        Display = display;
        Localizable = localizable;
    }
}
