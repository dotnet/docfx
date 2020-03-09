// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class XrefMapModel
    {
        public ExternalXrefSpec[] References { get; set; } = Array.Empty<ExternalXrefSpec>();

        public XrefProperties? Properties { get; set; }
    }
}
