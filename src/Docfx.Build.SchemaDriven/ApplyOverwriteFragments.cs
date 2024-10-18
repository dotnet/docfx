// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;

using Docfx.Build.Common;
using Docfx.Build.OverwriteDocuments;
using Docfx.Common;
using Docfx.Exceptions;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

using YamlDotNet.RepresentationModel;

namespace Docfx.Build.SchemaDriven;

[Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
public class ApplyOverwriteFragments : BaseDocumentBuildStep
{
    public override string Name => nameof(ApplyOverwriteFragments);

    public override int BuildOrder => 0x08;

    public override void Build(FileModel model, IHostService host)
    {
        if (model.MarkdownFragmentsModel == null)
        {
            return;
        }

        if (model.MarkdownFragmentsModel.Content == null)
        {
            return;
        }

        if (model.MarkdownFragmentsModel.Content is not string)
        {
            var message = "Unable to parse markdown fragments. Expect string content.";
            Logger.LogError(message);
            throw new DocfxException(message);
        }
        if (model.MarkdownFragmentsModel.Properties.MarkdigMarkdownService == null || model.MarkdownFragmentsModel.Properties.MarkdigMarkdownService is not MarkdigMarkdownService)
        {
            var message = "Unable to find markdig markdown service in file model.";
            Logger.LogError(message);
            throw new DocfxException(message);
        }
        if (model.Properties.Schema is not DocumentSchema)
        {
            var message = "Unable to find schema in file model.";
            Logger.LogError(message);
            throw new DocfxException(message);
        }

        using (new LoggerFileScope(model.MarkdownFragmentsModel.LocalPathFromRoot))
        {
            try
            {
                BuildCore(model, host);
            }
            catch (MarkdownFragmentsException ex)
            {
                Logger.LogWarning(
                    $"Unable to parse markdown fragments: {ex.Message}",
                    line: ex.Position == -1 ? null : (ex.Position + 1).ToString(),
                    code: WarningCodes.Overwrite.InvalidMarkdownFragments);
            }
            catch (DocumentException de)
            {
                Logger.LogError(de.Message);
                throw;
            }
        }
    }

    private static void BuildCore(FileModel model, IHostService host)
    {
        var markdownService = (MarkdigMarkdownService)model.MarkdownFragmentsModel.Properties.MarkdigMarkdownService;
        var overwriteDocumentModelCreator = new OverwriteDocumentModelCreator(model.MarkdownFragmentsModel.OriginalFileAndType.File);
        var overwriteApplier = new OverwriteApplier(host, OverwriteModelType.MarkdownFragments);
        var schema = model.Properties.Schema as DocumentSchema;
        List<OverwriteDocumentModel> overwriteDocumentModels;

        // 1. string => AST(MarkdownDocument)
        var ast = markdownService.Parse((string)model.MarkdownFragmentsModel.Content, model.MarkdownFragmentsModel.OriginalFileAndType.File);

        // 2 AST(MarkdownDocument) => MarkdownFragmentModel
        var fragments = new MarkdownFragmentsCreator().Create(ast).ToList();

        // 3. MarkdownFragmentModel => OverwriteDocument
        overwriteDocumentModels = fragments.Select(overwriteDocumentModelCreator.Create).ToList();
        model.MarkdownFragmentsModel.Content = overwriteDocumentModels;

        // Validate here as OverwriteDocumentModelCreator already filtered some invalid cases, e.g. duplicated H2
        ValidateWithSchema(fragments, model);

        // 4. Apply schema to OverwriteDocument, and merge with skeleton YAML object
        foreach (var overwriteDocumentModel in overwriteDocumentModels)
        {
            var uidDefinitions = model.Uids.Where(s => s.Name == overwriteDocumentModel.Uid).ToList();
            if (uidDefinitions.Count == 0)
            {
                Logger.LogWarning(
                    $"Unable to find UidDefinition for Uid: {overwriteDocumentModel.Uid}",
                    code: WarningCodes.Overwrite.InvalidMarkdownFragments);
                continue;
            }
            if (uidDefinitions.Count > 1)
            {
                Logger.LogWarning($"There are more than one UidDefinitions found for Uid {overwriteDocumentModel.Uid} in lines {string.Join(", ", uidDefinitions.Select(uid => uid.Line).ToList())}");
            }

            var ud = uidDefinitions[0];
            var jsonPointer = new JsonPointer(ud.Path).GetParentPointer();
            var schemaForCurrentUid = jsonPointer.FindSchema(schema);
            var source = jsonPointer.GetValue(model.Content);
            var overwriteObject = overwriteApplier.BuildOverwriteWithSchema(model.MarkdownFragmentsModel, overwriteDocumentModel, schema);
            overwriteApplier.MergeContentWithOverwrite(ref source, overwriteObject, ud.Name, string.Empty, schemaForCurrentUid);
        }

        // 5. Validate schema after the merge
        using (new LoggerFileScope(model.LocalPathFromRoot))
        {
            ((SchemaDrivenDocumentProcessor)host.Processor).SchemaValidator.Validate(model.Content);
        }

        // 6. Re-export xrefspec after the merge
        overwriteApplier.UpdateXrefSpec(model, schema);

        model.LinkToUids = model.LinkToUids.Union(model.MarkdownFragmentsModel.LinkToUids);
        model.LinkToFiles = model.LinkToFiles.Union(model.MarkdownFragmentsModel.LinkToFiles);
        model.FileLinkSources = model.FileLinkSources.Merge(model.MarkdownFragmentsModel.FileLinkSources);
        model.UidLinkSources = model.UidLinkSources.Merge(model.MarkdownFragmentsModel.UidLinkSources);
        model.MarkdownFragmentsModel.Content = overwriteDocumentModels;
    }

    private static void ValidateWithSchema(List<MarkdownFragmentModel> fragments, FileModel model)
    {
        var iterator = new SchemaFragmentsIterator(new ValidateFragmentsHandler());
        var yamlStream = new YamlStream();
        using (var sr = EnvironmentContext.FileAbstractLayer.OpenReadText(model.FileAndType.File))
        {
            yamlStream.Load(sr);
        }
        iterator.Traverse(
            yamlStream.Documents[0].RootNode,
            fragments
                .GroupBy(f => f.Uid)
                .ToDictionary(g => g.Key, g => g.First().ToMarkdownFragment()),
            model.Properties.Schema);
    }
}
