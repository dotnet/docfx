// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class ResourceDocumentProcessor : IDocumentProcessor
    {
        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Resource)
            {
                return ProcessingPriority.Normal;
            }
            if (file.Type == DocumentType.Article)
            {
                return ProcessingPriority.Lowest;
            }
            return ProcessingPriority.NotSupportted;
        }

        public FileModel Load(FileAndType file)
        {
            return new FileModel(file, null)
            {
                Uids = new string[] { file.File }.ToImmutableArray(),
            };
        }

        public void Save(FileModel model)
        {
            if (model.FileAndType != model.OriginalFileAndType)
            {
                File.Copy(
                    Path.Combine(model.OriginalFileAndType.BaseDir, model.OriginalFileAndType.File),
                    Path.Combine(model.BaseDir, model.File),
                    true);
                // todo : metadata.
            }
        }

        public IEnumerable<FileModel> Prebuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article && model.Type != DocumentType.Resource)
            {
                throw new NotSupportedException();
            }
            // todo : metadata.
        }

        public IEnumerable<FileModel> Postbuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
