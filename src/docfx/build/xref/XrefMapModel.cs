// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class XrefMapModel
    {
        public List<ExternalXrefSpec> References { get; } = new List<ExternalXrefSpec>();
    }
}
