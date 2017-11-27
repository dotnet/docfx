// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    internal class SourceFileInputParameters : IInputParameters
    {
        public ExtractMetadataOptions Options { get; }

        public SourceFileInputParameters(ExtractMetadataOptions options)
        {
            Options = options;
        }
    }
}
