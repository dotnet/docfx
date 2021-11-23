// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal enum UrlType
{
    /// <summary>
    /// a.md -> /xxx/a
    /// </summary>
    Docs,

    /// <summary>
    /// a.md --> /xxx/a/
    /// </summary>
    Pretty,

    /// <summary>
    /// a.md --> /xxx/a.html
    /// </summary>
    Ugly,
}
