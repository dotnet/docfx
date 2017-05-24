// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.CodeAnalysis;
    using System.Collections.Generic;

    internal class ExtractMetadataOptions
    {
        public bool ShouldSkipMarkup { get; set; }
        public bool PreserveRawinlineComments { get; set; }
        public string FilterConfigFile { get; set; }
        public bool UseCompatibilityFileName { get; set; }
        public IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> ExtensionMethods { get; set; }
    }
}
