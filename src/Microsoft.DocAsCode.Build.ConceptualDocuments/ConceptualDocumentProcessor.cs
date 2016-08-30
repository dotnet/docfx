﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ConceptualDocuments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class ConceptualDocumentProcessor : DisposableDocumentProcessor, ISupportIncrementalDocumentProcessor
    {
        [ImportMany(nameof(ConceptualDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override string Name => nameof(ConceptualDocumentProcessor);

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type != DocumentType.Article)
            {
                return ProcessingPriority.NotSupported;
            }
            if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
            {
                return ProcessingPriority.Normal;
            }
            return ProcessingPriority.NotSupported;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            if (file.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            var content = MarkdownReader.ReadMarkdownAsConceptual(file.BaseDir, file.File);
            foreach (var item in metadata)
            {
                if (!content.ContainsKey(item.Key))
                {
                    content[item.Key] = item.Value;
                }
            }

            var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

            return new FileModel(
                file,
                content,
                serializer: Environment.Is64BitProcess ? null : new BinaryFormatter())
            {
                LocalPathFromRepoRoot = (content["source"] as SourceDetail)?.Remote?.RelativePath,
                LocalPathFromRoot = displayLocalPath
            };
        }

        public override SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }

            var result = new SaveResult
            {
                DocumentType = model.DocumentType ?? "Conceptual",
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = model.LinkToFiles.ToImmutableArray(),
                LinkToUids = model.LinkToUids,
                FileLinkSources = model.FileLinkSources,
                UidLinkSources = model.UidLinkSources,
            };
            if (model.Properties.XrefSpec != null)
            {
                result.XRefSpecs = ImmutableArray.Create(model.Properties.XrefSpec);
            }

            return result;
        }

        #region ISupportIncrementalDocumentProcessor Members

        public string GetIncrementalContextHash()
        {
            return null;
        }

        public void SaveIntermediateModel(FileModel model, Stream stream)
        {
            throw new NotImplementedException();
        }

        public FileModel LoadIntermediateModel(Stream stream)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
