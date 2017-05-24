// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    internal class ExtractMetadataOptions
    {
        public bool ShouldSkipMarkup { get; set; }
        public bool PreserveRawInlineComments { get; set; }
        public string FilterConfigFile { get; set; }
        public bool UseCompatibilityFileName { get; set; }
        public IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> ExtensionMethods { get; set; }
    }
}
