// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    public class XrefMap
    {
        public List<XrefSpec> References { get; } = new List<XrefSpec>();
    }
}
