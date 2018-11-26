// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System.Collections.Generic;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.OverwriteDocuments;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.Plugins;

    using YamlDotNet.RepresentationModel;

    [Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyOverwriteFragments : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        public override string Name => nameof(ApplyOverwriteFragments);

        public override int BuildOrder => 0x08;

        private static bool? IsUsingMarkdigMarkdownService = null;
        private static object SyncRoot = new object();

        public override void Build(FileModel model, IHostService host)
        {
            if (model.MarkdownFragmentsModel == null)
            {
                return;
            }

            host.ReportDependencyTo(model, model.MarkdownFragmentsModel.Key, DependencyTypeName.Include);

            if (model.MarkdownFragmentsModel.Content == null)
            {
                return;
            }

            CheckMarkdownService(host);
            if (!(model.MarkdownFragmentsModel.Content is string))
            {
                var message = "Unable to parse markdown fragments. Expect string content.";
                Logger.LogError(message);
                throw new DocfxException(message);
            }
            if (model.MarkdownFragmentsModel.Properties.MarkdigMarkdownService == null || !(model.MarkdownFragmentsModel.Properties.MarkdigMarkdownService is MarkdigMarkdownService))
            {
                var message = "Unable to find markdig markdown service in file model.";
                Logger.LogError(message);
                throw new DocfxException(message);
            }
            if (!(model.Properties.Schema is DocumentSchema))
            {
                var message = "Unable to find schema in file model.";
                Logger.LogError(message);
                throw new DocfxException(message);
            }

            using (new LoggerFileScope(model.MarkdownFragmentsModel.LocalPathFromRoot))
            {
                try
                {
                    BuildCore(model, host);
                }
                catch (MarkdownFragmentsException ex)
                {
                    Logger.LogWarning(
                        $"Unable to parse markdown fragments: {ex.Message}",
                        line: ex.Position == -1 ? null : (ex.Position + 1).ToString(),
                        code: WarningCodes.Overwrite.InvalidMarkdownFragments);
                    return;
                }
                catch (DocumentException de)
                {
                    Logger.LogError(de.Message);
                    throw;
                }
            }
        }

        private void BuildCore(FileModel model, IHostService host)
        {
            var markdownService = (MarkdigMarkdownService)model.MarkdownFragmentsModel.Properties.MarkdigMarkdownService;
            var overwriteDocumentModelCreater = new OverwriteDocumentModelCreater(model.MarkdownFragmentsModel.OriginalFileAndType.File);
            var overwriteApplier = new OverwriteApplier(host, OverwriteModelType.MarkdownFragments);
            var schema = model.Properties.Schema as DocumentSchema;
            List<OverwriteDocumentModel> overwriteDocumentModels;

            // 1. string => AST(MarkdownDocument)
            var ast = markdownService.Parse((string)model.MarkdownFragmentsModel.Content, model.MarkdownFragmentsModel.OriginalFileAndType.File);

            // 2 AST(MarkdownDocument) => MarkdownFragmentModel
            var fragments = new MarkdownFragmentsCreater().Create(ast).ToList();

            // 3. MarkdownFragmentModel => OverwriteDocument
            overwriteDocumentModels = fragments.Select(overwriteDocumentModelCreater.Create).ToList();
            model.MarkdownFragmentsModel.Content = overwriteDocumentModels;

            // Validate here as OverwriteDocumentModelCreater already filtered some invalid cases, e.g. duplicated H2
            ValidateWithSchema(fragments, model);

            // 4. Apply schema to OverwriteDocument, and merge with skeyleton YAML object
            foreach (var overwriteDocumentModel in overwriteDocumentModels)
            {
                var uidDefinitons = model.Uids.Where(s => s.Name == overwriteDocumentModel.Uid).ToList();
                if (uidDefinitons.Count == 0)
                {
                    Logger.LogWarning(
                        $"Unable to find UidDefinition for Uid: { overwriteDocumentModel.Uid}",
                        code: WarningCodes.Overwrite.InvalidMarkdownFragments);
                    continue;
                }
                if (uidDefinitons.Count > 1)
                {
                    Logger.LogWarning($"There are more than one UidDefinitions found for Uid {overwriteDocumentModel.Uid} in lines {string.Join(", ", uidDefinitons.Select(uid => uid.Line).ToList())}");
                }

                var ud = uidDefinitons[0];
                var jsonPointer = new JsonPointer(ud.Path).GetParentPointer();
                var schemaForCurrentUid = jsonPointer.FindSchema(schema);
                var source = jsonPointer.GetValue(model.Content);
                var overwriteObject = overwriteApplier.BuildOverwriteWithSchema(model.MarkdownFragmentsModel, overwriteDocumentModel, schema);
                overwriteApplier.MergeContentWithOverwrite(ref source, overwriteObject, ud.Name, string.Empty, schemaForCurrentUid);
            }

            // 5. Validate schema after the merge
            using (new LoggerFileScope(model.LocalPathFromRoot))
            {
                ((SchemaDrivenDocumentProcessor)host.Processor).SchemaValidator.Validate(model.Content);
            }

            // 6. Re-export xrefspec after the merge
            overwriteApplier.UpdateXrefSpec(model, schema);

            model.LinkToUids = model.LinkToUids.Union(model.MarkdownFragmentsModel.LinkToUids);
            model.LinkToFiles = model.LinkToFiles.Union(model.MarkdownFragmentsModel.LinkToFiles);
            model.FileLinkSources = model.FileLinkSources.Merge(model.MarkdownFragmentsModel.FileLinkSources);
            model.UidLinkSources = model.UidLinkSources.Merge(model.MarkdownFragmentsModel.UidLinkSources);
            model.MarkdownFragmentsModel.Content = overwriteDocumentModels;
        }

        private void ValidateWithSchema(List<MarkdownFragmentModel> fragments, FileModel model)
        {
            var iterator = new SchemaFragmentsIterator(new ValidateFragmentsHandler());
            var yamlStream = new YamlStream();
            using (var sr = EnvironmentContext.FileAbstractLayer.OpenReadText(model.FileAndType.File))
            {
                yamlStream.Load(sr);
            }
            iterator.Traverse(
                yamlStream.Documents[0].RootNode,
                fragments
                    .GroupBy(f => f.Uid)
                    .ToDictionary(g => g.Key, g => g.First().ToMarkdownFragment()),
                model.Properties.Schema);
        }

        private void CheckMarkdownService(IHostService host)
        {
            if (IsUsingMarkdigMarkdownService == null)
            {
                lock (SyncRoot)
                {
                    if (IsUsingMarkdigMarkdownService == null)
                    {
                        if (host.MarkdownServiceName != "markdig")
                        {
                            Logger.LogWarning("Markdownfragments depend on Markdig Markdown Engine. To avoid markup result inconsistency, please set `\"markdownEngineName\": \"markdig\"` in docfx.json's build section.");
                        }
                    }
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
