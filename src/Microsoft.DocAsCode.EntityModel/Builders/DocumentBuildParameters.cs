// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System.Collections.Immutable;

    public sealed class DocumentBuildParameters
    {
        public FileCollection Files { get; set; }
        public string OutputBaseDir { get; set; }
        public ImmutableArray<string> ExternalReferencePackages { get; set; }
        public ImmutableDictionary<string, object> Metadata { get; set; }
    }
}
