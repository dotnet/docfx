// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal delegate string ResolveHref(Document relativeTo, string href, Document resultRelativeTo);
}
