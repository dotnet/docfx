// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.SchemaDriven.Processors;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocument : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        private readonly SchemaProcessor _overwriteProcessor = new SchemaProcessor(
            new FileIncludeInterpreter(),
            new MarkdownWithContentAnchorInterpreter(new MarkdownInterpreter()),
            new FileInterpreter(true, false),
            new HrefInterpreter(true, false),
            new XrefInterpreter()
            );

        private readonly SchemaProcessor _xrefSpecUpdater = new SchemaProcessor(
            new XrefPropertiesInterpreter()
            );

        private readonly Merger _merger = new Merger();

        public override string Name => nameof(ApplyOverwriteDocument);

        public override int BuildOrder => 0x10;

        public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            foreach (var uid in host.GetAllUids())
            {
                var ms = host.LookupByUid(uid);
                var ods = ms.Where(m => m.Type == DocumentType.Overwrite).ToList();
                var articles = ms.Except(ods).ToList();
                if (articles.Count == 0 || ods.Count == 0)
                {
                    continue;
                }

                if (articles.Count > 1)
                {
                    throw new DocumentException($"{uid} is defined in multiple articles {articles.Select(s => s.LocalPathFromRoot).ToDelimitedString()}");
                }

                var model = articles[0];
                var schema = model.Properties.Schema as DocumentSchema;
                using (new LoggerFileScope(model.LocalPathFromRoot))
                {
                    var uidDefiniton = model.Uids.Where(s => s.Name == uid).ToList();
                    if (uidDefiniton.Count == 0)
                    {
                        throw new DocfxException($"Unable to find UidDefinition for Uid {uid}");
                    }

                    foreach (var ud in uidDefiniton)
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
                                    var overwriteObject = BuildOverwriteWithSchema(od, fm, host, schemaForCurrentUid);
                                    _merger.Merge(ref source, overwriteObject, ud.Name, string.Empty, schemaForCurrentUid);

                                    model.LinkToUids = model.LinkToUids.Union(od.LinkToUids);
                                    model.LinkToFiles = model.LinkToFiles.Union(od.LinkToFiles);
                                    model.FileLinkSources = model.FileLinkSources.Merge(od.FileLinkSources);
                                    model.UidLinkSources = model.UidLinkSources.Merge(od.UidLinkSources);
                                    ((List<XRefSpec>)model.Properties.XRefSpecs).AddRange((List<XRefSpec>)(od.Properties.XRefSpecs));
                                    ((List<XRefSpec>)model.Properties.ExternalXRefSpecs).AddRange((List<XRefSpec>)(od.Properties.ExternalXRefSpecs));
                                }
                            }
                        }
                    }

                    // 1. Validate schema after the merge
                    // ((SchemaDrivenDocumentProcessor)host.Processor).SchemaValidator.Validate(model.Content);

                    // 2. Re-export xrefspec after the merge
                    var context = new ProcessContext(host, model);
                    _xrefSpecUpdater.Process(model.Content, schema, context);

                    ((List<XRefSpec>)model.Properties.XRefSpecs).AddRange((context.XRefSpecs));
                    ((List<XRefSpec>)model.Properties.ExternalXRefSpecs).AddRange((context.ExternalXRefSpecs));
                }
            }
        }

        private object BuildOverwriteWithSchema(FileModel owModel, OverwriteDocumentModel overwrite, IHostService host, BaseSchema schema)
        {
            dynamic overwriteObject = ConvertToObjectHelper.ConvertToDynamic(overwrite.Metadata);
            overwriteObject.uid = overwrite.Uid;
            var overwriteModel = new FileModel(owModel.FileAndType, overwriteObject, owModel.OriginalFileAndType);
            var context = new ProcessContext(host, overwriteModel)
            {
                ContentAnchorParser = new ContentAnchorParser(overwrite.Conceptual)
            };

            var transformed = _overwriteProcessor.Process(overwriteObject, schema, context) as IDictionary<string, object>;
            if (!context.ContentAnchorParser.ContainsAnchor)
            {
                transformed["conceptual"] = context.ContentAnchorParser.Content;
            }

            // add SouceDetail back to transformed, in week type
            transformed[Constants.PropertyName.Documentation] = new Dictionary<string, object>
            {
                ["remote"] = overwrite.Documentation.Remote == null ? null : new Dictionary<string, object>
                {
                    ["path"] = overwrite.Documentation.Remote.RelativePath,
                    ["branch"] = overwrite.Documentation.Remote.RemoteBranch,
                    ["repo"] = overwrite.Documentation.Remote.RemoteRepositoryUrl,
                }
                ["path"] = overwrite.Documentation?.Path,
                ["startLine"] = overwrite.Documentation?.StartLine ?? 0,
                ["endLine"] = overwrite.Documentation?.EndLine ?? 0,
            };

            owModel.LinkToUids = owModel.LinkToUids.Union((context.UidLinkSources).Keys);
            owModel.LinkToFiles = owModel.LinkToFiles.Union((context.FileLinkSources).Keys);
            owModel.FileLinkSources = owModel.FileLinkSources.Merge(context.FileLinkSources);
            owModel.UidLinkSources = owModel.UidLinkSources.Merge(context.UidLinkSources);
            owModel.Uids = owModel.Uids.AddRange(context.Uids);
            owModel.Properties.XRefSpecs = context.XRefSpecs;
            owModel.Properties.ExternalXRefSpecs = context.ExternalXRefSpecs;

            foreach (var d in context.Dependency)
            {
                host.ReportDependencyTo(owModel, d, DependencyTypeName.Include);
            }
            return transformed;
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
