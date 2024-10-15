// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Build.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;

namespace Docfx.Build.ManagedReference;

[Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
public class FillMetadata : BaseDocumentBuildStep
{
    public override string Name => nameof(FillMetadata);
    public override int BuildOrder => 0x30;

    public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
        if (models.Count > 0)
        {
            foreach (var model in models)
            {
                if (model.Type != DocumentType.Article)
                {
                    continue;
                }

                model.ManifestProperties.Uid = null;
                var pageViewModel = (PageViewModel)model.Content;
                if (pageViewModel.Items.Count == 0)
                {
                    continue;
                }

                model.ManifestProperties.IsMRef = true;
                model.ManifestProperties.Title = pageViewModel.Items[0].FullName;
                model.ManifestProperties.Summary = pageViewModel.Items[0].Summary;
            }
        }
    }
}
