// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.SchemaDriven.Processors;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.Plugins;

    public class OverwriteApplier
    {
        private readonly SchemaProcessor _xrefSpecUpdater = new SchemaProcessor(
            new XrefPropertiesInterpreter()
        );

        private readonly Merger _merger;

        private readonly IHostService _host;
        private readonly SchemaProcessor _overwriteProcessor;
        private readonly OverwriteModelType _overwriteModelType;

        public OverwriteApplier(IHostService host, OverwriteModelType type)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _overwriteModelType = type;
            _merger = new Merger
            {
                OverwriteType = type
            };
            switch (type)
            {
                case OverwriteModelType.Classic:
                    _overwriteProcessor = new SchemaProcessor(
                        new FileIncludeInterpreter(),
                        new MarkdownWithContentAnchorInterpreter(new MarkdownInterpreter()),
                        new FileInterpreter(true, false),
                        new HrefInterpreter(true, false),
                        new XrefInterpreter(true, false)
                    );
                    break;
                case OverwriteModelType.MarkdownFragments:
                    _overwriteProcessor = new SchemaProcessor(
                        new FragmentsValidationInterpreter(),
                        new FileIncludeInterpreter(),
                        new FileInterpreter(true, false),
                        new HrefInterpreter(true, false),
                        new MarkdownAstInterpreter(new MarkdownInterpreter()),
                        new XrefInterpreter(true, false)
                    );
                    break;
            }
        }

        public void UpdateXrefSpec(FileModel fileModel, BaseSchema schema)
        {
            if (fileModel == null)
            {
                return;
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            var context = new ProcessContext(_host, fileModel);
            _xrefSpecUpdater.Process(fileModel.Content, schema, context);

            UpdateXRefSpecs((List<XRefSpec>)fileModel.Properties.XRefSpecs, context.XRefSpecs);
            UpdateXRefSpecs((List<XRefSpec>)fileModel.Properties.ExternalXRefSpecs, context.ExternalXRefSpecs);
        }

        public object BuildOverwriteWithSchema(FileModel owModel, OverwriteDocumentModel overwrite, BaseSchema schema)
        {
            if (overwrite == null || owModel == null)
            {
                return null;
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            dynamic overwriteObject = ConvertToObjectHelper.ConvertToDynamic(overwrite.Metadata);
            overwriteObject.uid = overwrite.Uid;
            var overwriteModel = new FileModel(owModel.FileAndType, overwriteObject, owModel.OriginalFileAndType);
            var context = (((IDictionary<string, object>)(owModel.Properties)).TryGetValue("MarkdigMarkdownService", out var service))
                ? new ProcessContext(_host, overwriteModel, null, (MarkdigMarkdownService)service)
                : new ProcessContext(_host, overwriteModel);
            if (_overwriteModelType == OverwriteModelType.Classic)
            {
                context.ContentAnchorParser = new ContentAnchorParser(overwrite.Conceptual);
            }

            var transformed = _overwriteProcessor.Process(overwriteObject, schema, context) as IDictionary<string, object>;
            if (_overwriteModelType == OverwriteModelType.Classic && !context.ContentAnchorParser.ContainsAnchor)
            {
                transformed["conceptual"] = context.ContentAnchorParser.Content;
            }

            // add SouceDetail back to transformed, in week type
            if (overwrite.Documentation != null)
            {
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
            }

            owModel.LinkToUids = owModel.LinkToUids.Union((context.UidLinkSources).Keys);
            owModel.LinkToFiles = owModel.LinkToFiles.Union((context.FileLinkSources).Keys);
            owModel.FileLinkSources = owModel.FileLinkSources.Merge(context.FileLinkSources);
            owModel.UidLinkSources = owModel.UidLinkSources.Merge(context.UidLinkSources);
            owModel.Uids = owModel.Uids.AddRange(context.Uids);
            owModel.Properties.XRefSpecs = context.XRefSpecs;
            owModel.Properties.ExternalXRefSpecs = context.ExternalXRefSpecs;

            foreach (var d in context.Dependency)
            {
                _host.ReportDependencyTo(owModel, d, DependencyTypeName.Include);
            }
            return transformed;
        }

        public void MergeContentWithOverwrite(ref object source, object overwrite, string uid, string path, BaseSchema schema)
        {
            _merger.Merge(ref source, overwrite, uid, path, schema);
        }

        private void UpdateXRefSpecs(List<XRefSpec> original, List<XRefSpec> overwrite)
        {
            foreach (var xref in overwrite)
            {
                var index = original.FindIndex(s => s.Uid == xref.Uid);
                if (index > -1)
                {
                    original[index] = xref;
                }
            }
        }
    }
}
