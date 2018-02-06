// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
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

        public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            var overwriteApplier = new OverwriteApplier(host, OverwriteModelType.MarkdownFragments);
            foreach (var fileModel in models)
            {
                var overwriteDocumentModels = fileModel.MarkdownFragmentsModel.ModelWithCache.Content as List<OverwriteDocumentModel>;
                if (overwriteDocumentModels == null)
                {
                    continue;
                }

                var schema = fileModel.Properties.Schema as DocumentSchema;
                using (new LoggerFileScope(fileModel.LocalPathFromRoot))
                {
                    foreach (var overwriteDocumentModel in overwriteDocumentModels)
                    {
                        var uidDefiniton = fileModel.Uids.Where(s => s.Name == overwriteDocumentModel.Uid).ToList();
                        if (uidDefiniton.Count == 0)
                        {
                            Logger.LogWarning($"Unable to find UidDefinition for Uid {overwriteDocumentModel.Uid}");
                        }

                        if (uidDefiniton.Count > 1)
                        {
                            Logger.LogWarning($"There are more than one UidDefinitions found for Uid {overwriteDocumentModel.Uid}");
                        }

                        var ud = uidDefiniton[0];
                        var jsonPointer = new JsonPointer(ud.Path).GetParentPointer();
                        var schemaForCurrentUid = jsonPointer.FindSchema(schema);
                        var source = jsonPointer.GetValue(fileModel.Content);

                        overwriteApplier.MergeContentWithOverwrite(ref source, overwriteDocumentModel.Metadata, ud.Name, string.Empty, schemaForCurrentUid);
                    }

                    // 1. Validate schema after the merge
                    ((SchemaDrivenDocumentProcessor)host.Processor).SchemaValidator.Validate(fileModel.Content);

                    // 2. Re-export xrefspec after the merge
                    overwriteApplier.UpdateXrefSpec(fileModel, schema);
                }
            }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
