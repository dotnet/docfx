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
    using Microsoft.DocAsCode.Plugins;

    public class ApplyOverwriteUtility
    {
        private static readonly SchemaProcessor OverwriteProcessor = new SchemaProcessor(
            new FileIncludeInterpreter(),
            new MarkdownWithContentAnchorInterpreter(new MarkdownInterpreter()),
            new FileInterpreter(true, false),
            new HrefInterpreter(true, false),
            new XrefInterpreter()
        );

        private static readonly SchemaProcessor XrefSpecUpdater = new SchemaProcessor(
            new XrefPropertiesInterpreter()
        );

        private static readonly Merger Merger = new Merger();

        public static void ReExportXrefSpec(FileModel fileModel, IHostService host, BaseSchema schema)
        {
            var context = new ProcessContext(host, fileModel);
            XrefSpecUpdater.Process(fileModel.Content, schema, context);

            UpdateXRefSpecs((List<XRefSpec>)fileModel.Properties.XRefSpecs, context.XRefSpecs);
            UpdateXRefSpecs((List<XRefSpec>)fileModel.Properties.ExternalXRefSpecs, context.ExternalXRefSpecs);
        }

        public static void MergeContentWithOverwrite(ref object source, object overwrite, string uid, string path, BaseSchema schema)
        {
            Merger.Merge(ref source, overwrite, uid, path, schema);
        }

        public static object BuildOverwriteWithSchema(FileModel owModel, OverwriteDocumentModel overwrite, IHostService host, BaseSchema schema)
        {
            if (overwrite == null || owModel == null)
            {
                return null;
            }

            if (host == null)
            {
                throw new ArgumentException("host");
            }

            if (schema == null)
            {
                throw new ArgumentException("schema");
            }

            dynamic overwriteObject = ConvertToObjectHelper.ConvertToDynamic(overwrite.Metadata);
            overwriteObject.uid = overwrite.Uid;
            var overwriteModel = new FileModel(owModel.FileAndType, overwriteObject, owModel.OriginalFileAndType);
            var context = new ProcessContext(host, overwriteModel)
            {
                ContentAnchorParser = new ContentAnchorParser(overwrite.Conceptual)
            };

            var transformed = OverwriteProcessor.Process(overwriteObject, schema, context) as IDictionary<string, object>;
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

        private static void UpdateXRefSpecs(List<XRefSpec> original, List<XRefSpec> overwrite)
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
