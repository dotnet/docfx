// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System.Collections.Generic;

    public interface IMetadataSchema
    {
        IReadOnlyDictionary<string, IMetadataDefinition> Definitions { get; }
        ValidationResults ValidateMetadata(string metadata);
    }
}
