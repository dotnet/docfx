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
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteDocument : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        public override string Name => nameof(ApplyOverwriteDocument);

        public override int BuildOrder => 0x10;

        public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            var schemaProcessor = new SchemaProcessor(new MergeTypeInterpreter());

            foreach (var uid in host.GetAllUids())
            {
                var ms = host.LookupByUid(uid);
                var od = ms.Where(m => m.Type == DocumentType.Overwrite).ToList();
                var articles = ms.Except(od).ToList();
                if (articles.Count == 0 || od.Count == 0)
                {
                    continue;
                }

                if (articles.Count > 1)
                {
                    throw new DocumentException($"{uid} is defined in multiple articles {articles.Select(s => s.LocalPathFromRoot).ToDelimitedString()}");
                }
                var model = articles[0];
                var uids = model.Properties.Uids;
                model.Content = schemaProcessor.Process(model.Content, model.Properties.Schema, new ProcessContext(host, model));
            }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
