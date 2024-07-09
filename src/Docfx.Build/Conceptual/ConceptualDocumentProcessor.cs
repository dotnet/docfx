// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Common;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.ConceptualDocuments;

[Export(typeof(IDocumentProcessor))]
class ConceptualDocumentProcessor : DisposableDocumentProcessor
{
    private readonly string[] SystemKeys = [
        "conceptual",
        "type",
        "source",
        "path",
        "documentation",
        "title",
        "rawTitle",
        "wordCount"
    ];

    public ConceptualDocumentProcessor()
    {
    }

    [ImportMany(nameof(ConceptualDocumentProcessor))]
    public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    public override string Name => nameof(ConceptualDocumentProcessor);

    public override ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type != DocumentType.Article)
        {
            return ProcessingPriority.NotSupported;
        }
        if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
        {
            var filenameWithoutExtension = Path.ChangeExtension(file.File, null);

            // exclude overwrite markdown segments
            var subExtension = Path.GetExtension(filenameWithoutExtension);
            if ((".yml".Equals(subExtension, StringComparison.OrdinalIgnoreCase) ||
                ".yaml".Equals(subExtension, StringComparison.OrdinalIgnoreCase))
                && EnvironmentContext.FileAbstractLayer.Exists(filenameWithoutExtension))
            {
                return ProcessingPriority.NotSupported;
            }

            return ProcessingPriority.Normal;
        }
        return ProcessingPriority.NotSupported;
    }

    public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        if (file.Type != DocumentType.Article)
        {
            throw new NotSupportedException();
        }
        var content = MarkdownReader.ReadMarkdownAsConceptual(file.File);
        foreach (var (key, value) in metadata.OrderBy(item => item.Key))
        {
            content[key] = value;
        }
        content[Constants.PropertyName.SystemKeys] = SystemKeys;

        var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));

        return new FileModel(file, content)
        {
            LocalPathFromRoot = localPathFromRoot,
        };
    }

    public override SaveResult Save(FileModel model)
    {
        if (model.Type != DocumentType.Article)
        {
            throw new NotSupportedException();
        }

        string documentType = model.DocumentType;
        if (string.IsNullOrEmpty(documentType))
        {
            var properties = (IDictionary<string, object>)model.Content;
            documentType = properties.ContainsKey(Constants.PropertyName.RedirectUrl)
              ? Constants.DocumentType.Redirection
              : Constants.DocumentType.Conceptual;
        }

        var result = new SaveResult
        {
            DocumentType = documentType,
            FileWithoutExtension = Path.ChangeExtension(model.File, null),
            LinkToFiles = model.LinkToFiles.ToImmutableArray(),
            LinkToUids = model.LinkToUids,
            FileLinkSources = model.FileLinkSources,
            UidLinkSources = model.UidLinkSources,
        };

        if (((IDictionary<string, object>)model.Properties).TryGetValue("XrefSpec", out var value))
        {
            var xrefSpec = value as XRefSpec;
            if (xrefSpec != null)
            {
                result.XRefSpecs = [xrefSpec];
            }
        }

        return result;
    }
}
