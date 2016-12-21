// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ReferenceBase
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// Base document processor for reference.
    /// </summary>
    public abstract class ReferenceDocumentProcessorBase : DisposableDocumentProcessor
    {
        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            switch (file.Type)
            {
                case DocumentType.Article:
                    return LoadArticle(file, metadata);
                case DocumentType.Overwrite:
                    return LoadOverwrite(file);
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
            var result = GenerateSaveResult(model);
            UpdateModelContent(model);
            return result;
        }

        protected virtual void UpdateModelContent(FileModel model)
        {
        }

        protected abstract FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata);

        protected abstract FileModel LoadOverwrite(FileAndType file);

        protected abstract SaveResult GenerateSaveResult(FileModel model);

    }
}
