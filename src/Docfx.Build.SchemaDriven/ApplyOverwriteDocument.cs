// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Common;
using Docfx.Common;
using Docfx.Exceptions;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven;

[Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
public class ApplyOverwriteDocument : BaseDocumentBuildStep
{
    public override string Name => nameof(ApplyOverwriteDocument);

    public override int BuildOrder => 0x10;

    public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
        var overwriteApplier = new OverwriteApplier(host, OverwriteModelType.Classic);
        host.GetAllUids().RunAll(uid => ApplyOverwriteToModel(overwriteApplier, uid, host));
    }

    private static void ApplyOverwriteToModel(OverwriteApplier overwriteApplier, string uid, IHostService host)
    {
        var ms = host.LookupByUid(uid);
        var ods = ms.Where(m => m.Type == DocumentType.Overwrite).ToList();
        var articles = ms.Except(ods).ToList();
        if (articles.Count == 0 || ods.Count == 0)
        {
            return;
        }

        if (articles.Count > 1)
        {
            var message = $"{uid} is defined in multiple articles {articles.Select(s => s.LocalPathFromRoot).ToDelimitedString()}";
            Logger.LogError(message, code: ErrorCodes.Build.UidFoundInMultipleArticles);
            throw new DocumentException(message);
        }

        var model = articles[0];
        var schema = model.Properties.Schema as DocumentSchema;
        using (new LoggerFileScope(model.LocalPathFromRoot))
        {
            var uidDefinition = model.Uids.Where(s => s.Name == uid).ToList();
            if (uidDefinition.Count == 0)
            {
                throw new DocfxException($"Unable to find UidDefinition for Uid {uid}");
            }

            try
            {
                foreach (var ud in uidDefinition)
                {
                    var jsonPointer = new JsonPointer(ud.Path).GetParentPointer();
                    var schemaForCurrentUid = jsonPointer.FindSchema(schema);
                    var source = jsonPointer.GetValue(model.Content);

                    foreach (var od in ods)
                    {
                        using (new LoggerFileScope(od.LocalPathFromRoot))
                        {
                            foreach (var fm in ((IEnumerable<OverwriteDocumentModel>)od.Content).Where(s => s.Uid == uid))
                            {
                                // Suppose that BuildOverwriteWithSchema do the validation of the overwrite object
                                var overwriteObject = overwriteApplier.BuildOverwriteWithSchema(od, fm, schemaForCurrentUid);
                                overwriteApplier.MergeContentWithOverwrite(ref source, overwriteObject, ud.Name, string.Empty, schemaForCurrentUid);

                                model.LinkToUids = model.LinkToUids.Union(od.LinkToUids);
                                model.LinkToFiles = model.LinkToFiles.Union(od.LinkToFiles);
                                model.FileLinkSources = model.FileLinkSources.Merge(od.FileLinkSources);
                                model.UidLinkSources = model.UidLinkSources.Merge(od.UidLinkSources);
                            }
                        }
                    }
                }

                // 1. Validate schema after the merge
                // TODO: Issue exists - however unable to identify which overwrite document the issue is from
                ((SchemaDrivenDocumentProcessor)host.Processor).SchemaValidator.Validate(model.Content);

                // 2. Re-export xrefspec after the merge
                overwriteApplier.UpdateXrefSpec(model, schema);

            }
            catch (DocumentException e)
            {
                // Log error here to preserve file info
                Logger.LogError(e.Message);
                throw;
            }
        }
    }
}
