// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Plugins;

    internal sealed class ManifestItemWithContext
    {
        public InternalManifestItem Item { get; }

        public FileModel FileModel { get; }

        public IDocumentProcessor Processor { get; }

        public TemplateBundle TemplateBundle { get; }

        public TransformModelOptions Options { get; set; }

        public ManifestItemWithContext(InternalManifestItem item, FileModel model, IDocumentProcessor processor, TemplateBundle bundle)
        {
            Item = item;
            FileModel = model;
            Processor = processor;
            TemplateBundle = bundle;
        }
    }
}
