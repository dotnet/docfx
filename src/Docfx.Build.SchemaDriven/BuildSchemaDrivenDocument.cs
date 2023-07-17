// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;

using Docfx.Build.Common;
using Docfx.Build.SchemaDriven.Processors;
using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven;

[Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
public class BuildSchemaBasedDocument : BuildReferenceDocumentBase
{
    private const string DocumentTypeKey = "documentType";
    private readonly SchemaProcessor _schemaProcessor = new(
        new FileIncludeInterpreter(),
        new MarkdownInterpreter(),
        new XrefPropertiesInterpreter(),
        new FileInterpreter(true, false),
        new HrefInterpreter(true, false),
        new XrefInterpreter(true, false)
        );

    public override string Name => nameof(BuildSchemaBasedDocument);

    public override int BuildOrder => 0;

    protected override void BuildArticle(IHostService host, FileModel model)
    {
        var content = model.Content;

        var context = new ProcessContext(host, model);
        DocumentSchema schema = model.Properties.Schema;
        content = _schemaProcessor.Process(content, schema, context);
        model.LinkToUids = model.LinkToUids.Union(context.UidLinkSources.Keys);
        model.LinkToFiles = model.LinkToFiles.Union(context.FileLinkSources.Keys);
        model.FileLinkSources = model.FileLinkSources.Merge(context.FileLinkSources);
        model.UidLinkSources = model.UidLinkSources.Merge(context.UidLinkSources);
        model.Uids = model.Uids.AddRange(context.Uids);
        model.Properties.XRefSpecs = context.XRefSpecs;
        model.Properties.ExternalXRefSpecs = context.ExternalXRefSpecs;

        if (content is IDictionary<string, object> eo)
        {
            if (eo.TryGetValue(DocumentTypeKey, out object documentType) && documentType is string dt)
            {
                model.DocumentType = dt;
            }
        }
    }
}
