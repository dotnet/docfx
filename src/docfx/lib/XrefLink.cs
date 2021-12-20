// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal readonly struct XrefLink
{
    public readonly string? Href;

    public readonly string Display;

    public readonly FilePath? DeclaringFile;

    public readonly bool Localizable;

    public XrefLink(string? href, string display, FilePath? declaringFile, bool localizable)
    {
        Href = href;
        Display = display;
        DeclaringFile = declaringFile;
        Localizable = localizable;
    }
}
