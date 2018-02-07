// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteFragments : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        public override string Name => nameof(ApplyOverwriteFragments);

        public override int BuildOrder => 0x08;

        public virtual void Build(FileModel model, IHostService host)
        {
            var overwriteApplier = new OverwriteApplier(host, OverwriteModelType.MarkdownFragments);

            var overwriteDocumentModels = model.MarkdownFragmentsModel?.Content as List<OverwriteDocumentModel>;
            if (overwriteDocumentModels == null)
            {
                return;
            }

            var schema = model.Properties.Schema as DocumentSchema;
            using (new LoggerFileScope(model.LocalPathFromRoot))
            {
                foreach (var overwriteDocumentModel in overwriteDocumentModels)
                {
                    var uidDefiniton = model.Uids.Where(s => s.Name == overwriteDocumentModel.Uid).ToList();
                    if (uidDefiniton.Count == 0)
                    {
                        Logger.LogWarning($"Unable to find UidDefinition for Uid {overwriteDocumentModel.Uid}");
                    }

                    if (uidDefiniton.Count > 1)
                    {
                        Logger.LogWarning($"There are more than one UidDefinitions found for Uid {overwriteDocumentModel.Uid} in lines {string.Join(", ", uidDefiniton.Select(uid => uid.Line).ToList())}");
                    }

                    var ud = uidDefiniton[0];
                    var jsonPointer = new JsonPointer(ud.Path).GetParentPointer();
                    var schemaForCurrentUid = jsonPointer.FindSchema(schema);
                    var source = jsonPointer.GetValue(model.Content);

                    overwriteApplier.MergeContentWithOverwrite(ref source, overwriteDocumentModel.Metadata, ud.Name, string.Empty, schemaForCurrentUid);
                }

                // 1. Validate schema after the merge
                ((SchemaDrivenDocumentProcessor)host.Processor).SchemaValidator.Validate(model.Content);

                // 2. Re-export xrefspec after the merge
                overwriteApplier.UpdateXrefSpec(model, schema);
            }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
