// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Immutable;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// Base document processor for reference.
    /// </summary>
    public abstract class ReferenceDocumentProcessorBase : DisposableDocumentProcessor
    {
        protected abstract string ProcessedDocumentType { get; }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    return LoadArticle(file, metadata);
                case DocumentType.Overwrite:
                    return LoadOverwrite(file, metadata);
                default:
                    throw new NotSupportedException();
            }
        }

        public override SaveResult Save(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                throw new NotSupportedException();
            }
            return new SaveResult
            {
                DocumentType = model.DocumentType ?? ProcessedDocumentType,
                FileWithoutExtension = Path.ChangeExtension(model.File, null),
                LinkToFiles = model.LinkToFiles.ToImmutableArray(),
                LinkToUids = model.LinkToUids,
                FileLinkSources = model.FileLinkSources,
                UidLinkSources = model.UidLinkSources,
            };
        }

        protected abstract FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata);

        protected virtual FileModel LoadOverwrite(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            // TODO: Refactor current behavior that overwrite file is read multiple times by multiple processors
            return OverwriteDocumentReader.Read(file);
        }
    }
}
