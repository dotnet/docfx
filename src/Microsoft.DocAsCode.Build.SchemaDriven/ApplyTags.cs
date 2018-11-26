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
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyTags : BaseDocumentBuildStep, ISupportIncrementalBuildStep
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

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
