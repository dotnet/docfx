// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

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
