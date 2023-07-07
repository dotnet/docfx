// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Common;
using Docfx.Build.SchemaDriven.Processors;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven;

[Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
public class ApplyTags : BaseDocumentBuildStep
{
    [ImportMany]
    public IEnumerable<ITagInterpreter> TagInterpreters { get; set; }

    public override string Name => nameof(ApplyTags);

    public override int BuildOrder => 0x100;

    public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
        if (TagInterpreters == null)
        {
            return;
        }

        var schemaProcessor = new SchemaProcessor(new TagsInterpreter(TagInterpreters.ToList()));

        models.Where(s => s.Type == DocumentType.Article).RunAll(model =>
        {
            model.Content = schemaProcessor.Process(model.Content, model.Properties.Schema, new ProcessContext(host, model));
        });
    }
}
