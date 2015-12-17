// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class ConceptualDocumentProcessor : IDocumentProcessor
    {
        [ImportMany(nameof(ConceptualDocumentProcessor))]
        public IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public string Name => nameof(ConceptualDocumentProcessor);

        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type != DocumentType.Article)
            {
                return ProcessingPriority.NotSupportted;
            }
            if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
            {
                return ProcessingPriority.Normal;
            }
            return ProcessingPriority.NotSupportted;
        }

        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
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
            return new FileModel(
                file,
                content,
                serializer: new BinaryFormatter())
            {
                LocalPathFromRepoRoot = (content["source"] as SourceDetail)?.Remote?.RelativePath
            };
        }

        public SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }

            string path = Path.Combine(model.BaseDir, model.File);
            try
            {
                JsonUtility.Serialize(path, model.Content);
            }
            catch (PathTooLongException e)
            {
                Logger.LogError($"Path \"{path}\": {e.Message}");
                throw;
            }

            return new SaveResult
            {
                DocumentType = model.DocumentType ?? "Conceptual",
                ModelFile = model.File,
                LinkToFiles = model.Properties.LinkToFiles,
                LinkToUids = model.Properties.LinkToUids,
            };
        }
    }
}
