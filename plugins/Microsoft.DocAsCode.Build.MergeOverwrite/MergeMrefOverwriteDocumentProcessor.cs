// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.MergeOverwrite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class MergeMrefOverwriteDocumentProcessor : IDocumentProcessor, IDocumentBuildStep
    {
        private readonly ApplyOverwriteDocumentForMref _applyOverwrite = new ApplyOverwriteDocumentForMref();
        private readonly ManagedReferenceDocumentProcessor _mrefProcessor = new ManagedReferenceDocumentProcessor();
        private readonly BuildManagedReferenceDocument _buildMref = new BuildManagedReferenceDocument();

        public int BuildOrder => 0;
        public IEnumerable<IDocumentBuildStep> BuildSteps => new[] { this };

        public string Name => nameof(MergeMrefOverwriteDocumentProcessor);

        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            var priority = _mrefProcessor.GetProcessingPriority(file);
            if (priority == ProcessingPriority.NotSupported) return ProcessingPriority.NotSupported;
            
            return (priority + 1); // Ensure the build engine favor us over ManagedReferenceDocumentProcessor
        }

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host) => models;

        public void Build(FileModel model, IHostService host)
        {
            // Build override documents with "momd" markdown engine and then html decode the result.
            if (model.Type == DocumentType.Overwrite)
            {
                _buildMref.Build(model, host);

                foreach (var overwrite in (IEnumerable<OverwriteDocumentModel>)model.Content)
                {
                    overwrite.Conceptual = StringHelper.HtmlDecode(overwrite.Conceptual);
                }
            }
        }

        public void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            _applyOverwrite.Postbuild(models, host);
        }

        public void UpdateHref(FileModel model, IDocumentBuildContext context) { }

        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata) => _mrefProcessor.Load(file, metadata);

        public SaveResult Save(FileModel model)
        {
            if (model.Type == DocumentType.Article)
            {
                YamlUtility.Serialize(model.File, model.Content);
                return new SaveResult { DocumentType = "MergeMrefOverwrite" };
            }
            return null;
        }
    }
}
