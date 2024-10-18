// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.ResourceFiles;

[Export(typeof(IDocumentProcessor))]
class ResourceDocumentProcessor : DisposableDocumentProcessor
{
    [ImportMany]
    public IEnumerable<IResourceFileConfig> Configs { get; set; }

    public override string Name => nameof(ResourceDocumentProcessor);

    [ImportMany(nameof(ResourceDocumentProcessor))]
    public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    public override ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type == DocumentType.Resource)
        {
            return ProcessingPriority.Normal;
        }
        if (file.Type == DocumentType.Article)
        {
            foreach (var config in Configs)
            {
                if (config.IsResourceFile(Path.GetExtension(file.File)))
                {
                    return ProcessingPriority.Lowest;
                }
            }
        }
        return ProcessingPriority.NotSupported;
    }

    public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        return new FileModel(file, new Dictionary<string, object>())
        {
            Uids = [],
            LocalPathFromRoot = PathUtility.MakeRelativePath(
                EnvironmentContext.BaseDirectory,
                EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File)),
        };
    }

    public override SaveResult Save(FileModel model)
    {
        if (model.FileAndType != model.OriginalFileAndType)
        {
            EnvironmentContext.FileAbstractLayer.Copy(
                model.OriginalFileAndType.File,
                model.FileAndType.File);
        }
        var result = new SaveResult
        {
            DocumentType = "Resource",
            ResourceFile = model.File,
        };

        if (model.Content != null)
        {
            // For resources, e.g. image.png, file extension is kept
            result.FileWithoutExtension = model.File;
        }

        return result;
    }
}
