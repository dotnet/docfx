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
    using System.IO;

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
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            var options = new PdfOptions
            {
                CssFilePath = "styles/default.css",
                PdfDocsetName = Path.GetFileName(manifest.SourceBasePath) ?? Path.GetRandomFileName(),
                DestDirectory = outputFolder,
                GenerateAppendices = true,
            };
            var p = new ConverterImpl(options);
            try
            {
                p.Convert(manifest, outputFolder);
                return manifest;
            }
            catch (Exception e) when (e is IOException)
            {
                throw new DocfxException($"Error converting to PDF: {e.Message}", e);
            }
        }
    }
}
