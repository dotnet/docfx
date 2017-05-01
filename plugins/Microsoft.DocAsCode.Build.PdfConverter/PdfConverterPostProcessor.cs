// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.PdfConverter
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(PdfConverterPostProcessor), typeof(IPostProcessor))]
    public class PdfConverterPostProcessor : IPostProcessor
    {
        public IPostProcessorHost PostProcessorHost { get; set; }

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            throw new NotImplementedException();
        }
    }
}
