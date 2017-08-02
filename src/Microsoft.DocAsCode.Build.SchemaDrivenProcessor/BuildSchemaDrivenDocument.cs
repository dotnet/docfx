// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDrivenProcessor
{
    using System.Collections.Generic;
    using System.Composition;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.SchemaDrivenProcessor.Processors;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildSchemaBasedDocument : BuildReferenceDocumentBase, ISupportIncrementalBuildStep
    {
        private const string DocumentTypeKey = "documentType";
        private readonly SchemaProcessor _schemaProcessor = new SchemaProcessor();

        public override string Name => nameof(BuildSchemaBasedDocument);

        public override int BuildOrder => 0;

        protected override void BuildArticle(IHostService host, FileModel model)
        {
            var content = model.Content;
            var context = new ProcessContext(host, model);
            context.Properties.Uids = new List<UidDefinition>();
            context.Properties.UidLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            context.Properties.FileLinkSources = new Dictionary<string, List<LinkSourceInfo>>();
            DocumentSchema schema = model.Properties.Schema;
            content = _schemaProcessor.Process(content, schema, context);
            model.LinkToUids = model.LinkToUids.Union(((Dictionary<string, List<LinkSourceInfo>>)context.Properties.UidLinkSources).Keys);
            model.LinkToFiles = model.LinkToFiles.Union(((Dictionary<string, List<LinkSourceInfo>>)context.Properties.FileLinkSources).Keys);
            model.FileLinkSources = model.FileLinkSources.Merge((Dictionary<string, List<LinkSourceInfo>>)context.Properties.FileLinkSources);
            model.UidLinkSources = model.UidLinkSources.Merge((Dictionary<string, List<LinkSourceInfo>>)context.Properties.UidLinkSources);
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
