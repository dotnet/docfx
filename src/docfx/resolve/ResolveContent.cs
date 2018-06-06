// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    // todo: insert to markdown pipleline to generate inclusion mappings
    internal delegate (string content, Document file) ResolveContent(Document relativeTo, string href, bool isInclude = false);
}
